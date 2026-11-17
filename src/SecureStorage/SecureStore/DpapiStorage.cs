using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExampleWPFClient.Infrastructure.SecureStore;

[SupportedOSPlatform("windows")]
public sealed class DpapiStorage<T> :
    ISecureStorage<T>
    where T : class
{
    #region Nested types

    private sealed record Envelope(
        int Version,
        T Session
        );

    #endregion

    #region Constants

    private const int FormatVersion = 1;

    #endregion

    #region Fields

    private readonly string _path;
    private readonly byte[] _entropy;
    private readonly SemaphoreSlim _lock = new(1, 1);

    #endregion

    #region Constructors

    public DpapiStorage(
        string applicationName,
        string storageScope
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        string typeName = typeof(T).Name;

        _path = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
                ),
            applicationName,
            storageScope,
            $"{typeName}.dat"
            );

        string typeIdentity = typeof(T).FullName ?? typeName;

        _entropy = Encoding.UTF8.GetBytes(
            $"{applicationName}.{storageScope}.{typeIdentity}");
    }

    #endregion

    #region ISecureStorage Implementation

    public async Task<T?> LoadAsync(CancellationToken ct) 
    {
        await _lock
            .WaitAsync(ct)
            .ConfigureAwait(false);

        try
        {
            if (!File.Exists(_path))
                return null;
            
            byte[]? encrypted = null;
            byte[]? plaintext = null;

            try
            {
                encrypted = await File
                    .ReadAllBytesAsync(_path, ct)
                    .ConfigureAwait(false);
              
                if (encrypted.Length == 0)
                    throw new InvalidDataException("Empty authentication storage.");

                plaintext = ProtectedData.Unprotect(
                    encrypted, 
                    _entropy, 
                    DataProtectionScope.CurrentUser
                    );

                var envelope = JsonSerializer.Deserialize<Envelope>(plaintext);
                if (envelope?.Version != FormatVersion || envelope.Session == null)
                {
                    throw new InvalidDataException(
                        "Unsupported authentication storage format."
                        );
                }

                return envelope.Session;
            }
            catch (Exception exception)
            when (exception
                is OperationCanceledException
                or IOException 
                or UnauthorizedAccessException 
                )
            {
                throw;
            }
            catch (Exception exception) 
            when (exception 
                is CryptographicException 
                or JsonException 
                or InvalidDataException
                )
            {
                TryDelete(_path);

                return null;
            }
            finally
            {
                if (plaintext != null)
                    CryptographicOperations.ZeroMemory(plaintext);
                if (encrypted != null)
                    CryptographicOperations.ZeroMemory(encrypted);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(
        T value,
        CancellationToken ct
        )
    {
        ArgumentNullException.ThrowIfNull(value);

        await _lock
            .WaitAsync(ct)
            .ConfigureAwait(false);

        try
        {
            var directory = Path.GetDirectoryName(_path)!;

            Directory.CreateDirectory(directory);

            byte[]? plaintext = null;
            byte[]? encrypted = null;

            var temporary = $"{_path}.tmp.{Guid.NewGuid():N}";

            try
            {
                plaintext = JsonSerializer.SerializeToUtf8Bytes(
                    new Envelope(FormatVersion, value)
                    );

                encrypted = ProtectedData.Protect(
                    plaintext,
                    _entropy,
                    DataProtectionScope.CurrentUser
                    );

                await using (var stream = new FileStream(
                    temporary,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.WriteThrough | FileOptions.Asynchronous)
                    )
                {
                    await stream
                        .WriteAsync(encrypted, ct)
                        .ConfigureAwait(false);

                    await stream
                        .FlushAsync(ct)
                        .ConfigureAwait(false);
                }

                if (File.Exists(_path))
                    File.Replace(temporary, _path, null);
                else
                    File.Move(temporary, _path);
            }
            finally
            {
                TryDelete(temporary);

                if (plaintext != null)
                    CryptographicOperations.ZeroMemory(plaintext);

                if (encrypted != null)
                    CryptographicOperations.ZeroMemory(encrypted);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAsync(CancellationToken ct)
    {
        await _lock
            .WaitAsync(ct)
            .ConfigureAwait(false);

        try
        {
            TryDelete(_path);
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region Private methods

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        { }
        catch (UnauthorizedAccessException)
        { }
    }

    #endregion
}

using HttpConnection.Helpers;
using System.Security.Cryptography;
using System.Text;

namespace HttpConnection.OAuth;

internal sealed record PkceSession(
    string CodeVerifier,
    string CodeChallenge,
    string State,
    string Nonce
    )
{
    public static PkceSession Create()
    {
        string verifier = Base64UrlEncoding
            .Encode(RandomNumberGenerator.GetBytes(64));

        string challenge = Base64UrlEncoding
            .Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        string state = Base64UrlEncoding
            .Encode(RandomNumberGenerator.GetBytes(32));

        string nonce = Base64UrlEncoding
            .Encode(RandomNumberGenerator.GetBytes(32));

        return new PkceSession(
            verifier,
            challenge,
            state,
            nonce
            );
    }
}

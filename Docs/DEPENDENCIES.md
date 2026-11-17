# Package dependencies

HttpConnection keeps OAuth inside the library. Its same-solution projects are
OAuth.Contracts and SecureStorage. They are separate packages; dotnet pack turns
project references into package dependencies rather than embedding their DLLs.
XmlTypedSettings is consumed as a versioned package from the workspace NuGet directory.

Package order: XmlTypedSettings, OAuth.Contracts, SecureStorage, HttpConnection.
The initial package versions are 0.1.0. Public API names, assembly names, framework
targets, OAuth behavior, proxy behavior and DPAPI type identities are unchanged.

There are no references to Templates source projects. Third-party packages still
require nuget.org or an existing package cache. XmlTypedSettings retains its
Microsoft.AspNetCore.App framework reference; this extraction does not change it.

## .NET 10 tool compatibility

HttpConnection and SecureStorage 0.1.1 target net9.0, so .NET 9 and .NET 10
consumers can reference them. DPAPI, its factory and AddSecureStorage remain
Windows-only via SupportedOSPlatform attributes. OAuth stays inside HttpConnection;
public APIs, namespaces and persisted formats are unchanged. Version 0.1.0 remains
in the feed. Build the two new packages with build/Pack-HttpConnection.ps1.

# HttpConnection

Shared Windows HTTP infrastructure for ExampleWPFClient, ArtifactBuildTool and Updater.
The project retains its OAuth implementation, OAuth.Contracts, SecureStorage and
XmlTypedSettings dependencies. OAuth is enabled only when an OAuth service is
explicitly supplied or registered by the application's existing composition root.

`ConnectionClientFactory.Create(Uri, TimeSpan, ...)` creates a caller-owned client
with a base server URL. `Create(TimeSpan, ...)` leaves BaseAddress unset for callers
such as Updater that send absolute URLs to several product servers. Both accept
optional `ProxySettings`. A null configuration preserves native .NET proxy behavior;
an explicit configuration selects disabled, system or manually configured proxy mode.

Updater and ArtifactBuildTool do not register OAuth handlers. Their application
configuration is documented in their respective `Docs/PROXY.md` files. The WPF
client keeps its existing XML settings and OAuth registration flow.

HTTP redirects and cookies are disabled. Default TLS certificate validation is
retained. Dispose caller-owned clients; clients created by a DI singleton factory
are disposed by the container.

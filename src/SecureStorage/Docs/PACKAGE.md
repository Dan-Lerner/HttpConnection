# SecureStorage

Windows DPAPI-protected per-user storage for typed application data.

Package version: 0.1.1; target framework: net9.0.

This package preserves the original public namespaces and runtime behavior from Templates.
It contains no application-specific configuration, credentials or user data.

A platform-neutral TFM allows .NET 9/10 consumers. DPAPI storage and its DI registration
remain Windows-only APIs; this release preserves namespaces and persisted data formats.

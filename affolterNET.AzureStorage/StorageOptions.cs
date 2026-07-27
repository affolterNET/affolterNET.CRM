namespace affolterNET.AzureStorage;

/// <summary>
/// Configuration options for Azure Storage access.
/// Priority:
/// 1. ConnectionString - if set, uses connection string auth (Azurite or production)
/// 2. StorageClientId - if set, uses ManagedIdentityCredential with this user-assigned client ID
/// 3. Otherwise - uses DefaultAzureCredential (Azure CLI, VS, etc.)
/// </summary>
public class StorageOptions
{
    /// <summary>Default configuration section; overridable per consumer via the DI options.</summary>
    public const string DefaultSectionName = "affolterNET:AzureStorage";

    /// <summary>
    /// Connection string for Azure Storage or the Azurite emulator.
    /// If set, this takes priority over managed identity authentication.
    /// Use "UseDevelopmentStorage=true" for local Azurite.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The client ID of the user-assigned managed identity for storage access.
    /// If set (and ConnectionString is not set), uses ManagedIdentityCredential with this client ID.
    /// </summary>
    public string StorageClientId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the Azure Storage account (without .blob.core.windows.net suffix).
    /// Required when using managed identity or default credential authentication.
    /// </summary>
    public string AccountName { get; set; } = string.Empty;
}

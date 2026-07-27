using Xunit;

namespace affolterNET.CRM.Tests;

/// <summary>
/// Integration tests run only when CRM_TEST_STORAGE holds a storage connection string
/// (point it at a dedicated TEST Azurite — never at a working instance).
/// </summary>
public sealed class AzuriteFactAttribute : FactAttribute
{
    public const string EnvVar = "CRM_TEST_STORAGE";

    public AzuriteFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvVar)))
        {
            Skip = $"Set {EnvVar} to a test-Azurite connection string to run storage integration tests.";
        }
    }
}

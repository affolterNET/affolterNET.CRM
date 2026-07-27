using affolterNET.CRM.Core.Storage;
using affolterNET.CRM.Entities;
using Xunit;

namespace affolterNET.CRM.Tests;

public class StorageRegistryTests
{
    private sealed class ExtendedPerson : PersonEntity
    {
        public string Language { get; set; } = string.Empty;
    }

    [Fact]
    public void TableNameFor_RegisteredType_Resolves()
    {
        var registry = new StorageRegistry("", new Dictionary<Type, string> { [typeof(PersonEntity)] = "persons" }, []);

        Assert.Equal("persons", registry.TableNameFor(typeof(PersonEntity)));
    }

    [Fact]
    public void TableNameFor_SubclassOfRegisteredBase_ResolvesToBaseTable()
    {
        var registry = new StorageRegistry("", new Dictionary<Type, string> { [typeof(PersonEntity)] = "persons" }, []);

        Assert.Equal("persons", registry.TableNameFor(typeof(ExtendedPerson)));
    }

    [Fact]
    public void TableNameFor_Unregistered_Throws()
    {
        var registry = new StorageRegistry("", new Dictionary<Type, string>(), []);

        Assert.Throws<InvalidOperationException>(() => registry.TableNameFor(typeof(PersonEntity)));
    }

    [Fact]
    public void TablePrefix_IsApplied()
    {
        var registry = new StorageRegistry("t1", new Dictionary<Type, string> { [typeof(PersonEntity)] = "persons" }, []);

        Assert.Equal("t1persons", registry.TableNameFor(typeof(PersonEntity)));
        Assert.Equal(["t1persons"], registry.TableNames);
    }
}

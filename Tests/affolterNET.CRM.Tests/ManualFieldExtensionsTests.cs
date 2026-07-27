using affolterNET.CRM.Core.Entities;
using affolterNET.CRM.Entities;
using Xunit;

namespace affolterNET.CRM.Tests;

public class ManualFieldExtensionsTests
{
    [Fact]
    public void MarkFieldManual_IsIdempotent()
    {
        var person = new PersonEntity();
        person.MarkFieldManual("Salutation");
        person.MarkFieldManual("Salutation");
        person.MarkFieldManual("Email");

        Assert.Equal("Salutation;Email", person.ManualFields);
        Assert.True(person.IsFieldManual("Salutation"));
        Assert.True(person.IsFieldManual("Email"));
        Assert.False(person.IsFieldManual("Phone"));
    }

    [Fact]
    public void SetUnlessManual_SkipsProtectedField()
    {
        var person = new PersonEntity { Salutation = "Frau" };
        person.MarkFieldManual(nameof(PersonEntity.Salutation));

        person.SetUnlessManual(nameof(PersonEntity.Salutation), () => person.Salutation = "Herr");
        Assert.Equal("Frau", person.Salutation);

        person.SetUnlessManual(nameof(PersonEntity.Email), () => person.Email = "a@b.ch");
        Assert.Equal("a@b.ch", person.Email);
    }

    [Fact]
    public void IsFieldManual_NoSubstringMatches()
    {
        var person = new PersonEntity();
        person.MarkFieldManual("PartnerName");

        Assert.False(person.IsFieldManual("Name"));
        Assert.False(person.IsFieldManual("Partner"));
    }

    [Fact]
    public void MarkFieldManual_SeparatorInName_Throws()
    {
        var person = new PersonEntity();
        Assert.Throws<ArgumentException>(() => person.MarkFieldManual("a;b"));
    }
}

using affolterNET.CRM.Configuration;
using affolterNET.CRM.Entities;
using Xunit;

namespace affolterNET.CRM.Tests;

public class EntityKeyTests
{
    private static readonly RoleDefinition President = new("president", HasPeriod: true, ExclusivePerOrg: true);
    private static readonly RoleDefinition Member = new("member", HasPeriod: false, ExclusivePerOrg: false);

    [Fact]
    public void BuildPersonKey_WithBirthdate_IncludesIt()
    {
        Assert.Equal("mueller-peter-1961-04-07", PersonEntity.BuildPersonKey("Müller", "Peter", "1961-04-07"));
    }

    [Fact]
    public void BuildPersonKey_WithoutBirthdate_NameOnly()
    {
        Assert.Equal("mueller-peter", PersonEntity.BuildPersonKey("Müller", "Peter"));
        Assert.Equal("mueller-peter", PersonEntity.BuildPersonKey("Müller", "Peter", "  "));
    }

    [Fact]
    public void RoleKeys_PeriodicExclusive()
    {
        Assert.Equal("president_2026-2027", RoleEntity.BuildPartitionKey(President, "2026-2027"));
        Assert.Equal("thun-niesen", RoleEntity.BuildRowKey(President, "thun-niesen", "mueller-peter"));
    }

    [Fact]
    public void RoleKeys_PeriodlessNonExclusive()
    {
        Assert.Equal("member", RoleEntity.BuildPartitionKey(Member, null));
        Assert.Equal("thun-niesen_mueller-peter", RoleEntity.BuildRowKey(Member, "thun-niesen", "mueller-peter"));
    }

    [Fact]
    public void RoleKeys_MissingPeriodForPeriodicRole_Throws()
    {
        Assert.Throws<ArgumentException>(() => RoleEntity.BuildPartitionKey(President, null));
    }

    [Theory]
    [InlineData("org_id", "person")]
    [InlineData("org", "person_id")]
    public void RoleKeys_SeparatorInComponents_Throws(string orgId, string personId)
    {
        Assert.Throws<ArgumentException>(() => RoleEntity.BuildRowKey(Member, orgId, personId));
    }

    [Fact]
    public void AddressOwnerKeys()
    {
        Assert.Equal("person_mueller-peter", AddressEntity.ForPerson("mueller-peter"));
        Assert.Equal("org_club_thun-niesen", AddressEntity.ForOrganization("club", "thun-niesen"));
    }

    [Theory]
    [InlineData("Hans Peter", "hans")]
    [InlineData("André", "andre")]
    [InlineData("  Vera  ", "vera")]
    public void FirstnameNormalize_FirstTokenSlugged(string input, string expected)
    {
        Assert.True(FirstnameSexEntity.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    public void FirstnameNormalize_Unusable_ReturnsFalse(string input)
    {
        Assert.False(FirstnameSexEntity.TryNormalize(input, out _));
    }
}

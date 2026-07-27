namespace affolterNET.CRM.Entities;

/// <summary>Values for <see cref="PersonEntity.Sex"/> and <see cref="FirstnameSexEntity.Sex"/>.</summary>
public static class Sexes
{
    public const string Male = "male";
    public const string Female = "female";

    public static readonly string[] All = [Male, Female];

    public static bool IsValid(string sex) => All.Contains(sex, StringComparer.Ordinal);
}

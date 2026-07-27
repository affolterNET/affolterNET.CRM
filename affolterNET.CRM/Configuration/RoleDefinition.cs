namespace affolterNET.CRM.Configuration;

/// <summary>
/// Declares a role type and its key semantics.
/// <para><c>HasPeriod</c>: the role is scoped to a period (e.g. a Rotary year "2026-2027");
/// periods are opaque strings that must sort ordinally.</para>
/// <para><c>ExclusivePerOrg</c>: at most one holder per organization (and period) — e.g. a
/// president or governor; assigning a new holder replaces the old one. Non-exclusive roles
/// (member, board-member) accumulate one row per person.</para>
/// </summary>
public sealed record RoleDefinition(string RoleType, bool HasPeriod, bool ExclusivePerOrg);

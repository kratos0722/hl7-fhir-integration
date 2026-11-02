namespace Hl7FhirIntegration.Auth;

public static class Roles
{
    public const string Clinician = "clinician";
    public const string Admin = "admin";
}

public enum AuthDecision
{
    Allowed,
    Unauthenticated,
    Forbidden,
}

/// <summary>
/// Pure role-based access decision for PHI endpoints. Kept free of HTTP so the
/// policy is unit-tested directly; an endpoint filter applies it per request.
/// </summary>
public static class Authorization
{
    private static readonly IReadOnlyDictionary<string, string[]> Grants =
        new Dictionary<string, string[]>
        {
            [Roles.Clinician] = ["patient:read", "patient:write"],
            [Roles.Admin] = ["patient:read", "patient:write", "audit:read"],
        };

    public static AuthDecision Check(string? role, string permission)
    {
        if (string.IsNullOrWhiteSpace(role))
            return AuthDecision.Unauthenticated;
        if (!Grants.TryGetValue(role.Trim().ToLowerInvariant(), out var perms))
            return AuthDecision.Forbidden;
        return perms.Contains(permission) ? AuthDecision.Allowed : AuthDecision.Forbidden;
    }
}

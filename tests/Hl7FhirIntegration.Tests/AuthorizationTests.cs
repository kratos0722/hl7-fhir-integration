using Hl7FhirIntegration.Auth;

namespace Hl7FhirIntegration.Tests;

public class AuthorizationTests
{
    [Theory]
    [InlineData("clinician", "patient:read", AuthDecision.Allowed)]
    [InlineData("clinician", "patient:write", AuthDecision.Allowed)]
    [InlineData("clinician", "audit:read", AuthDecision.Forbidden)]
    [InlineData("admin", "patient:read", AuthDecision.Allowed)]
    [InlineData("admin", "audit:read", AuthDecision.Allowed)]
    [InlineData("ADMIN", "audit:read", AuthDecision.Allowed)]  // case-insensitive
    [InlineData("intruder", "patient:read", AuthDecision.Forbidden)]
    public void Check_returns_expected_decision(string role, string permission, AuthDecision expected)
    {
        Assert.Equal(expected, Authorization.Check(role, permission));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_role_is_unauthenticated(string? role)
    {
        Assert.Equal(AuthDecision.Unauthenticated, Authorization.Check(role, "patient:read"));
    }
}

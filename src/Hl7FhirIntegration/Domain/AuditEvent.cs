namespace Hl7FhirIntegration.Domain;

/// <summary>An access/modification of PHI, recorded for audit.</summary>
public sealed record AuditEvent(
    DateTimeOffset Timestamp,
    string Actor,
    string Action,
    string PatientMrn);

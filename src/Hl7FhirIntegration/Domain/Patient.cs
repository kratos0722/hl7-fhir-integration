namespace Hl7FhirIntegration.Domain;

/// <summary>
/// Canonical internal patient model. Both HL7 v2 and FHIR inputs map onto this
/// single representation so the rest of the system is format-agnostic.
/// </summary>
public sealed class Patient
{
    public required string Mrn { get; init; }
    public required string Family { get; init; }
    public required string Given { get; init; }
    public string? BirthDate { get; init; }   // ISO yyyy-MM-dd
    public string Gender { get; init; } = "U"; // HL7 administrative sex: M/F/O/U
}

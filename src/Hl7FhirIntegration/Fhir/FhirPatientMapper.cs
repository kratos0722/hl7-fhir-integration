using System.Text.Json;
using System.Text.Json.Nodes;
using Hl7FhirIntegration.Domain;

namespace Hl7FhirIntegration.Fhir;

public sealed class FhirMappingException(string message) : Exception(message);

/// <summary>
/// Maps between the canonical <see cref="Patient"/> and a FHIR R4 Patient
/// resource (JSON). Gender is translated between HL7 administrative sex codes
/// and FHIR's gender value set.
/// </summary>
public static class FhirPatientMapper
{
    public static string ToFhirJson(Patient p)
    {
        var resource = new JsonObject
        {
            ["resourceType"] = "Patient",
            ["id"] = p.Mrn,
            ["identifier"] = new JsonArray(new JsonObject
            {
                ["system"] = "urn:oid:mrn",
                ["value"] = p.Mrn,
            }),
            ["name"] = new JsonArray(new JsonObject
            {
                ["family"] = p.Family,
                ["given"] = new JsonArray(p.Given),
            }),
            ["gender"] = ToFhirGender(p.Gender),
        };
        if (p.BirthDate is not null)
            resource["birthDate"] = p.BirthDate;

        return resource.ToJsonString();
    }

    public static Patient FromFhirJson(string json)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FhirMappingException($"Invalid JSON: {ex.Message}");
        }

        if (root is null || (string?)root["resourceType"] != "Patient")
            throw new FhirMappingException("Resource is not a FHIR Patient");

        var mrn = (string?)root["identifier"]?[0]?["value"];
        if (string.IsNullOrEmpty(mrn))
            throw new FhirMappingException("Patient has no identifier value");

        var name = root["name"]?[0];
        var family = (string?)name?["family"] ?? "";
        var given = (string?)name?["given"]?[0] ?? "";

        return new Patient
        {
            Mrn = mrn,
            Family = family,
            Given = given,
            BirthDate = (string?)root["birthDate"],
            Gender = FromFhirGender((string?)root["gender"]),
        };
    }

    private static string ToFhirGender(string hl7) => hl7 switch
    {
        "M" => "male",
        "F" => "female",
        "O" => "other",
        _ => "unknown",
    };

    private static string FromFhirGender(string? fhir) => fhir switch
    {
        "male" => "M",
        "female" => "F",
        "other" => "O",
        _ => "U",
    };
}

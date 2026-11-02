using System.Text.Json.Nodes;
using Hl7FhirIntegration.Domain;
using Hl7FhirIntegration.Fhir;

namespace Hl7FhirIntegration.Tests;

public class FhirPatientMapperTests
{
    private static Patient Sample() => new()
    {
        Mrn = "MRN1",
        Family = "Doe",
        Given = "John",
        BirthDate = "1980-01-01",
        Gender = "M",
    };

    [Fact]
    public void ToFhirJson_emits_patient_resource()
    {
        var json = JsonNode.Parse(FhirPatientMapper.ToFhirJson(Sample()))!;
        Assert.Equal("Patient", (string?)json["resourceType"]);
        Assert.Equal("MRN1", (string?)json["identifier"]![0]!["value"]);
        Assert.Equal("Doe", (string?)json["name"]![0]!["family"]);
        Assert.Equal("John", (string?)json["name"]![0]!["given"]![0]);
        Assert.Equal("male", (string?)json["gender"]);
        Assert.Equal("1980-01-01", (string?)json["birthDate"]);
    }

    [Fact]
    public void Round_trip_preserves_patient()
    {
        var original = Sample();
        var restored = FhirPatientMapper.FromFhirJson(FhirPatientMapper.ToFhirJson(original));
        Assert.Equal(original.Mrn, restored.Mrn);
        Assert.Equal(original.Family, restored.Family);
        Assert.Equal(original.Given, restored.Given);
        Assert.Equal(original.BirthDate, restored.BirthDate);
        Assert.Equal(original.Gender, restored.Gender);
    }

    [Fact]
    public void FromFhirJson_rejects_non_patient()
    {
        Assert.Throws<FhirMappingException>(() =>
            FhirPatientMapper.FromFhirJson("{\"resourceType\":\"Observation\"}"));
    }

    [Fact]
    public void FromFhirJson_rejects_invalid_json()
    {
        Assert.Throws<FhirMappingException>(() => FhirPatientMapper.FromFhirJson("{not json"));
    }
}

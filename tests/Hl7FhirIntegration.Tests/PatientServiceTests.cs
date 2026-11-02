using Hl7FhirIntegration.Infrastructure;
using Hl7FhirIntegration.Services;

namespace Hl7FhirIntegration.Tests;

public class PatientServiceTests
{
    private const string Adt =
        "MSH|^~\\&|S|F|R|F|20260101||ADT^A01|M1|P|2.5\r" +
        "PID|1||MRN9^^^FAC^MR||Smith^Alice||19910303|F";

    private static PatientService NewService(out InMemoryAuditRepository audit)
    {
        audit = new InMemoryAuditRepository();
        return new PatientService(new InMemoryPatientRepository(), audit);
    }

    [Fact]
    public void IngestHl7_persists_and_audits()
    {
        var svc = NewService(out var audit);
        var p = svc.IngestHl7(Adt, "nurse1");

        Assert.Equal("MRN9", p.Mrn);
        Assert.Equal("Alice", p.Given);
        var entry = Assert.Single(audit.All());
        Assert.Equal("INGEST_HL7", entry.Action);
        Assert.Equal("nurse1", entry.Actor);
        Assert.Equal("MRN9", entry.PatientMrn);
    }

    [Fact]
    public void Get_returns_patient_and_records_read()
    {
        var svc = NewService(out var audit);
        svc.IngestHl7(Adt, "nurse1");

        var fetched = svc.Get("MRN9", "doctor2");
        Assert.Equal("Smith", fetched.Family);
        Assert.Contains(audit.All(), e => e.Action == "READ" && e.Actor == "doctor2");
    }

    [Fact]
    public void Get_unknown_throws()
    {
        var svc = NewService(out _);
        Assert.Throws<PatientNotFoundException>(() => svc.Get("NOPE", "x"));
    }

    [Fact]
    public void IngestFhir_persists()
    {
        var svc = NewService(out _);
        var json = "{\"resourceType\":\"Patient\",\"identifier\":[{\"value\":\"MRN7\"}]," +
                   "\"name\":[{\"family\":\"Lee\",\"given\":[\"Sam\"]}],\"gender\":\"female\"}";
        var p = svc.IngestFhir(json, "importer");
        Assert.Equal("MRN7", p.Mrn);
        Assert.Equal("F", p.Gender);
        Assert.Equal("Lee", svc.Get("MRN7", "importer").Family);
    }
}

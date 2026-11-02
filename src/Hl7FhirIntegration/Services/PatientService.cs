using Hl7FhirIntegration.Domain;
using Hl7FhirIntegration.Fhir;
using Hl7FhirIntegration.Hl7;

namespace Hl7FhirIntegration.Services;

public sealed class PatientNotFoundException(string mrn)
    : Exception($"No patient with MRN '{mrn}'");

/// <summary>
/// Ingests patients from HL7 or FHIR and reads them back, writing an audit
/// event for every PHI touch (minimum-necessary access, fully logged).
/// </summary>
public sealed class PatientService(IPatientRepository patients, IAuditRepository audit, TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public Patient IngestHl7(string message, string actor)
    {
        var patient = Hl7Parser.ParsePatient(message);
        patients.Upsert(patient);
        Log(actor, "INGEST_HL7", patient.Mrn);
        return patient;
    }

    public Patient IngestFhir(string json, string actor)
    {
        var patient = FhirPatientMapper.FromFhirJson(json);
        patients.Upsert(patient);
        Log(actor, "INGEST_FHIR", patient.Mrn);
        return patient;
    }

    public Patient Get(string mrn, string actor)
    {
        var patient = patients.Get(mrn) ?? throw new PatientNotFoundException(mrn);
        Log(actor, "READ", mrn);
        return patient;
    }

    public IReadOnlyList<AuditEvent> AuditTrail() => audit.All();

    private void Log(string actor, string action, string mrn) =>
        audit.Record(new AuditEvent(_clock.GetUtcNow(), actor, action, mrn));
}

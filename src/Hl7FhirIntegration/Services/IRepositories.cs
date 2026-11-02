using Hl7FhirIntegration.Domain;

namespace Hl7FhirIntegration.Services;

public interface IPatientRepository
{
    void Upsert(Patient patient);
    Patient? Get(string mrn);
}

public interface IAuditRepository
{
    void Record(AuditEvent entry);
    IReadOnlyList<AuditEvent> All();
}

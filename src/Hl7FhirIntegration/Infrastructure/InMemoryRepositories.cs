using System.Collections.Concurrent;
using Hl7FhirIntegration.Domain;
using Hl7FhirIntegration.Services;

namespace Hl7FhirIntegration.Infrastructure;

public sealed class InMemoryPatientRepository : IPatientRepository
{
    private readonly ConcurrentDictionary<string, Patient> _byMrn = new();

    public void Upsert(Patient patient) => _byMrn[patient.Mrn] = patient;

    public Patient? Get(string mrn) => _byMrn.TryGetValue(mrn, out var p) ? p : null;
}

public sealed class InMemoryAuditRepository : IAuditRepository
{
    private readonly List<AuditEvent> _entries = new();
    private readonly object _lock = new();

    public void Record(AuditEvent entry)
    {
        lock (_lock) _entries.Add(entry);
    }

    public IReadOnlyList<AuditEvent> All()
    {
        lock (_lock) return _entries.ToList();
    }
}

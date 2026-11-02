using Hl7FhirIntegration.Domain;

namespace Hl7FhirIntegration.Hl7;

public sealed class Hl7ParseException(string message) : Exception(message);

/// <summary>
/// Minimal HL7 v2 ADT parser. Reads the MSH and PID segments of an ADT message
/// and projects them onto the canonical <see cref="Patient"/>. Deliberately
/// dependency-free and strict: a message without MSH/PID is rejected.
/// </summary>
public static class Hl7Parser
{
    public static Patient ParsePatient(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new Hl7ParseException("Empty HL7 message");

        var segments = message.Replace("\r\n", "\r").Replace("\n", "\r")
            .Split('\r', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0 || !segments[0].StartsWith("MSH"))
            throw new Hl7ParseException("Missing MSH segment");

        var pid = Array.Find(segments, s => s.StartsWith("PID"))
            ?? throw new Hl7ParseException("Missing PID segment");

        var f = pid.Split('|');
        // PID-3 identifier list: MRN is the first component before '^'.
        var mrn = Component(Field(f, 3), 0);
        if (string.IsNullOrEmpty(mrn))
            throw new Hl7ParseException("Missing patient identifier (PID-3)");

        // PID-5 name: family^given^middle
        var name = Field(f, 5);
        var family = Component(name, 0);
        var given = Component(name, 1);

        var birth = Field(f, 7);
        var gender = Field(f, 8) is { Length: > 0 } g ? g.ToUpperInvariant() : "U";

        return new Patient
        {
            Mrn = mrn,
            Family = family,
            Given = given,
            BirthDate = FormatBirthDate(birth),
            Gender = gender is "M" or "F" or "O" or "U" ? gender : "U",
        };
    }

    private static string Field(string[] fields, int index) =>
        index < fields.Length ? fields[index].Trim() : "";

    private static string Component(string field, int index)
    {
        var parts = field.Split('^');
        return index < parts.Length ? parts[index].Trim() : "";
    }

    private static string? FormatBirthDate(string yyyymmdd)
    {
        if (yyyymmdd.Length < 8 || !long.TryParse(yyyymmdd[..8], out _))
            return null;
        return $"{yyyymmdd[..4]}-{yyyymmdd.Substring(4, 2)}-{yyyymmdd.Substring(6, 2)}";
    }
}

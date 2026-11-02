using Hl7FhirIntegration.Hl7;

namespace Hl7FhirIntegration.Tests;

public class Hl7ParserTests
{
    private const string AdtMessage =
        "MSH|^~\\&|SENDER|FAC|RECV|FAC|20260101120000||ADT^A01|MSG0001|P|2.5\r" +
        "PID|1||MRN12345^^^FAC^MR||Doe^John^A||19800101|M";

    [Fact]
    public void Parses_patient_fields()
    {
        var p = Hl7Parser.ParsePatient(AdtMessage);
        Assert.Equal("MRN12345", p.Mrn);
        Assert.Equal("Doe", p.Family);
        Assert.Equal("John", p.Given);
        Assert.Equal("1980-01-01", p.BirthDate);
        Assert.Equal("M", p.Gender);
    }

    [Fact]
    public void Missing_msh_throws()
    {
        Assert.Throws<Hl7ParseException>(() => Hl7Parser.ParsePatient("PID|1||X^^^FAC^MR||Doe^John"));
    }

    [Fact]
    public void Missing_pid_throws()
    {
        Assert.Throws<Hl7ParseException>(() =>
            Hl7Parser.ParsePatient("MSH|^~\\&|S|F|R|F|20260101||ADT^A01|M1|P|2.5"));
    }

    [Fact]
    public void Missing_identifier_throws()
    {
        Assert.Throws<Hl7ParseException>(() =>
            Hl7Parser.ParsePatient("MSH|^~\\&|S|F|R|F|20260101||ADT^A01|M1|P|2.5\rPID|1||||Doe^John"));
    }

    [Fact]
    public void Unknown_gender_defaults_to_u()
    {
        var p = Hl7Parser.ParsePatient(
            "MSH|^~\\&|S|F|R|F|20260101||ADT^A01|M1|P|2.5\rPID|1||MRN1^^^FAC^MR||Doe^Jane||19900202|X");
        Assert.Equal("U", p.Gender);
    }
}

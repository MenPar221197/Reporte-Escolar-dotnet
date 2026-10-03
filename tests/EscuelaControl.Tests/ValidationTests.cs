using System.ComponentModel.DataAnnotations;
using EscuelaControl.Services;
using EscuelaControl.ViewModels;
using Xunit;

namespace EscuelaControl.Tests;

public class ValidationTests
{
    private static bool Valid(object form) => Validator.TryValidateObject(form, new ValidationContext(form), [], true);

    [Fact]
    public void FamiliaNecesitaAlMenosUnResponsable() => Assert.False(Valid(new FamiliaForm()));

    [Fact]
    public void FamiliaPuedeTenerSoloUnaTutora() => Assert.True(Valid(new FamiliaForm { NombreTutor = "Tutora de prueba" }));

    [Fact]
    public void TelefonoDebeTenerUnResponsable() => Assert.False(Valid(new FamiliaForm { NombreMama = "Madre", TelefonoPapa = "5551234567" }));

    [Fact]
    public void AlumnoNoPuedeNacerEnElFuturo()
    {
        var form = new AlumnoForm { Matricula = "ALU-001", NombreCompleto = "Alumno", Grado = "1", Grupo = "A", FamiliaId = 1, EscuelaId = 1, FechaNacimiento = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1) };
        Assert.False(Valid(form));
        form.FechaNacimiento = new DateOnly(2015, 1, 1);
        Assert.True(Valid(form));
    }

    [Theory]
    [InlineData("=1+1")]
    [InlineData("+SUM(A1)")]
    [InlineData("-1+2")]
    [InlineData("@SUM(A1)")]
    [InlineData("   =1+1")]
    [InlineData("\t=1+1")]
    public void ExportacionNeutralizaFormulas(string value) => Assert.StartsWith("\"'", CsvExporter.Cell(value));

    [Fact]
    public void ExportacionConservaComasComillasYAcentos() => Assert.Equal("\"María, \"\"Luz\"\"\"", CsvExporter.Cell("María, \"Luz\""));
}

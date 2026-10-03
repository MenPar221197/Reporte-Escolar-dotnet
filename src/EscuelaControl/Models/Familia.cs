using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EscuelaControl.Models;

public class Familia : Registro
{
    [StringLength(100)]
    public string? NombrePapa
    {
        get; set;
    }
    [StringLength(20)]
    public string? TelefonoPapa
    {
        get; set;
    }
    [StringLength(100)]
    public string? NombreMama
    {
        get; set;
    }
    [StringLength(20)]
    public string? TelefonoMama
    {
        get; set;
    }
    [StringLength(100)]
    public string? NombreTutor
    {
        get; set;
    }
    [StringLength(20)]
    public string? TelefonoTutor
    {
        get; set;
    }
    [StringLength(150)]
    public string? EmailContacto
    {
        get; set;
    }
    [StringLength(500)]
    public string? Notas
    {
        get; set;
    }
    public ICollection<Alumno> Alumnos { get; set; } = new List<Alumno>();
    [NotMapped] public string Referencia => string.Join(" / ", new[] { NombrePapa, NombreMama, NombreTutor }.Where(n => !string.IsNullOrWhiteSpace(n)));
}

using System.ComponentModel.DataAnnotations;

namespace EscuelaControl.Models;

public enum NivelEducativo
{
    Preescolar, Primaria, Secundaria, Bachillerato, Superior, Otro
}

public class Escuela : Registro
{
    [Required, StringLength(20)] public string Clave { get; set; } = "";
    [Required, StringLength(120)] public string Nombre { get; set; } = "";
    public NivelEducativo Nivel
    {
        get; set;
    }
    [StringLength(20)]
    public string? Telefono
    {
        get; set;
    }
    [StringLength(250)]
    public string? Direccion
    {
        get; set;
    }
    public bool Activa { get; set; } = true;
    public ICollection<Alumno> Alumnos { get; set; } = new List<Alumno>();
}

using System.ComponentModel.DataAnnotations;

namespace EscuelaControl.Models;

public class Alumno : Registro
{
    [Required, StringLength(20)] public string Matricula { get; set; } = "";
    [Required, StringLength(120)] public string NombreCompleto { get; set; } = "";
    public DateOnly FechaNacimiento
    {
        get; set;
    }
    [Required, StringLength(30)] public string Grado { get; set; } = "";
    [Required, StringLength(15)] public string Grupo { get; set; } = "";
    public bool Activo { get; set; } = true;
    public int FamiliaId
    {
        get; set;
    }
    public Familia Familia { get; set; } = null!;
    public int EscuelaId
    {
        get; set;
    }
    public Escuela Escuela { get; set; } = null!;
}

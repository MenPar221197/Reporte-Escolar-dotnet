using System.ComponentModel.DataAnnotations;
using EscuelaControl.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EscuelaControl.ViewModels;

public class EscuelaForm
{
    public int Id
    {
        get; set;
    }
    public Guid Version
    {
        get; set;
    }
    [Required(ErrorMessage = "Escribe la clave de la escuela."), StringLength(20), RegularExpression(@"[A-Za-z0-9-]{3,20}", ErrorMessage = "Usa de 3 a 20 letras, números o guiones."), Display(Name = "Clave / CCT")]
    public string Clave { get; set; } = "";
    [Required(ErrorMessage = "Escribe el nombre de la escuela."), StringLength(120), Display(Name = "Nombre de la escuela")]
    public string Nombre { get; set; } = "";
    [EnumDataType(typeof(NivelEducativo)), Display(Name = "Nivel educativo")]
    public NivelEducativo Nivel
    {
        get; set;
    }
    [StringLength(20), RegularExpression(@"\+?[0-9 ()-]{7,20}", ErrorMessage = "Escribe un teléfono válido de 7 a 20 caracteres."), Display(Name = "Teléfono")]
    public string? Telefono
    {
        get; set;
    }
    [StringLength(250), Display(Name = "Dirección")]
    public string? Direccion
    {
        get; set;
    }
    [Display(Name = "Escuela activa")]
    public bool Activa { get; set; } = true;
}

public class FamiliaForm : IValidatableObject
{
    public int Id
    {
        get; set;
    }
    public Guid Version
    {
        get; set;
    }
    [StringLength(100), Display(Name = "Nombre del padre")]
    public string? NombrePapa
    {
        get; set;
    }
    [StringLength(20), RegularExpression(@"\+?[0-9 ()-]{7,20}", ErrorMessage = "Escribe un teléfono válido."), Display(Name = "Teléfono del padre")]
    public string? TelefonoPapa
    {
        get; set;
    }
    [StringLength(100), Display(Name = "Nombre de la madre")]
    public string? NombreMama
    {
        get; set;
    }
    [StringLength(20), RegularExpression(@"\+?[0-9 ()-]{7,20}", ErrorMessage = "Escribe un teléfono válido."), Display(Name = "Teléfono de la madre")]
    public string? TelefonoMama
    {
        get; set;
    }
    [StringLength(100), Display(Name = "Nombre del tutor o tutora")]
    public string? NombreTutor
    {
        get; set;
    }
    [StringLength(20), RegularExpression(@"\+?[0-9 ()-]{7,20}", ErrorMessage = "Escribe un teléfono válido."), Display(Name = "Teléfono del tutor")]
    public string? TelefonoTutor
    {
        get; set;
    }
    [EmailAddress(ErrorMessage = "Escribe un correo válido."), StringLength(150), Display(Name = "Correo de contacto")]
    public string? EmailContacto
    {
        get; set;
    }
    [StringLength(500), Display(Name = "Notas de contacto")]
    public string? Notas
    {
        get; set;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (new[] { NombrePapa, NombreMama, NombreTutor }.All(string.IsNullOrWhiteSpace))
        {
            yield return new ValidationResult("Registra al menos a la madre, al padre o a un tutor.");
        }

        if (!string.IsNullOrWhiteSpace(TelefonoPapa) && string.IsNullOrWhiteSpace(NombrePapa))
        {
            yield return new ValidationResult("Indica a quién pertenece el teléfono del padre.", [nameof(NombrePapa)]);
        }

        if (!string.IsNullOrWhiteSpace(TelefonoMama) && string.IsNullOrWhiteSpace(NombreMama))
        {
            yield return new ValidationResult("Indica a quién pertenece el teléfono de la madre.", [nameof(NombreMama)]);
        }

        if (!string.IsNullOrWhiteSpace(TelefonoTutor) && string.IsNullOrWhiteSpace(NombreTutor))
        {
            yield return new ValidationResult("Indica el nombre del tutor.", [nameof(NombreTutor)]);
        }
    }
}

public class AlumnoForm : IValidatableObject
{
    public int Id
    {
        get; set;
    }
    public Guid Version
    {
        get; set;
    }
    [Required(ErrorMessage = "Escribe la matrícula."), StringLength(20), RegularExpression(@"[A-Za-z0-9-]{3,20}", ErrorMessage = "Usa de 3 a 20 letras, números o guiones."), Display(Name = "Matrícula")]
    public string Matricula { get; set; } = "";
    [Required(ErrorMessage = "Escribe el nombre del alumno."), StringLength(120), Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = "";
    [Required(ErrorMessage = "Indica la fecha de nacimiento."), DataType(DataType.Date), Display(Name = "Fecha de nacimiento")]
    public DateOnly? FechaNacimiento
    {
        get; set;
    }
    [Required(ErrorMessage = "Indica el grado."), StringLength(30)] public string Grado { get; set; } = "";
    [Required(ErrorMessage = "Indica el grupo."), StringLength(15)] public string Grupo { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una familia."), Display(Name = "Familia")]
    public int FamiliaId
    {
        get; set;
    }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una escuela."), Display(Name = "Escuela")]
    public int EscuelaId
    {
        get; set;
    }
    [Display(Name = "Alumno activo")] public bool Activo { get; set; } = true;
    [ValidateNever] public IEnumerable<SelectListItem> Familias { get; set; } = [];
    [ValidateNever] public IEnumerable<SelectListItem> Escuelas { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (FechaNacimiento is { } date && (date > today || date < today.AddYears(-100)))
        {
            yield return new ValidationResult("La fecha debe estar entre hoy y hace 100 años.", [nameof(FechaNacimiento)]);
        }
    }
}

public class LoginForm
{
    [Required, EmailAddress, Display(Name = "Correo electrónico")] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password), Display(Name = "Contraseña")] public string Password { get; set; } = "";
    public string? ReturnUrl
    {
        get; set;
    }
}

public class CambiarClaveForm
{
    [Required, DataType(DataType.Password), Display(Name = "Contraseña actual")] public string Actual { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12, ErrorMessage = "Usa entre 12 y 128 caracteres."), DataType(DataType.Password), Display(Name = "Nueva contraseña")] public string Nueva { get; set; } = "";
    [Compare(nameof(Nueva), ErrorMessage = "Las contraseñas no coinciden."), DataType(DataType.Password), Display(Name = "Confirmar contraseña")] public string Confirmacion { get; set; } = "";
}

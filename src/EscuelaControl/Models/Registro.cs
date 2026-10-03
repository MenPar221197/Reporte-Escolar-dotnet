using System.ComponentModel.DataAnnotations;

namespace EscuelaControl.Models;

public abstract class Registro
{
    public int Id
    {
        get; set;
    }
    [ConcurrencyCheck]
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadoUtc { get; set; } = DateTime.UtcNow;
}

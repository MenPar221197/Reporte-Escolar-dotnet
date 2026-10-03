using EscuelaControl.Data;
using EscuelaControl.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EscuelaControl.Controllers;

public abstract class CrudController(EscuelaDbContext db) : Controller
{
    protected readonly EscuelaDbContext Db = db;
    protected static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    protected void CheckVersion(Registro entity, Guid version) => Db.Entry(entity).Property(e => e.Version).OriginalValue = version;
    protected async Task<bool> SaveAsync(CancellationToken ct)
    {
        try
        {
            await Db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "Otro usuario cambió o eliminó este registro. Vuelve a la lista y abre la versión actual antes de guardar.");
            return false;
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "No se guardó el cambio. Revisa las claves duplicadas y los registros vinculados; recarga la página e inténtalo de nuevo.");
            return false;
        }
    }
}

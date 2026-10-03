using EscuelaControl.Data;
using EscuelaControl.Models;
using EscuelaControl.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EscuelaControl.Controllers;

public class EscuelasController(EscuelaDbContext db) : CrudController(db)
{
    public async Task<IActionResult> Index(string? q, int page = 1, CancellationToken ct = default)
    {
        q = Clean(q);
        ViewData["Q"] = q;
        var query = Db.Escuelas.AsNoTracking();
        if (q is not null)
        {
            query = query.Where(e => e.Nombre.Contains(q) || e.Clave.Contains(q));
        }

        return View(await PagedList<EscuelaFila>.CreateAsync(query.OrderBy(e => e.Nombre).ThenBy(e => e.Id)
            .Select(e => new EscuelaFila(e.Id, e.Clave, e.Nombre, e.Nivel, e.Activa, e.Alumnos.Count)), page, ct));
    }
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var model = await Db.Escuelas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (model is null)
        {
            return NotFound();
        }

        ViewData["Alumnos"] = await Db.Alumnos.CountAsync(a => a.EscuelaId == id, ct);
        return View(model);
    }
    [HttpGet] public IActionResult Create() => View("Form", new EscuelaForm());
    [HttpPost]
    public async Task<IActionResult> Create(EscuelaForm form, CancellationToken ct)
    {
        form.Id = 0;
        await ValidateAsync(form, ct);
        if (!ModelState.IsValid)
        {
            return View("Form", form);
        }

        var entity = new Escuela();
        Apply(form, entity);
        Db.Escuelas.Add(entity);
        if (!await SaveAsync(ct))
        {
            return View("Form", form);
        }

        TempData["Success"] = "Escuela registrada. Ya puedes asignarle alumnos.";
        return RedirectToAction(nameof(Details), new
        {
            id = entity.Id
        });
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var e = await Db.Escuelas.FindAsync([id], ct);
        return e is null ? NotFound() : View("Form", new EscuelaForm { Id = e.Id, Version = e.Version, Clave = e.Clave, Nombre = e.Nombre, Nivel = e.Nivel, Telefono = e.Telefono, Direccion = e.Direccion, Activa = e.Activa });
    }
    [HttpPost]
    public async Task<IActionResult> Edit(int id, EscuelaForm form, CancellationToken ct)
    {
        if (id != form.Id)
        {
            return BadRequest();
        }

        var e = await Db.Escuelas.FindAsync([id], ct);
        if (e is null)
        {
            return NotFound();
        }

        await ValidateAsync(form, ct);
        if (!ModelState.IsValid)
        {
            return View("Form", form);
        }

        Apply(form, e);
        CheckVersion(e, form.Version);
        if (!await SaveAsync(ct))
        {
            return View("Form", form);
        }

        TempData["Success"] = "Datos de la escuela actualizados.";
        return RedirectToAction(nameof(Details), new
        {
            id
        });
    }
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var e = await Db.Escuelas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (e is null)
        {
            return NotFound();
        }

        ViewData["Relacionados"] = await Db.Alumnos.CountAsync(a => a.EscuelaId == id, ct);
        return View(e);
    }
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, Guid version, CancellationToken ct)
    {
        var e = await Db.Escuelas.FindAsync([id], ct);
        if (e is null)
        {
            return NotFound();
        }

        var count = await Db.Alumnos.CountAsync(a => a.EscuelaId == id, ct);
        ViewData["Relacionados"] = count;
        if (count > 0)
        {
            ModelState.AddModelError("", "Esta escuela tiene alumnos vinculados. Puedes marcarla como inactiva desde Editar.");
            return View(e);
        }
        CheckVersion(e, version);
        Db.Escuelas.Remove(e);
        if (!await SaveAsync(ct))
        {
            return View(e);
        }

        TempData["Success"] = "Escuela eliminada.";
        return RedirectToAction(nameof(Index));
    }
    private async Task ValidateAsync(EscuelaForm f, CancellationToken ct)
    {
        f.Clave = (f.Clave ?? "").Trim().ToUpperInvariant();
        if (await Db.Escuelas.AnyAsync(e => e.Clave == f.Clave && e.Id != f.Id, ct))
        {
            ModelState.AddModelError(nameof(f.Clave), "Ya existe una escuela con esa clave.");
        }
    }
    private static void Apply(EscuelaForm f, Escuela e)
    {
        e.Clave = f.Clave;
        e.Nombre = f.Nombre.Trim();
        e.Nivel = f.Nivel;
        e.Telefono = Clean(f.Telefono);
        e.Direccion = Clean(f.Direccion);
        e.Activa = f.Activa;
    }
}

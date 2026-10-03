using EscuelaControl.Data;
using EscuelaControl.Models;
using EscuelaControl.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EscuelaControl.Controllers;

public class FamiliasController(EscuelaDbContext db) : CrudController(db)
{
    public async Task<IActionResult> Index(string? q, bool sinContacto = false, int page = 1, CancellationToken ct = default)
    {
        q = Clean(q);
        ViewData["Q"] = q;
        ViewData["SinContacto"] = sinContacto;
        var query = Db.Familias.AsNoTracking();
        if (q is not null)
        {
            query = query.Where(f => (f.NombrePapa != null && f.NombrePapa.Contains(q)) || (f.NombreMama != null && f.NombreMama.Contains(q)) || (f.NombreTutor != null && f.NombreTutor.Contains(q)) || (f.EmailContacto != null && f.EmailContacto.Contains(q)));
        }

        if (sinContacto)
        {
            query = query.Where(f => f.TelefonoPapa == null && f.TelefonoMama == null && f.TelefonoTutor == null && f.EmailContacto == null);
        }

        return View(await PagedList<FamiliaFila>.CreateAsync(query.OrderBy(f => f.NombrePapa ?? f.NombreMama ?? f.NombreTutor).ThenBy(f => f.Id)
            .Select(f => new FamiliaFila(f.Id, f.NombrePapa, f.NombreMama, f.NombreTutor, f.TelefonoPapa, f.TelefonoMama, f.TelefonoTutor, f.Alumnos.Count)), page, ct));
    }
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var f = await Db.Familias.AsNoTracking().Include(f => f.Alumnos).ThenInclude(a => a.Escuela).FirstOrDefaultAsync(f => f.Id == id, ct);
        return f is null ? NotFound() : View(f);
    }
    [HttpGet] public IActionResult Create() => View("Form", new FamiliaForm());
    [HttpPost]
    public async Task<IActionResult> Create(FamiliaForm form, CancellationToken ct)
    {
        form.Id = 0;
        if (!ModelState.IsValid)
        {
            return View("Form", form);
        }

        var f = new Familia();
        Apply(form, f);
        Db.Familias.Add(f);
        if (!await SaveAsync(ct))
        {
            return View("Form", form);
        }

        TempData["Success"] = "Familia registrada. Ahora puedes vincular a sus hijos.";
        return RedirectToAction(nameof(Details), new
        {
            id = f.Id
        });
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var f = await Db.Familias.FindAsync([id], ct);
        return f is null ? NotFound() : View("Form", new FamiliaForm { Id = f.Id, Version = f.Version, NombrePapa = f.NombrePapa, TelefonoPapa = f.TelefonoPapa, NombreMama = f.NombreMama, TelefonoMama = f.TelefonoMama, NombreTutor = f.NombreTutor, TelefonoTutor = f.TelefonoTutor, EmailContacto = f.EmailContacto, Notas = f.Notas });
    }
    [HttpPost]
    public async Task<IActionResult> Edit(int id, FamiliaForm form, CancellationToken ct)
    {
        if (id != form.Id)
        {
            return BadRequest();
        }

        var f = await Db.Familias.FindAsync([id], ct);
        if (f is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View("Form", form);
        }

        Apply(form, f);
        CheckVersion(f, form.Version);
        if (!await SaveAsync(ct))
        {
            return View("Form", form);
        }

        TempData["Success"] = "Datos de contacto actualizados.";
        return RedirectToAction(nameof(Details), new
        {
            id
        });
    }
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var f = await Db.Familias.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);
        if (f is null)
        {
            return NotFound();
        }

        ViewData["Relacionados"] = await Db.Alumnos.CountAsync(a => a.FamiliaId == id, ct);
        return View(f);
    }
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, Guid version, CancellationToken ct)
    {
        var f = await Db.Familias.FindAsync([id], ct);
        if (f is null)
        {
            return NotFound();
        }

        var count = await Db.Alumnos.CountAsync(a => a.FamiliaId == id, ct);
        ViewData["Relacionados"] = count;
        if (count > 0)
        {
            ModelState.AddModelError("", "La familia tiene alumnos vinculados. Reasígnalos antes de eliminarla.");
            return View(f);
        }
        CheckVersion(f, version);
        Db.Familias.Remove(f);
        if (!await SaveAsync(ct))
        {
            return View(f);
        }

        TempData["Success"] = "Familia eliminada.";
        return RedirectToAction(nameof(Index));
    }
    private static void Apply(FamiliaForm f, Familia e)
    {
        e.NombrePapa = Clean(f.NombrePapa);
        e.TelefonoPapa = Clean(f.TelefonoPapa);
        e.NombreMama = Clean(f.NombreMama);
        e.TelefonoMama = Clean(f.TelefonoMama);
        e.NombreTutor = Clean(f.NombreTutor);
        e.TelefonoTutor = Clean(f.TelefonoTutor);
        e.EmailContacto = Clean(f.EmailContacto);
        e.Notas = Clean(f.Notas);
    }
}

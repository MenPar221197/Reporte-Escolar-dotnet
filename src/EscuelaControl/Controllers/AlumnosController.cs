using EscuelaControl.Data;
using EscuelaControl.Models;
using EscuelaControl.Services;
using EscuelaControl.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EscuelaControl.Controllers;

public class AlumnosController(EscuelaDbContext db) : CrudController(db)
{
    private IQueryable<Alumno> Filter(string? q, int? escuelaId, string? estado)
    {
        q = Clean(q);
        var query = Db.Alumnos.AsNoTracking().Include(a => a.Familia).Include(a => a.Escuela).AsQueryable();
        if (q is not null)
        {
            query = query.Where(a => a.NombreCompleto.Contains(q) || a.Matricula.Contains(q) || (a.Familia.NombrePapa != null && a.Familia.NombrePapa.Contains(q)) || (a.Familia.NombreMama != null && a.Familia.NombreMama.Contains(q)) || (a.Familia.NombreTutor != null && a.Familia.NombreTutor.Contains(q)));
        }

        if (escuelaId.HasValue)
        {
            query = query.Where(a => a.EscuelaId == escuelaId.Value);
        }

        if (estado == "activo")
        {
            query = query.Where(a => a.Activo);
        }

        if (estado == "inactivo")
        {
            query = query.Where(a => !a.Activo);
        }

        return query.OrderBy(a => a.NombreCompleto).ThenBy(a => a.Id);
    }
    public async Task<IActionResult> Index(string? q, int? escuelaId, string? estado, int page = 1, CancellationToken ct = default)
    {
        ViewData["Q"] = q;
        ViewData["Estado"] = estado;
        ViewData["EscuelaId"] = escuelaId;
        ViewBag.Escuelas = new SelectList(await Db.Escuelas.AsNoTracking().OrderBy(e => e.Nombre).ToListAsync(ct), "Id", "Nombre", escuelaId);
        return View(await PagedList<Alumno>.CreateAsync(Filter(q, escuelaId, estado), page, ct));
    }
    public async Task<IActionResult> Exportar(string? q, int? escuelaId, string? estado, CancellationToken ct)
    {
        var query = Filter(q, escuelaId, estado);
        if (await query.CountAsync(ct) > 10000)
        {
            return BadRequest("Filtra por escuela para exportar hasta 10,000 alumnos por archivo.");
        }

        return File(CsvExporter.Alumnos(await query.ToListAsync(ct)), "text/csv; charset=utf-8", $"alumnos-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var a = await Filter(null, null, null).FirstOrDefaultAsync(a => a.Id == id, ct);
        return a is null ? NotFound() : View(a);
    }
    [HttpGet]
    public async Task<IActionResult> Create(int? familiaId, CancellationToken ct)
    {
        var form = new AlumnoForm { FamiliaId = familiaId ?? 0 };
        await LoadOptionsAsync(form, ct);
        return View("Form", form);
    }
    [HttpPost]
    public async Task<IActionResult> Create(AlumnoForm form, CancellationToken ct)
    {
        form.Id = 0;
        await ValidateAsync(form, null, ct);
        if (ModelState.IsValid)
        {
            var a = new Alumno();
            Apply(form, a);
            Db.Alumnos.Add(a);
            if (await SaveAsync(ct))
            {
                TempData["Success"] = "Alumno registrado y vinculado con su familia y escuela.";
                return RedirectToAction(nameof(Details), new
                {
                    id = a.Id
                });
            }
        }
        await LoadOptionsAsync(form, ct);
        return View("Form", form);
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var a = await Db.Alumnos.FindAsync([id], ct);
        if (a is null)
        {
            return NotFound();
        }

        var form = new AlumnoForm { Id = a.Id, Version = a.Version, Matricula = a.Matricula, NombreCompleto = a.NombreCompleto, FechaNacimiento = a.FechaNacimiento, Grado = a.Grado, Grupo = a.Grupo, Activo = a.Activo, FamiliaId = a.FamiliaId, EscuelaId = a.EscuelaId };
        await LoadOptionsAsync(form, ct);
        return View("Form", form);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(int id, AlumnoForm form, CancellationToken ct)
    {
        if (id != form.Id)
        {
            return BadRequest();
        }

        var a = await Db.Alumnos.FindAsync([id], ct);
        if (a is null)
        {
            return NotFound();
        }

        await ValidateAsync(form, a, ct);
        if (ModelState.IsValid)
        {
            Apply(form, a);
            CheckVersion(a, form.Version);
            if (await SaveAsync(ct))
            {
                TempData["Success"] = "Expediente del alumno actualizado.";
                return RedirectToAction(nameof(Details), new
                {
                    id
                });
            }
        }
        await LoadOptionsAsync(form, ct);
        return View("Form", form);
    }
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var a = await Filter(null, null, null).FirstOrDefaultAsync(a => a.Id == id, ct);
        return a is null ? NotFound() : View(a);
    }
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, Guid version, CancellationToken ct)
    {
        var a = await Db.Alumnos.Include(a => a.Escuela).Include(a => a.Familia).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (a is null)
        {
            return NotFound();
        }

        CheckVersion(a, version);
        Db.Alumnos.Remove(a);
        if (!await SaveAsync(ct))
        {
            return View(a);
        }

        TempData["Success"] = "Alumno eliminado. La familia y la escuela se conservaron.";
        return RedirectToAction(nameof(Index));
    }
    private async Task LoadOptionsAsync(AlumnoForm form, CancellationToken ct)
    {
        var familias = await Db.Familias.AsNoTracking().OrderBy(f => f.NombrePapa ?? f.NombreMama ?? f.NombreTutor).ToListAsync(ct);
        form.Familias = familias.Select(f => new SelectListItem($"F-{f.Id:000} · {f.Referencia}", f.Id.ToString()));
        form.Escuelas = (await Db.Escuelas.AsNoTracking().Where(e => e.Activa || e.Id == form.EscuelaId).OrderBy(e => e.Nombre).ToListAsync(ct))
            .Select(e => new SelectListItem(e.Nombre + (e.Activa ? "" : " (inactiva)"), e.Id.ToString()));
    }
    private async Task ValidateAsync(AlumnoForm f, Alumno? current, CancellationToken ct)
    {
        f.Matricula = (f.Matricula ?? "").Trim().ToUpperInvariant();
        if (await Db.Alumnos.AnyAsync(a => a.Matricula == f.Matricula && a.Id != f.Id, ct))
        {
            ModelState.AddModelError(nameof(f.Matricula), "La matrícula ya está registrada.");
        }

        if (!await Db.Familias.AnyAsync(fam => fam.Id == f.FamiliaId, ct))
        {
            ModelState.AddModelError(nameof(f.FamiliaId), "Selecciona una familia existente.");
        }

        var escuela = await Db.Escuelas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == f.EscuelaId, ct);
        if (escuela is null || (!escuela.Activa && current?.EscuelaId != escuela.Id))
        {
            ModelState.AddModelError(nameof(f.EscuelaId), "Selecciona una escuela activa para el alumno.");
        }
    }
    private static void Apply(AlumnoForm f, Alumno a)
    {
        a.Matricula = f.Matricula;
        a.NombreCompleto = f.NombreCompleto.Trim();
        a.FechaNacimiento = f.FechaNacimiento!.Value;
        a.Grado = f.Grado.Trim();
        a.Grupo = f.Grupo.Trim();
        a.Activo = f.Activo;
        a.FamiliaId = f.FamiliaId;
        a.EscuelaId = f.EscuelaId;
    }
}

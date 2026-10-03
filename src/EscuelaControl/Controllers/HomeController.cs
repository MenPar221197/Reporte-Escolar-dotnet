using System.Diagnostics;
using EscuelaControl.Data;
using EscuelaControl.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EscuelaControl.Controllers;

public class HomeController(EscuelaDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(new DashboardVm
        {
            AlumnosActivos = await db.Alumnos.CountAsync(a => a.Activo, ct),
            Familias = await db.Familias.CountAsync(ct),
            EscuelasActivas = await db.Escuelas.CountAsync(e => e.Activa, ct),
            SinContacto = await db.Familias.CountAsync(f => f.TelefonoPapa == null && f.TelefonoMama == null && f.TelefonoTutor == null && f.EmailContacto == null, ct),
            Recientes = await db.Alumnos.AsNoTracking().Include(a => a.Escuela).OrderByDescending(a => a.CreadoUtc).ThenByDescending(a => a.Id).Take(5).ToListAsync(ct),
            PorEscuela = await db.Escuelas.AsNoTracking().Where(e => e.Activa).OrderBy(e => e.Nombre)
                .Select(e => new EscuelaResumen(e.Nombre, e.Alumnos.Count(a => a.Activo))).ToListAsync(ct)
        });
    }
    [AllowAnonymous, ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return View(new ErrorVm(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
    }
}

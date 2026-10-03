using EscuelaControl.Models;
using Microsoft.EntityFrameworkCore;

namespace EscuelaControl.ViewModels;

public class PagedList<T>
{
    public List<T> Items { get; set; } = [];
    public int Page
    {
        get; set;
    }
    public int Total
    {
        get; set;
    }
    public int PageSize { get; set; } = 15;
    public int Pages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
    public static async Task<PagedList<T>> CreateAsync(IQueryable<T> query, int page, CancellationToken ct)
    {
        var result = new PagedList<T> { Total = await query.CountAsync(ct) };
        result.Page = Math.Clamp(page, 1, result.Pages);
        result.Items = await query.Skip((result.Page - 1) * result.PageSize).Take(result.PageSize).ToListAsync(ct);
        return result;
    }
}

public record EscuelaFila(int Id, string Clave, string Nombre, NivelEducativo Nivel, bool Activa, int Alumnos);
public record FamiliaFila(int Id, string? NombrePapa, string? NombreMama, string? NombreTutor, string? TelefonoPapa, string? TelefonoMama, string? TelefonoTutor, int Hijos);
public record EscuelaResumen(string Nombre, int Alumnos);
public class DashboardVm
{
    public int AlumnosActivos
    {
        get; set;
    }
    public int Familias
    {
        get; set;
    }
    public int EscuelasActivas
    {
        get; set;
    }
    public int SinContacto
    {
        get; set;
    }
    public List<Alumno> Recientes { get; set; } = [];
    public List<EscuelaResumen> PorEscuela { get; set; } = [];
}
public record ErrorVm(string RequestId);
public record PaginationVm(int Page, int Pages, int Total);

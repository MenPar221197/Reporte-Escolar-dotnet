using EscuelaControl.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EscuelaControl.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config, bool seedDemo)
    {
        var db = services.GetRequiredService<EscuelaDbContext>();
        await db.Database.MigrateAsync();
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        if (!await users.Users.AnyAsync())
        {
            var email = config["Bootstrap:Email"]?.Trim();
            var password = config["Bootstrap:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Para el primer inicio configura Bootstrap:Email y Bootstrap:Password con dotnet user-secrets o variables de entorno.");
            }

            var result = await users.CreateAsync(new IdentityUser { UserName = email, Email = email }, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("No se creó el administrador: " + string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
        if (seedDemo && !await db.Escuelas.AnyAsync() && !await db.Familias.AnyAsync() && !await db.Alumnos.AnyAsync())
        {
            var escuelas = new[]
            {
                new Escuela { Clave = "DEMO-PRI", Nombre = "Colegio Horizonte · Primaria", Nivel = NivelEducativo.Primaria, Direccion = "Plantel de demostración, Querétaro" },
                new Escuela { Clave = "DEMO-SEC", Nombre = "Colegio Horizonte · Secundaria", Nivel = NivelEducativo.Secundaria, Direccion = "Plantel de demostración, Querétaro" }
            };
            var familias = new[]
            {
                new Familia { NombrePapa = "Gabriel Salas", NombreMama = "Mariana Ríos", EmailContacto = "familia.salas@example.invalid", Notas = "Registro ficticio de demostración." },
                new Familia { NombreMama = "Valeria Montes", EmailContacto = "familia.montes@example.invalid", Notas = "Registro ficticio de demostración." },
                new Familia { NombreTutor = "Andrés Vidal", EmailContacto = "familia.vidal@example.invalid", Notas = "Registro ficticio de demostración." },
                new Familia { NombrePapa = "Daniel Torres", NombreMama = "Lucía Navarro", Notas = "Registro ficticio sin teléfono: ejemplo de contacto pendiente." }
            };
            db.Escuelas.AddRange(escuelas);
            db.Familias.AddRange(familias);
            var nombres = new[] { "Sofía Salas Ríos", "Mateo Salas Ríos", "Renata Montes", "Diego Vidal", "Emilia Torres Navarro", "Nicolás Torres Navarro" };
            for (var i = 0; i < nombres.Length; i++)
            {
                db.Alumnos.Add(new Alumno
                {
                    Matricula = $"DEMO-{i + 1:000}",
                    NombreCompleto = nombres[i],
                    FechaNacimiento = new DateOnly(2013 + i % 4, 3 + i, 12),
                    Grado = i < 4 ? "3.º" : "1.º",
                    Grupo = i % 2 == 0 ? "A" : "B",
                    Familia = familias[i < 2 ? 0 : i == 2 ? 1 : i == 3 ? 2 : 3],
                    Escuela = escuelas[i < 4 ? 0 : 1]
                });
            }
            await db.SaveChangesAsync();
        }
        Console.WriteLine("Base de datos configurada y acceso inicial preparados.");
    }
}

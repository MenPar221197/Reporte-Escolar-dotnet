using System.Net;
using System.Text.RegularExpressions;
using EscuelaControl.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EscuelaControl.Tests;

// Estas pruebas usan SQL Server real: no sustituyen el motor por una base en memoria.
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION")))
        {
            Skip = "Configura TEST_SQL_CONNECTION para ejecutar las pruebas de integración con SQL Server.";
        }
    }
}

public sealed class SchoolApp : WebApplicationFactory<Program>
{
    public const string Email = "admin@example.invalid";
    public string Password { get; } = "Aa1!" + Guid.NewGuid().ToString("N");
    public string Connection
    {
        get;
    } = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION"))
    {
        InitialCatalog = "AulaTests_" + Guid.NewGuid().ToString("N")
    }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:EscuelaDB"] = Connection,
            ["Bootstrap:Email"] = Email,
            ["Bootstrap:Password"] = Password
        }));
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        await DbInitializer.InitializeAsync(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<IConfiguration>(), true);
    }

    public async Task DeleteTestDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<EscuelaDbContext>().Database.EnsureDeletedAsync();
    }
}

public class SchoolWebTests
{
    private static string Hidden(string html, string name)
    {
        var tag = Regex.Match(html, $"<input[^>]*name=\"{Regex.Escape(name)}\"[^>]*>", RegexOptions.IgnoreCase).Value;
        return WebUtility.HtmlDecode(Regex.Match(tag, "value=\"([^\"]*)\"").Groups[1].Value);
    }

    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, Dictionary<string, string> values)
    {
        var html = await client.GetStringAsync(path);
        values["__RequestVerificationToken"] = Hidden(html, "__RequestVerificationToken");
        if (!values.ContainsKey("Version"))
        {
            values["Version"] = Hidden(html, "Version");
        }

        return await client.PostAsync(path, new FormUrlEncodedContent(values));
    }

    private static async Task Login(HttpClient client, SchoolApp app)
    {
        var response = await Post(client, "/Account/Login", new()
        {
            ["Email"] = SchoolApp.Email,
            ["Password"] = app.Password
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static int CreatedId(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return int.Parse(response.Headers.Location!.ToString().Split('/').Last());
    }

    private static Dictionary<string, string> School(string key) => new() { ["Clave"] = key, ["Nombre"] = "Plantel de prueba", ["Nivel"] = "1", ["Activa"] = "true" };
    private static Dictionary<string, string> Student(string key, int family, int school) => new()
    {
        ["Matricula"] = key,
        ["NombreCompleto"] = "Alumno de prueba",
        ["FechaNacimiento"] = "2015-04-12",
        ["Grado"] = "3",
        ["Grupo"] = "A",
        ["FamiliaId"] = family.ToString(),
        ["EscuelaId"] = school.ToString(),
        ["Activo"] = "true"
    };

    private static async Task WithApp(Func<SchoolApp, HttpClient, Task> test)
    {
        await using var app = new SchoolApp();
        try
        {
            await app.InitializeAsync();
            using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            await test(app, client);
        }
        finally { await app.DeleteTestDatabaseAsync(); }
    }

    [SqlServerFact, Trait("Category", "Integration")]
    public async Task AccesoYFormulariosEstanProtegidos() => await WithApp(async (app, client) =>
    {
        foreach (var path in new[] { "/", "/Alumnos", "/Familias", "/Escuelas", "/Alumnos/Exportar" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
        }
        var invalid = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        await Login(client, app);
        var noToken = await client.PostAsync("/Escuelas/Create", new FormUrlEncodedContent(School("TEST-001")));
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
        var page = await client.GetAsync("/Alumnos");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("no-store", page.Headers.CacheControl!.ToString());
    });

    [SqlServerFact, Trait("Category", "Integration")]
    public async Task FlujoCompletoFamiliaHermanosEscuelaExportacionYBajas() => await WithApp(async (app, client) =>
    {
        await Login(client, app);
        var school = CreatedId(await Post(client, "/Escuelas/Create", School("TEST-001")));
        var family = CreatedId(await Post(client, "/Familias/Create", new()
        {
            ["NombreTutor"] = "Tutora de prueba",
            ["EmailContacto"] = "tutora@example.invalid"
        }));
        var first = CreatedId(await Post(client, "/Alumnos/Create", Student("PRUEBA-001", family, school)));
        var second = CreatedId(await Post(client, "/Alumnos/Create", Student("PRUEBA-002", family, school)));
        var familyPage = await client.GetStringAsync($"/Familias/Details/{family}");
        Assert.Contains("PRUEBA-001", familyPage);
        Assert.Contains("PRUEBA-002", familyPage);
        var list = await client.GetStringAsync($"/Alumnos?escuelaId={school}&q=PRUEBA-001");
        Assert.Contains("PRUEBA-001", list);
        Assert.DoesNotContain("PRUEBA-002", list);
        var csv = await client.GetStringAsync($"/Alumnos/Exportar?escuelaId={school}");
        Assert.Contains("PRUEBA-001", csv);
        Assert.Contains("tutora@example.invalid", csv);
        Assert.DoesNotContain("DEMO-001", csv);
        var familyDelete = await Post(client, $"/Familias/Delete/{family}", new());
        Assert.Equal(HttpStatusCode.OK, familyDelete.StatusCode);
        Assert.Contains("alumnos vinculados", await familyDelete.Content.ReadAsStringAsync());
        var schoolDelete = await Post(client, $"/Escuelas/Delete/{school}", new());
        Assert.Equal(HttpStatusCode.OK, schoolDelete.StatusCode);
        var edit = Student("PRUEBA-001", family, school);
        edit["Id"] = first.ToString();
        edit["Activo"] = "false";
        Assert.Equal(HttpStatusCode.Redirect, (await Post(client, $"/Alumnos/Edit/{first}", edit)).StatusCode);
        Assert.Contains("PRUEBA-001", await client.GetStringAsync($"/Alumnos?escuelaId={school}&estado=inactivo"));
        Assert.DoesNotContain("PRUEBA-001", await client.GetStringAsync($"/Alumnos?escuelaId={school}&estado=activo"));
        foreach (var id in new[] { first, second })
        {
            Assert.Equal(HttpStatusCode.Redirect, (await Post(client, $"/Alumnos/Delete/{id}", new())).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Redirect, (await Post(client, $"/Familias/Delete/{family}", new())).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await Post(client, $"/Escuelas/Delete/{school}", new())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Alumnos/Details/{first}")).StatusCode);
    });

    [SqlServerFact, Trait("Category", "Integration")]
    public async Task DuplicadosYEdicionesSimultaneasNoSobrescribenDatos() => await WithApp(async (app, client) =>
    {
        await Login(client, app);
        var id = CreatedId(await Post(client, "/Escuelas/Create", School("TEST-001")));
        var duplicate = await Post(client, "/Escuelas/Create", School("TEST-001"));
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Contains("Ya existe", await duplicate.Content.ReadAsStringAsync());
        var original = await client.GetStringAsync($"/Escuelas/Edit/{id}");
        var edit = School("TEST-001");
        edit["Id"] = id.ToString();
        edit["Nombre"] = "Cambio primero";
        edit["Version"] = Hidden(original, "Version");
        Assert.Equal(HttpStatusCode.Redirect, (await Post(client, $"/Escuelas/Edit/{id}", edit)).StatusCode);
        edit["Nombre"] = "Cambio obsoleto";
        var stale = await Post(client, $"/Escuelas/Edit/{id}", edit);
        Assert.Equal(HttpStatusCode.OK, stale.StatusCode);
        Assert.Contains("Otro usuario", await stale.Content.ReadAsStringAsync());
        var details = await client.GetStringAsync($"/Escuelas/Details/{id}");
        Assert.Contains("Cambio primero", details);
        Assert.DoesNotContain("Cambio obsoleto", details);
    });

    [SqlServerFact, Trait("Category", "Integration")]
    public async Task PreparacionEsRepetibleYFormulariosInvalidosNoSeGuardan() => await WithApp(async (app, client) =>
    {
        await app.InitializeAsync();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EscuelaDbContext>();
            Assert.Equal(6, await db.Alumnos.CountAsync());
            Assert.Equal(4, await db.Familias.CountAsync());
            Assert.Equal(2, await db.Escuelas.CountAsync());
            Assert.Equal(1, await db.Users.CountAsync());
        }
        await Login(client, app);
        var invalidFamily = await Post(client, "/Familias/Create", new());
        Assert.Equal(HttpStatusCode.OK, invalidFamily.StatusCode);
        Assert.Contains("Registra al menos", await invalidFamily.Content.ReadAsStringAsync());
        var invalidStudent = await Post(client, "/Alumnos/Create", Student("TEST-001", 999999, 999999));
        Assert.Equal(HttpStatusCode.OK, invalidStudent.StatusCode);
        Assert.Contains("Selecciona una familia existente", await invalidStudent.Content.ReadAsStringAsync());
    });
}

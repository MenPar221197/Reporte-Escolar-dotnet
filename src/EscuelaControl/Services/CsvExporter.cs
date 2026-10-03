using System.Text;
using EscuelaControl.Models;

namespace EscuelaControl.Services;

public static class CsvExporter
{
    // Además de escapar comillas, neutraliza fórmulas al abrir el CSV en Excel.
    public static string Cell(string? value)
    {
        value ??= "";
        if (value.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@' || value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n'))
        {
            value = "'" + value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    public static byte[] Alumnos(IEnumerable<Alumno> alumnos)
    {
        var text = new StringBuilder("Matrícula,Alumno,Escuela,Grado,Grupo,Estado,Padre,Teléfono padre,Madre,Teléfono madre,Tutor,Teléfono tutor,Correo\r\n");
        foreach (var a in alumnos)
        {
            text.AppendLine(string.Join(",", new[] { a.Matricula, a.NombreCompleto, a.Escuela.Nombre, a.Grado, a.Grupo, a.Activo ? "Activo" : "Inactivo", a.Familia.NombrePapa, a.Familia.TelefonoPapa, a.Familia.NombreMama, a.Familia.TelefonoMama, a.Familia.NombreTutor, a.Familia.TelefonoTutor, a.Familia.EmailContacto }.Select(Cell)));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray();
    }
}

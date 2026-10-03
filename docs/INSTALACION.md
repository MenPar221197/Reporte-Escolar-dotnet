# Instalación y primer uso

## Requisitos

- [SDK de .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0), no solamente el runtime.
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) instalado e iniciado. Puedes usar Express; Developer es para desarrollo y demostración.
- Visual Studio compatible con .NET 10 es opcional. También puedes ejecutar todo desde PowerShell.

## Preparación guiada en Windows

Abre PowerShell en la carpeta que contiene `EscuelaControl.sln` y ejecuta:

```powershell
.\scripts\preparar.ps1 -ConDatosDemo
.\scripts\iniciar.ps1
```

Si usas una instancia con nombre, copia el nombre que aparece al conectarte en SSMS:

```powershell
.\scripts\preparar.ps1 -Servidor '.\SQLEXPRESS' -ConDatosDemo
```

Para LocalDB, si lo tienes instalado:

```powershell
.\scripts\preparar.ps1 -Servidor '(localdb)\MSSQLLocalDB' -ConDatosDemo
```

La preparación usa autenticación de Windows. Tu usuario debe poder crear la base de datos en esa instancia. La contraseña solicitada pertenece a **Aula**, no a SQL Server; debe tener al menos 12 caracteres, mayúscula, minúscula, número y símbolo. Se usa durante la creación y no se escribe en un archivo del proyecto.

La conexión local queda guardada en `dotnet user-secrets`. La aplicación crea `EscuelaControlDB` mediante migraciones e inserta una cuenta administrativa solo si todavía no existe ninguna. Ejecutar nuevamente la preparación no cambia la contraseña existente ni duplica los ejemplos. Los datos demo solo se insertan cuando las tres tablas de negocio están vacías.

Abre **http://localhost:5109**. Para detener la aplicación, presiona `Ctrl+C` en la terminal. Los registros permanecen en SQL Server y se conservan cuando vuelvas a iniciar.

### Si PowerShell bloquea el script descargado

Revisa el contenido de los archivos y desbloquea únicamente estos dos:

```powershell
Unblock-File .\scripts\preparar.ps1
Unblock-File .\scripts\iniciar.ps1
```

Si tu organización restringe los scripts, usa los comandos manuales de la siguiente sección; no necesitas cambiar la política del equipo.

## Ejecución manual

Desde la raíz del repositorio:

```powershell
dotnet restore
dotnet build
dotnet user-secrets set "ConnectionStrings:EscuelaDB" "Server=localhost;Database=EscuelaControlDB;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True" --project src/EscuelaControl
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Bootstrap__Email = Read-Host 'Correo administrador'
$claveAula = Read-Host 'Nueva contraseña de Aula' -AsSecureString
$punteroAula = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($claveAula)
try {
    $env:Bootstrap__Password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($punteroAula)
    dotnet run --project src/EscuelaControl --no-launch-profile -- --initialize --seed-demo
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($punteroAula)
    Remove-Item Env:Bootstrap__Password -ErrorAction SilentlyContinue
    Remove-Item Env:Bootstrap__Email -ErrorAction SilentlyContinue
}
dotnet run --project src/EscuelaControl --launch-profile http
```

Para SQL Server con usuario y contraseña, configura `ConnectionStrings:EscuelaDB` mediante un gestor de secretos o una variable `ConnectionStrings__EscuelaDB` en el proceso. Formato:

```text
Server=SERVIDOR,PUERTO;Database=EscuelaControlDB;User ID=USUARIO;Password=CLAVE;Encrypt=True;TrustServerCertificate=True
```

Los valores en mayúsculas son marcadores que debes sustituir. `TrustServerCertificate=True` es para la conexión local de desarrollo; usa un certificado válido y `False` en el servidor institucional.

## Base de datos desde SSMS

No necesitas usar el asistente “Generate Scripts” para ejecutar este proyecto.

La vía recomendada es `--initialize`. Si quieres revisar o crear manualmente el esquema:

1. Ejecuta `database/00-crear-base.sql`.
2. Selecciona **EscuelaControlDB** en el selector de base de la ventana de consulta.
3. Ejecuta `database/01-esquema.sql`, generado a partir de las migraciones. Es idempotente: solo aplica las migraciones pendientes.
4. Ejecuta la preparación de Aula para crear la cuenta administrativa y, si lo deseas, los ejemplos.

No ejecutes el esquema nuevo sobre la base antigua `EscuelaDB`. Esta versión no incluye un importador de sus registros. Conserva esa base si contiene datos que quieras migrar después.

## Visual Studio

1. **Archivo → Abrir → Proyecto o solución**, selecciona `EscuelaControl.sln`.
2. **Ver → Explorador de soluciones** (`Ctrl+Alt+L`).
3. Haz clic derecho en `EscuelaControl` → **Establecer como proyecto de inicio**.
4. Tras preparar la base, elige el perfil **http** y ejecuta con `Ctrl+F5`.

Para HTTPS local:

```powershell
dotnet dev-certs https --trust
dotnet run --project src/EscuelaControl --launch-profile https
```

Abre `https://localhost:7189`. El certificado de desarrollo sirve para tu equipo; no es el certificado de un sitio público.

## Solución de problemas

| Mensaje o síntoma | Qué revisar |
| --- | --- |
| `dotnet` no se reconoce | Instala el SDK de .NET 10 y vuelve a abrir PowerShell. |
| No se encuentra el SDK de `global.json` | Comprueba `dotnet --list-sdks`; debe existir una versión estable `10.0.x`. |
| Error 26/40, servidor no encontrado | Usa en `-Servidor` el mismo nombre de instancia que funciona en SSMS. Confirma que el servicio SQL Server esté iniciado. |
| Login failed / acceso denegado | La preparación usa tu usuario de Windows. Revisa sus permisos sobre la instancia. |
| Invalid object name / tabla inexistente | Ejecuta la preparación contra la misma conexión con la que inicias la aplicación. |
| No se creó el administrador | Usa un correo válido y una contraseña que cumpla las reglas. Vuelve a ejecutar la preparación. |
| La contraseña no cambió al repetir la preparación | Es intencional: una cuenta existente conserva su contraseña. Cámbiala dentro de Aula. |
| El navegador no conecta | Espera el mensaje `Now listening on` y deja la terminal abierta. Comprueba el puerto 5109. |
| Página de error al entrar | Revisa el mensaje de la terminal; la pantalla pública no muestra detalles internos de SQL. |
| No puedo eliminar una familia o escuela | Tiene alumnos vinculados. Reasígnalos o marca la escuela como inactiva. |

## Actualizar el esquema durante el desarrollo

```bash
dotnet tool restore
dotnet ef migrations add NombreDelCambio --project src/EscuelaControl
dotnet ef migrations script --idempotent --project src/EscuelaControl --output database/01-esquema.sql
```

Revisa el SQL y respalda la base antes de aplicar cambios sobre registros que necesites conservar. El servidor web no aplica migraciones por cada solicitud: se ejecutan explícitamente con `--initialize`.

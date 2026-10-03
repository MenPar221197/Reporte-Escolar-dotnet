$ErrorActionPreference = "Stop"
$proyectoAula = Join-Path $PSScriptRoot "../src/EscuelaControl/EscuelaControl.csproj"
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "Instala el SDK de .NET 10 y vuelve a abrir PowerShell."
}
Write-Host "Aula se abrira en http://localhost:5109. Deja esta ventana abierta." -ForegroundColor Green
Write-Host "La primera vez ejecuta scripts/preparar.ps1 para crear la base y tu cuenta."
& dotnet run --project $proyectoAula --launch-profile http
if ($LASTEXITCODE -ne 0) { throw "No se pudo iniciar Aula. Revisa el mensaje anterior y docs/INSTALACION.md." }

param(
    [string]$Servidor = "localhost",
    [string]$Correo,
    [switch]$ConDatosDemo
)

$ErrorActionPreference = "Stop"
$proyectoAula = Join-Path $PSScriptRoot "../src/EscuelaControl/EscuelaControl.csproj"
$entornoAnteriorAula = $env:ASPNETCORE_ENVIRONMENT
$conexionAnteriorAula = $env:ConnectionStrings__EscuelaDB
$correoAnteriorAula = $env:Bootstrap__Email
$claveAnteriorAula = $env:Bootstrap__Password

function Confirmar-Comando([string]$Paso) {
    if ($LASTEXITCODE -ne 0) { throw "Fallo: $Paso. Revisa el mensaje anterior." }
}

try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "Instala el SDK de .NET 10 y vuelve a abrir PowerShell: https://dotnet.microsoft.com/download/dotnet/10.0"
    }
    if ([string]::IsNullOrWhiteSpace($Correo)) { $Correo = Read-Host "Correo para la cuenta administrativa" }
    $claveSeguraAula = Read-Host "Nueva contrasena (12+ caracteres, mayuscula, minuscula, numero y simbolo)" -AsSecureString
    $punteroClaveAula = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($claveSeguraAula)
    try { $env:Bootstrap__Password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($punteroClaveAula) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($punteroClaveAula) }

    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:Bootstrap__Email = $Correo.Trim()
    $env:ConnectionStrings__EscuelaDB = "Server=$Servidor;Database=EscuelaControlDB;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10"
    & dotnet user-secrets set "ConnectionStrings:EscuelaDB" $env:ConnectionStrings__EscuelaDB --project $proyectoAula
    Confirmar-Comando "guardar configuracion local"
    & dotnet restore $proyectoAula
    Confirmar-Comando "restaurar paquetes"
    & dotnet build $proyectoAula --no-restore
    Confirmar-Comando "compilar"
    $argumentosAula = @("run", "--project", $proyectoAula, "--no-build", "--no-launch-profile", "--", "--initialize")
    if ($ConDatosDemo) { $argumentosAula += "--seed-demo" }
    & dotnet @argumentosAula
    Confirmar-Comando "crear base y cuenta (SQL Server debe estar iniciado)"
    Write-Host "Preparacion terminada. Ejecuta .\scripts\iniciar.ps1" -ForegroundColor Green
    Write-Host "Si ya existia una cuenta, conserva su contrasena anterior. Este script no la cambia."
}
finally {
    $env:ASPNETCORE_ENVIRONMENT = $entornoAnteriorAula
    $env:ConnectionStrings__EscuelaDB = $conexionAnteriorAula
    $env:Bootstrap__Email = $correoAnteriorAula
    $env:Bootstrap__Password = $claveAnteriorAula
}

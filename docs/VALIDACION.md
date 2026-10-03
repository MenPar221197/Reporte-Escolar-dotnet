# Validación de la entrega

Comprobaciones ejecutadas el **3 de octubre de 2026** sobre la versión de entrega.

## Entorno utilizado

- Ubuntu 24.04, SDK .NET 10.0.401.
- SQL Server 2025 Developer, instancia local de pruebas.
- Chrome Headless 154 y Playwright para el recorrido del navegador.
- Vistas revisadas a 1440 × 1040 y 390 × 844 píxeles.

## Resultado

| Comprobación | Resultado |
| --- | --- |
| Compilación de la solución | Correcta, sin errores ni advertencias. |
| Pruebas automatizadas en Release | 15 aprobadas, 0 fallidas, 0 omitidas. |
| Publicación `dotnet publish -c Release` | Correcta. |
| Recorrido del navegador | Completado; sin errores de JavaScript ni de consola. |
| Vista móvil | Sin desbordamiento horizontal de la página en las pantallas revisadas. |

Las pruebas se ejecutaron contra **SQL Server real**, con bases temporales independientes. Verifican acceso autenticado, rechazo de solicitudes sin token antifalsificación, creación y vínculo de hermanos, búsquedas, filtros, exportación, bajas, eliminación restringida, claves duplicadas, conflictos de edición, validaciones y preparación repetible sin duplicar datos.

Las pruebas unitarias adicionales revisan el responsable mínimo por familia, teléfonos sin responsable, fechas de nacimiento y neutralización de fórmulas/escape de comillas en el CSV.

En el navegador se recorrieron inicio de sesión, resumen, validación y alta de familia, familia preseleccionada al agregar un hijo, alta de alumno, búsqueda, descarga del CSV, vista móvil, eliminación de los registros de prueba y cierre de sesión. Las capturas de `docs/images/` corresponden a la aplicación ejecutándose con datos ficticios.

## Cómo repetir las pruebas

Sin base de pruebas:

```powershell
dotnet test -c Release
```

Se ejecutan 11 casos unitarios y se omiten explícitamente 4 pruebas de integración.

Con SQL Server y autenticación de Windows:

```powershell
$env:TEST_SQL_CONNECTION = 'Server=localhost;Database=master;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True'
dotnet test -c Release --logger trx
Remove-Item Env:TEST_SQL_CONNECTION
```

Usa el nombre de tu instancia. La cuenta necesita poder crear y eliminar bases **de prueba**. Cada ejecución asigna nombres `AulaTests_<identificador>` y elimina únicamente esas bases al finalizar; no utiliza `EscuelaControlDB` para sus pruebas de integración.

El flujo de GitHub Actions incluido compila y ejecuta la misma suite con una instancia efímera de SQL Server y una contraseña aleatoria para esa ejecución.

## Límites de la comprobación

La instalación local de SQL Server de tu laptop y los scripts PowerShell no se ejecutaron en Windows durante estas pruebas. La guía describe cómo seleccionar tu instancia y resolver problemas de conexión. No se ha desplegado un sitio público ni realizado una prueba de carga o auditoría de seguridad de producción.

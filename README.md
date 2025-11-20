# LabClinic.Api (.NET 8 Web API)
API para el sistema de laboratorio, conectada a MySQL `dblaboratorio`.

## Requisitos
- .NET 8 SDK
- MySQL en localhost con BD `dblaboratorio` (importa `sislaboratorio/dblaboratorio.sql`)
- Cambia `Jwt:Key` en `appsettings.json`

## Comandos
```bash
dotnet restore
dotnet run
# Swagger en https://localhost:5001/swagger (o http://localhost:5000/swagger)
```
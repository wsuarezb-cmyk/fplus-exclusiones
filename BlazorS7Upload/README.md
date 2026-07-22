# Pandora Exclusiones

Aplicacion interna en Blazor Server para gestion de exclusiones de KPI de marketplace. Permite consultar ordenes desde BigQuery y registrar exclusiones individuales o masivas por pais.

## Estado actual

- Framework: ASP.NET Core 9 (Blazor Server)
- Runtime local: .NET 9
- Auth/SSO en produccion: Google IAP
- Hosting objetivo de produccion: Cloud Run (proyecto flb-rtl-3p-sx-reg-dev, region us-east4) detras de HTTPS Load Balancer + IAP

## Arquitectura funcional

- Frontend: Blazor Server
- Datos operativos: PostgreSQL por Npgsql + Dapper
- Consulta de ordenes: Google BigQuery
- Componentes UI: Blazor Bootstrap

## Funcionalidades principales

- Gestion de exclusiones:
  - Individual
  - Masivo
  - Homologacion
- Consulta de ordenes por pais (CO, CL, PE)
- Gestion de roles de usuario para autorizacion funcional

## Estructura relevante

```
BlazorS7Upload/
  Authentication/            # IapAuthenticationStateProvider
  Data/                      # ExclusionesService
  Interfaces/                # IExclusionesService, IUserRolesService
  Models/                    # Modelos de dominio
  Pages/                     # Vistas Razor
  Shared/                    # Layout y menu
  Program.cs                 # Configuracion de servicios y pipeline
  appsettings.json           # Base local sanitizada
  appsettings.Development.json
```

Nota: archivos y modulos antiguos fueron archivados en carpetas _unused para no afectar compilacion.

## Configuracion

### Local (Development)

- Usa appsettings.Development.json para connection strings de desarrollo.
- En local, la app expone:
  - http://localhost:5051
  - https://localhost:444

### Cloud Run (Produccion)

La app esta preparada para leer configuracion desde secretos montados en filesystem:

- Variable de entorno AppSettingsPath, por defecto /config/appsettings.json
- Variable de entorno BigQuery__CredentialsPath (ejemplo: /creds/mycreds.json)

Esto evita guardar credenciales en la imagen del contenedor.

## Secretos usados en despliegue

- appsettings_forte -> /config/appsettings.json
- BQ_CREDENTIALS_AOVIEDO -> /creds/mycreds.json

## Comando de despliegue de referencia

```bash
gcloud run deploy forte \
  --image=us-east4-docker.pkg.dev/flb-rtl-3p-sx-reg-dev/forte-repo/forte:latest \
  --region=us-east4 \
  --platform=managed \
  --port=8080 \
  --memory=1Gi \
  --min-instances=1 \
  --no-allow-unauthenticated \
  --ingress=internal-and-cloud-load-balancing \
  --set-secrets=/config/appsettings.json=appsettings_forte:latest,/creds/mycreds.json=BQ_CREDENTIALS_AOVIEDO:latest \
  --set-env-vars=AppSettingsPath=/config/appsettings.json,BigQuery__CredentialsPath=/creds/mycreds.json
```

## Flujo de acceso en produccion

1. Usuario entra por HTTPS Load Balancer
2. IAP autentica contra cuenta Google corporativa
3. IAP reenvia request a Cloud Run
4. La app asigna autorizacion funcional usando roles en base de datos

## Seguridad

- No subir credenciales reales al repositorio.
- Mantener appsettings.json del repo en estado sanitizado.
- Consumir secretos solo desde Secret Manager en produccion.

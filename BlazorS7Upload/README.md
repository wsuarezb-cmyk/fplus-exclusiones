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
- BQ_CREDENTIALS_AOVIEDO -> /secrets/mycreds.json

  (Ojo: no puede ir en `/config/mycreds.json`. Ver "Problemas encontrados al desplegar" más abajo — Cloud Run no permite montar dos secretos distintos como archivos dentro del mismo directorio.)

  Dentro del contenido del secreto `appsettings_forte`, la clave `BigQuery:CredentialsPath` debe apuntar exactamente a `/secrets/mycreds.json` (ruta absoluta), no a un nombre relativo como `"mycreds.json"` — si no coincide con la ruta real de montaje, la app arranca bien pero falla al conectarse a BigQuery.

## Comando de despliegue de referencia (el que realmente quedó funcionando)

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
  --set-secrets=/config/appsettings.json=appsettings_forte:latest,/secrets/mycreds.json=BQ_CREDENTIALS_AOVIEDO:latest
```

## Problemas encontrados al desplegar (julio 2026) y cómo se resolvieron

Migrar este proyecto a Cloud Run como servicio `forte` (proyecto `flb-rtl-3p-sx-reg-dev`, región `us-east4`) tomó varias vueltas. Dejamos esto documentado para no repetir los mismos errores:

1. **Nunca montar secretos como archivo dentro de `/app`.** El `Dockerfile` publica el DLL y las dependencias en `/app` (`WORKDIR /app`). Si se monta un secreto en una ruta dentro de ese mismo directorio (ej. `/app/appsettings.json`), Cloud Run crea un volumen nuevo para *todo* el directorio y oculta lo demás — incluyendo `BlazorS7Upload.dll`. El contenedor arranca sin encontrar su propio binario. Por eso los secretos se montan en `/config/` y `/secrets/`, fuera de `/app`.

2. **Cloud Run no permite montar dos secretos distintos como archivos en el mismo directorio** vía `--set-secrets`. El primer intento montó `appsettings_forte` en `/config/appsettings.json` y `BQ_CREDENTIALS_AOVIEDO` en `/config/mycreds.json` (mismo directorio `/config/`) y falló con: `Cannot update secret at [/config/mycreds.json] because a different secret is already mounted in the same directory.` Solución: cada secreto en su propio directorio (`/config/` y `/secrets/`).

3. **`--ingress=internal-and-cloud-load-balancing` bloquea el acceso directo por la URL pública** (`https://forte-....run.app`). Sin un Load Balancer HTTPS configurado apuntando a este servicio (o acceso desde dentro de la VPC), cualquier intento de entrar por esa URL da un 404 genérico ("Error: Page not found / The requested URL was not found on this server") — **no es un bug de la app, es Google bloqueando la petición antes de llegar al contenedor.** Para pruebas rápidas se cambió temporalmente a `--ingress=all`; hay que volver a restringirlo una vez el Load Balancer + IAP estén completamente configurados para producción.

4. **Nunca poner `ASPNETCORE_ENVIRONMENT=Development` en este servicio.** `Program.cs` fuerza `UseUrls("http://localhost:5051", "https://localhost:444")` cuando el entorno es Development, ignorando el puerto 8080 que espera Cloud Run. Además, al intentar levantar el endpoint HTTPS en `localhost:444`, Kestrel busca un certificado de desarrollo que no existe dentro del contenedor (`dotnet dev-certs https` nunca corre ahí) y el proceso truena por completo (`InvalidOperationException: Unable to configure HTTPS endpoint...`). Cloud Run reporta esto como "container failed to start and listen on the port 8080". Si esto ocurre, revisar con `gcloud run services describe forte` si quedó esa variable pegada de un intento anterior y quitarla con `--remove-env-vars=ASPNETCORE_ENVIRONMENT`.

5. **Un `gcloud run services update`/`deploy` fallido puede dejar variables de entorno "pegadas"** en la configuración del servicio aunque esa revisión nunca haya llegado a servir tráfico. No asumir que un deploy fallido revirtió solo los cambios — revisar/quitar explícitamente con `--remove-env-vars` si hace falta.

6. **La sesión de `gcloud`/Docker en Cloud Shell puede expirar.** Si `docker push` falla con `error getting credentials ... You do not currently have an active account selected`, correr de nuevo `gcloud auth login` y `gcloud auth configure-docker us-east4-docker.pkg.dev` antes de reintentar.

## Flujo de acceso en produccion

1. Usuario entra por HTTPS Load Balancer (o, temporalmente, directo por la URL con `--ingress=all` mientras no esté armado el Load Balancer)
2. IAP autentica contra cuenta Google corporativa
3. IAP reenvia request a Cloud Run
4. La app asigna autorizacion funcional consultando roles en PostgreSQL (`dbo.usuarios` / `dbo.usuarios_roles` / `dbo.roles`, connection string `db_contenido`)

## ⚠️ Acceso provisional activo — pendiente de resolver

**Estado al 22 de julio de 2026:** las connection strings de PostgreSQL (`db_contenido`, `db_contenido_`, `db_posgreSQLCompliance`) dentro del secreto `appsettings_forte` todavía tienen el valor placeholder `"CONFIGURED_VIA_SECRET_MANAGER"` — no son cadenas de conexión reales. Esto hace que **cualquier consulta a Postgres falle silenciosamente** (el código atrapa la excepción y sigue), incluyendo la consulta de roles por usuario (`GetListRolesByUser`), que siempre devuelve una lista vacía sin importar qué rol tenga el usuario asignado en la base de datos real.

Para poder probar la vista mientras se resuelve lo anterior, se agregó un bypass acotado en `Authentication/IapAuthenticationStateProvider.cs`: si el email autenticado por IAP coincide **exactamente** con el valor de la config `Dev:BypassEmail`, se le asigna directamente el rol de `Dev:BypassRole`, sin pasar por la consulta a la base de datos. Este bypass **no afecta a ningún otro usuario** — solo al email configurado.

Configuración actual en el servicio `forte`:
```
Dev__BypassEmail=wsuarezb@falabella.com
Dev__BypassRole=administrador
```

**Pendiente para resolver esto de forma definitiva** (en este orden):
1. Completar las connection strings reales de Postgres en el secreto `appsettings_forte` (host, usuario, contraseña reales — posiblemente los mismos que usa `canon-compliance`, dado que comparten las mismas bases `Users_Pandora` / `prod_sx_co` / `prod_sx_compliance`).
2. Confirmar si `forte` necesita un conector VPC para alcanzar esa base de datos (el comando de deploy actual no tiene `--vpc-connector` configurado).
3. Verificar que la consulta real de roles funcione correctamente (ya se confirmó que el usuario `wsuarezb@falabella.com` existe en `dbo.usuarios` con rol `administrador`).
4. **Quitar las variables `Dev__BypassEmail` y `Dev__BypassRole` del servicio** (`gcloud run services update forte --region=us-east4 --remove-env-vars=Dev__BypassEmail,Dev__BypassRole`) una vez confirmado que el flujo real de roles funciona.
5. Evaluar si el código de bypass en `IapAuthenticationStateProvider.cs` se debe eliminar por completo o dejarlo documentado como mecanismo de emergencia (apagado por defecto, ya que sin esas env vars configuradas no hace nada).

## Seguridad

- No subir credenciales reales al repositorio.
- Mantener appsettings.json del repo en estado sanitizado.
- Consumir secretos solo desde Secret Manager en produccion.
- **No dejar `--ingress=all` ni las variables `Dev__BypassEmail`/`Dev__BypassRole` activas más tiempo del necesario para pruebas** — ver sección de acceso provisional arriba.

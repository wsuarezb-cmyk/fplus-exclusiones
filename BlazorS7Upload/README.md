# Pandora Exclusiones

Aplicación web interna desarrollada en **Blazor Server (.NET 8)** para la gestión de exclusiones de KPI en el proceso de Soporte de marketplace (Falabella). Permite a los equipos de operaciones consultar órdenes desde Google BigQuery y registrar exclusiones de forma individual, masiva o mediante homologación.

---

## Tecnologías principales

| Capa | Tecnología |
|---|---|
| Framework | ASP.NET Core 8 – Blazor Server |
| UI | Blazor Bootstrap 2.2.0 |
| Base de datos | PostgreSQL (Npgsql + EF Core 8 + Dapper) |
| Data warehouse | Google BigQuery (Google.Cloud.BigQuery.V2) |
| Tiempo real | ASP.NET Core SignalR |
| Excel | ClosedXML |
| FTP | FluentFTP |
| Autenticación | Custom Claims + ProtectedLocalStorage |

---

## Estructura del proyecto

```
BlazorS7Upload/
├── Authentication/          # Proveedor de autenticación custom, modelos de sesión y roles
├── Data/                    # Servicios de negocio (ExclusionesService)
├── DB/                      # DbContext de PostgreSQL (PSqlComplianceDbContext)
├── Helpers/                 # Utilidades: paginación, filtros, Excel, CSV, conversiones
├── Interfaces/              # IExclusionesService, IUserRolesService
├── Models/                  # Modelos de datos (ExclusionesModel, KPI, Motivos, etc.)
├── Pages/                   # Páginas Razor (Index, Login, Exclusiones, Configuración)
│   └── Components/          # Componentes reutilizables (Sidebar, Modales, Dropdowns, etc.)
├── Shared/                  # Layout principal y NavMenu
├── wwwroot/                 # Archivos estáticos (CSS, JS)
├── Program.cs               # Configuración de servicios y pipeline HTTP
├── appsettings.json         # Configuración de conexiones y BigQuery
├── mycreds.json             # Credenciales de Google Cloud (no versionar)
└── ftp.json                 # Configuración FTP (no versionar)
```

---

## Funcionalidades

### Exclusiones
- **Individual**: registro de una exclusión por orden.
- **Masivo**: selección múltiple de órdenes con KPI y motivo unificado.
- **Homologación**: gestión de tabla de homologación de atributos.

### Consulta de órdenes
- Consulta en tiempo real a **Google BigQuery** para validar órdenes por país (CO, CL, PE, AR).
- Paginación, filtros dinámicos y ordenamiento en cliente.

### Gestión de usuarios (rol `administrador`)
- Registro de nuevos usuarios.
- Asignación y revocación de roles.
- Cambio de contraseña.
- Eliminación de usuarios.

### Autenticación
- Login con email y contraseña (hash SHA256 + salt por email).
- Sesión persistida en `ProtectedLocalStorage`.
- Autorización basada en roles: `administrador`, `exclusiones`.

---

## Bases de datos (PostgreSQL – red interna)

| Connection string key | Base de datos | Uso |
|---|---|---|
| `db_contenido` | `Users_Pandora` | Usuarios y autenticación |
| `db_contenido_` | `prod_sx_co` | Datos de exclusiones |
| `db_posgreSQLCompliance` | `prod_sx_compliance` | Compliance general |

---

## Configuración requerida

### `appsettings.json`
```json
{
  "ConnectionStrings": {
    "db_contenido": "...",
    "db_contenido_": "...",
    "db_posgreSQLCompliance": "..."
  },
  "BigQuery": {
    "ProjectId": "<gcp-project-id>",
    "CredentialsPath": "mycreds.json"
  }
}
```

### `mycreds.json`
Archivo de credenciales de servicio de Google Cloud (Service Account JSON). **No debe ser versionado**.

### `ftp.json`
Configuración del servidor FTP para carga de archivos. **No debe ser versionado**.

---

## Ejecución local

```bash
# Restaurar dependencias
dotnet restore

# Ejecutar en desarrollo
dotnet run
```

La aplicación estará disponible en:
- `http://localhost:5051`
- `https://localhost:444`

---

## Roles de usuario

| Rol | Acceso |
|---|---|
| `administrador` | Todas las páginas + gestión de usuarios y roles |
| `exclusiones` | Página de exclusiones |

---

## Consideraciones de seguridad

- Las cadenas de conexión y credenciales **no deben subirse al repositorio**. Usar variables de entorno o secretos en producción.
- `mycreds.json` y `ftp.json` deben estar en `.gitignore`.
- Las contraseñas se almacenan como hash (SHA256 + salt). No se guardan en texto plano.

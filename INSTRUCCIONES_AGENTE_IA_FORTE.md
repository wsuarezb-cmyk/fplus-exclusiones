# Instrucciones: botón flotante + panel del Asistente IA en Forte

> **Para quién es este documento:** un agente de código (o desarrollador) que trabajará sobre el repo `BlazorS7Upload` (Forte, Blazor Server, .NET 9).
> **Objetivo:** que en **todas las vistas** de Forte aparezca un botón flotante que abre un panel lateral con el asistente de IA (servicio en Cloud Run), igual que el patrón "Pregúntale a Echo" de la app de referencia.
> **Regla general:** preparar los cambios para revisión/aprobación. **No ejecutar despliegues ni cambios en producción** (ver sección 9).

---

## 0. Decisiones pendientes (resolver antes de implementar)

Cada una tiene un valor por defecto para poder avanzar; confirmar con el dueño del proyecto.

| # | Decisión | Opciones | Por defecto |
|---|----------|----------|-------------|
| D1 | **URL del servicio del agente** | URL `run.app` o dominio propio | `Agent:Url` en config; sin URL el botón no se muestra |
| D2 | **Modo de integración** | **A)** iframe directo a la URL del agente · **B)** proxy inverso dentro de Forte (iframe a `/agent/`) | Fase 1 = A (MVP). Fase 2 = B (recomendado si el agente es privado) |
| D3 | **Quién ve el botón** | Cualquier usuario autenticado · solo algunos roles (ej. `operator`) | Cualquier usuario autenticado (`Agent:AllowedRoles` vacío) |
| D4 | **Nombre, subtítulo y textos** | Libre | "Asistente Forte" / "Pregúntale al asistente" |
| D5 | **Origen(es) de Forte tal como los ve el navegador** | URL `run.app` y/o dominio del Load Balancer con IAP | Necesarios para `frame-ancestors` del agente |
| D6 | **¿Quién construye la UI de chat del agente?** | El servicio ya trae su propia UI web · solo expone API y hay que construir el chat | Se asume que el servicio **ya sirve una UI de chat** embebible |

---

## 1. Referencia visual (app "Echo") y cómo se traduce a Forte

Lo observado en las capturas de la app de referencia:

| Elemento en la referencia | Equivalente en Forte |
|---|---|
| Botón flotante tipo "píldora" abajo a la derecha: ícono + texto "Pregúntale a Echo" + insignia de atajo de teclado | `.agent-fab` fijo abajo a la derecha: ícono + texto configurable. Atajo `Ctrl/⌘ + K` opcional (sección 7) |
| Al hacer clic, se abre un **panel lateral derecho** a pantalla completa en alto sobre el dashboard | `<aside class="agent-panel">` fijo a la derecha, ancho ~460 px (100 % en móvil), sin bloquear la app |
| Encabezado del panel: ícono, título, subtítulo, estado "Conectado", botón de cerrar (X) | Encabezado con título, subtítulo, botón cerrar. Estado "Conectando…" mientras carga el iframe |
| Contenido del panel: la UI de chat del servicio (bienvenida, sugerencias, caja de texto "Enviar") | `<iframe>` que apunta al servicio del agente (UI propia del servicio) |
| El botón se ve en la vista principal del dashboard | En Forte: se monta **una sola vez en `MainLayout.razor`** para que aparezca en todas las vistas |

Paleta de Forte (ya existente en `site.css`): amarillo `#daca3d`, texto `#1A1A1A`, fuente `Inter`. El botón usa esa paleta (no la morada/azul de la referencia).

---

## 2. Contexto técnico de Forte (verificado en el repo)

- **Blazor Server**, .NET 9, Blazor Bootstrap + Bootstrap 5.3 + Bootstrap Icons (ya cargados en `Pages/_Layout.cshtml`).
- **`Shared/MainLayout.razor`** es el layout único; contiene `NavMenu` (sidebar), `OvertureTopBar` y `@Body`. Como el layout **no se recrea al navegar** entre páginas, un componente puesto allí **persiste** (el iframe y la conversación no se pierden al cambiar de vista).
- **Autenticación:** Google IAP. `IapAuthenticationStateProvider` lee `X-Goog-Authenticated-User-Email` y arma los roles desde `accesos.*`. Se usa `<AuthorizeView>` en el layout. Ojo: solo está registrado `AddAuthenticationCore()` (no hay middleware de autenticación de ASP.NET), por lo que **endpoints HTTP propios no tienen `ClaimsPrincipal`** (relevante para la Fase 2).
- **`Program.cs`** no define CSP ni `X-Frame-Options` hoy → Forte no bloquea iframes salientes.
- **Despliegue:** Cloud Run (proyecto `flb-rtl-3p-sx-reg-dev`, región `us-east4`) detrás de Load Balancer + IAP. Configuración desde el secreto `appsettings_forte` montado en `/config/appsettings.json`, y/o variables de entorno.
- **z-index existentes:** navbar 1030, offcanvas 1045, modal-backdrop 1050, modal 1055. El botón y el panel deben quedar **por debajo de los modales**.

---

## 3. Arquitectura propuesta

```
Navegador (usuario con sesión IAP en Forte)
 └─ Forte (Blazor Server)
     └─ MainLayout.razor
         └─ <AgentLauncher />   ← botón flotante + panel (se monta una vez)
             └─ <iframe src="Agent:Url">   ← se crea SOLO la primera vez que se abre
                      │
   Fase 1 (modo A) ───┴──► Servicio del agente en Cloud Run (URL directa)
   Fase 2 (modo B) ───────► Forte /agent/**  ──(ID token de SA)──► Servicio del agente (privado)
```

Principios:
1. **Carga perezosa:** el iframe no se crea hasta el primer clic (no penaliza el tiempo de carga de ninguna vista).
2. **Persistencia:** una vez creado, el iframe se mantiene al cerrar/abrir el panel y al navegar entre vistas.
3. **Mínimo privilegio:** `sandbox` en el iframe; sin datos personales en la URL; sin secretos en el front.
4. **Degradación segura:** si `Agent:Enabled=false` o `Agent:Url` es inválida, el botón **no se renderiza**.

---

## 4. FASE 1 — Implementación del botón y el panel (iframe directo)

### 4.1 Archivos a crear / modificar

| Acción | Archivo |
|---|---|
| Crear | `Models/AgentOptions.cs` |
| Crear | `Shared/AgentLauncher.razor` |
| Crear | `Shared/AgentLauncher.razor.css` (CSS aislado; ya queda incluido en `BlazorS7Upload.styles.css`, que `_Layout.cshtml` ya enlaza → no hay que tocar `_Layout`) |
| Modificar | `Program.cs` (registrar opciones) |
| Modificar | `Shared/MainLayout.razor` (montar el componente) |
| Modificar | `appsettings.json` (sección `Agent` sanitizada, deshabilitada) |
| Modificar | `README.md` (documentar la sección `Agent`) |

### 4.2 `Models/AgentOptions.cs`

```csharp
namespace BlazorS7Upload.Models
{
    public class AgentOptions
    {
        /// Interruptor general. Si es false, el botón no se renderiza.
        public bool Enabled { get; set; }

        /// URL del agente. Absoluta https (o http solo en loopback para desarrollo),
        /// o relativa "/agent/" cuando se use el proxy de la Fase 2.
        public string Url { get; set; }

        public string Title { get; set; } = "Asistente Forte";
        public string Subtitle { get; set; } = "";
        public string ButtonLabel { get; set; } = "Pregúntale al asistente";

        /// Roles permitidos separados por coma (ej. "operator"). Vacío = cualquier usuario autenticado.
        public string AllowedRoles { get; set; }
    }
}
```

### 4.3 `Program.cs`

Agregar junto a los demás `builder.Services...`:

```csharp
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection("Agent"));
```

(`using BlazorS7Upload.Models;` ya existe en el archivo.)

### 4.4 `appsettings.json` (versión del repo, sanitizada)

```json
"Agent": {
  "Enabled": false,
  "Url": "",
  "Title": "Asistente Forte",
  "Subtitle": "",
  "ButtonLabel": "Pregúntale al asistente",
  "AllowedRoles": ""
}
```

En **Development** (`appsettings.Development.json`) apuntar al agente local con el mismo esquema que Forte para evitar contenido mixto, por ejemplo `http://localhost:PUERTO` si Forte corre en `http://localhost:5051`.

### 4.5 `Shared/AgentLauncher.razor`

```razor
@using Microsoft.Extensions.Options
@using BlazorS7Upload.Models
@using Microsoft.AspNetCore.Components.Web
@inject IOptions<AgentOptions> AgentOptionsAccessor

@if (_enabled)
{
    <AuthorizeView Roles="@_options.AllowedRoles">
        <Authorized>

            <button type="button"
                    class="agent-fab @(_open ? "agent-fab--hidden" : "")"
                    @onclick="Open"
                    aria-haspopup="dialog"
                    aria-expanded="@(_open ? "true" : "false")"
                    aria-controls="agent-panel">
                <span class="bi bi-stars" aria-hidden="true"></span>
                <span class="agent-fab__label">@_options.ButtonLabel</span>
            </button>

            <aside id="agent-panel"
                   class="agent-panel @(_open ? "agent-panel--open" : "")"
                   role="dialog"
                   aria-label="@_options.Title"
                   tabindex="-1"
                   @onkeydown="OnKeyDown">

                <header class="agent-panel__header">
                    <div class="agent-panel__titles">
                        <div class="agent-panel__title">@_options.Title</div>
                        @if (!string.IsNullOrWhiteSpace(_options.Subtitle))
                        {
                            <div class="agent-panel__subtitle">@_options.Subtitle</div>
                        }
                    </div>
                    <button type="button" class="agent-panel__close" @onclick="Close" aria-label="Cerrar asistente">
                        <span class="bi bi-x-lg" aria-hidden="true"></span>
                    </button>
                </header>

                <div class="agent-panel__body">
                    @if (_everOpened)
                    {
                        @if (!_frameLoaded)
                        {
                            <div class="agent-panel__loading">Conectando con el asistente…</div>
                        }
                        <iframe class="agent-panel__frame @(_frameLoaded ? "" : "agent-panel__frame--pending")"
                                src="@_options.Url"
                                title="@_options.Title"
                                sandbox="allow-scripts allow-same-origin allow-forms allow-popups allow-popups-to-escape-sandbox"
                                referrerpolicy="strict-origin"
                                @onload="OnFrameLoaded"></iframe>
                    }
                </div>
            </aside>

        </Authorized>
    </AuthorizeView>
}

@code {
    private AgentOptions _options = new();
    private bool _enabled;
    private bool _open;
    private bool _everOpened;   // el iframe se crea la primera vez que se abre y ya no se destruye
    private bool _frameLoaded;

    protected override void OnInitialized()
    {
        _options = AgentOptionsAccessor.Value;
        _enabled = _options.Enabled && IsAllowedUrl(_options.Url);
    }

    private static bool IsAllowedUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (url.StartsWith("/") && !url.StartsWith("//")) return true;   // mismo origen (proxy, Fase 2)
        return Uri.TryCreate(url, UriKind.Absolute, out var u)
            && (u.Scheme == Uri.UriSchemeHttps
                || (u.Scheme == Uri.UriSchemeHttp && u.IsLoopback));      // http solo en local
    }

    private void Open()  { _open = true; _everOpened = true; }
    private void Close() { _open = false; }
    private void OnFrameLoaded() { _frameLoaded = true; }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape") Close();
    }
}
```

Notas de implementación:
- Si `AuthorizeView` con `Roles` vacío/nulo da problemas, dejar solo la verificación de autenticación y filtrar roles por separado.
- Blazor no vuelve a tocar el `src` del iframe mientras el valor no cambie, por eso la conversación persiste al abrir/cerrar.
- `@onload` en `<iframe>` usa el evento `onload` de Blazor (`ProgressEventArgs`). **Verificar en pruebas** que se dispara; si no, quitar el indicador de carga (el resto funciona igual).

### 4.6 `Shared/AgentLauncher.razor.css`

```css
/* ---------- Botón flotante ---------- */
.agent-fab {
    position: fixed;
    right: 24px;
    bottom: 24px;
    z-index: 1040;                     /* debajo de offcanvas (1045) y modales (1055) */
    display: inline-flex;
    align-items: center;
    gap: 10px;
    padding: 12px 20px;
    border: 0;
    border-radius: 999px;
    background: #daca3d;
    color: #1A1A1A;
    font-family: 'Inter', sans-serif;
    font-size: 14px;
    font-weight: 600;
    box-shadow: 0 6px 18px rgba(0,0,0,.18);
    cursor: pointer;
    transition: transform .15s ease, box-shadow .15s ease, opacity .15s ease;
}
.agent-fab:hover { transform: translateY(-2px); box-shadow: 0 10px 24px rgba(0,0,0,.22); }
.agent-fab:focus-visible { outline: 3px solid #1A1A1A; outline-offset: 2px; }
.agent-fab--hidden { opacity: 0; pointer-events: none; transform: scale(.9); }

/* ---------- Panel lateral ---------- */
.agent-panel {
    position: fixed;
    top: 0;
    right: 0;
    bottom: 0;
    width: min(460px, 100vw);
    z-index: 1046;                     /* sobre el offcanvas, bajo el backdrop/modales */
    display: flex;
    flex-direction: column;
    background: #FFFFFF;
    border-left: 1px solid #E0E0E0;
    box-shadow: -8px 0 24px rgba(0,0,0,.12);
    transform: translateX(100%);
    visibility: hidden;                /* evita foco por teclado cuando está cerrado */
    transition: transform .25s ease, visibility 0s linear .25s;
}
.agent-panel--open {
    transform: translateX(0);
    visibility: visible;
    transition: transform .25s ease, visibility 0s;
}
.agent-panel:focus { outline: none; }

.agent-panel__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 14px 16px;
    border-top: 3px solid #daca3d;
    border-bottom: 1px solid #E0E0E0;
    flex-shrink: 0;
}
.agent-panel__title { font-family: 'Montserrat', 'Arial', sans-serif; font-weight: 700; font-size: 1rem; color: #1A1A1A; line-height: 1.2; }
.agent-panel__subtitle { font-family: 'Inter', sans-serif; font-size: .75rem; color: #757575; }

.agent-panel__close {
    width: 32px; height: 32px;
    display: inline-flex; align-items: center; justify-content: center;
    border: 1px solid #E0E0E0; border-radius: 8px;
    background: #FFFFFF; color: #3F3F3F; cursor: pointer;
}
.agent-panel__close:hover { background: rgba(218,202,61,.25); }
.agent-panel__close:focus-visible { outline: 3px solid #1A1A1A; outline-offset: 2px; }

.agent-panel__body { position: relative; flex: 1; min-height: 0; }
.agent-panel__frame { position: absolute; inset: 0; width: 100%; height: 100%; border: 0; }
.agent-panel__frame--pending { opacity: 0; }
.agent-panel__loading {
    position: absolute; inset: 0;
    display: flex; align-items: center; justify-content: center;
    font-family: 'Inter', sans-serif; font-size: .85rem; color: #757575;
}

/* ---------- Responsive / accesibilidad ---------- */
@media (max-width: 576px) {
    .agent-fab { right: 16px; bottom: 16px; padding: 14px; }
    .agent-fab__label { display: none; }
    .agent-panel { width: 100vw; }
}
@media (prefers-reduced-motion: reduce) {
    .agent-fab, .agent-panel, .agent-panel--open { transition: none !important; }
}
```

### 4.7 `Shared/MainLayout.razor`

Agregar `<AgentLauncher />` **una sola vez**, al final del contenedor raíz (después del `div` principal `d-flex`). No dentro de `@Body` ni de páginas individuales.

```razor
<div class="d-flex" style="height:100vh; overflow:hidden;">
    ... (sin cambios) ...
</div>

<AgentLauncher />
```

Verificaciones para confirmar que aparece en **todas** las vistas:
- Buscar en el repo `@layout` y `Layout =` para detectar páginas que usen otro layout. Si existe alguna, decidir si también debe llevar el botón.
- Confirmar en `App.razor` que `DefaultLayout="@typeof(MainLayout)"`.

---

## 5. Requisitos del lado del servicio del agente (Cloud Run)

Esto no se implementa en Forte, pero **sin esto el iframe no se verá**. Compartir con quien construye/sube el servicio.

1. **Permitir ser embebido por Forte.** Responder en todas las páginas de la UI el header:
   `Content-Security-Policy: frame-ancestors 'self' https://<ORIGEN_FORTE>`
   (incluir **cada** origen con el que los usuarios abren Forte: el `run.app` y/o el dominio del Load Balancer — ver D5). **No** enviar `X-Frame-Options: DENY` ni `SAMEORIGIN`.
2. Escuchar en `0.0.0.0:$PORT` (Cloud Run inyecta `PORT`).
3. Servir una **UI de chat responsive** que funcione en un ancho de ~460 px (o 100 % en móvil).
4. Si usa respuestas en streaming (SSE/WebSocket), configurar el `--timeout` de Cloud Run acorde.
5. Evitar arranques en frío largos para la primera pregunta: considerar `--min-instances=1`.
6. **No registrar en logs** datos personales ni el contenido de preguntas con datos sensibles.
7. Autenticación: ver sección 6 (define cómo se protege el servicio).

---

## 6. Autenticación del servicio del agente: dos caminos

| | **Modo A — iframe directo** | **Modo B — proxy desde Forte (recomendado)** |
|---|---|---|
| iframe apunta a | URL del agente | `/agent/` (mismo origen que Forte) |
| Servicio del agente | Público, **o** detrás de IAP | **Privado** (`--no-allow-unauthenticated`) |
| Riesgos | Si está tras IAP: la redirección de login de Google **no puede mostrarse dentro de un iframe**, y las cookies de terceros entre dominios `run.app` distintos pueden bloquearse → panel en blanco. Si es público: cualquiera con la URL lo usa | Requiere trabajo extra en Forte (Fase 2) y que el agente soporte *base path* `/agent/` |
| Identidad del usuario | El agente tendría que resolverla por su cuenta | Forte la envía en un header de servidor a servidor (nunca en la URL) |
| Cuándo usarlo | MVP / pruebas / agente con auth propia ya resuelta | Producción con datos internos |

**Recomendación:** implementar la Fase 1 (modo A) para validar la experiencia con un agente de pruebas, y pasar a modo B antes de exponer datos internos reales.

---

## 7. (Opcional) Atajo de teclado `Ctrl/⌘ + K`

La referencia muestra una insignia de atajo en el botón. Si se quiere replicar:

`wwwroot/js/agentLauncher.js`
```javascript
window.forteAgent = {
    register(dotNetRef) {
        this._h = e => {
            if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
                e.preventDefault();
                dotNetRef.invokeMethodAsync('Toggle');
            }
        };
        document.addEventListener('keydown', this._h);
    },
    dispose() { document.removeEventListener('keydown', this._h); }
};
```

En `_Layout.cshtml`, junto a los otros scripts propios: `<script src="~/js/agentLauncher.js"></script>`.

En `AgentLauncher.razor`: `@inject IJSRuntime JS`, `@implements IAsyncDisposable`, un `DotNetObjectReference<AgentLauncher>`, un método `[JSInvokable] public void Toggle()` que alterne `_open`, ponga `_everOpened = true` y llame `InvokeAsync(StateHasChanged)`, registrar en `OnAfterRenderAsync(firstRender)` **solo si `_enabled`**, y llamar `forteAgent.dispose` + `Dispose()` de la referencia en `DisposeAsync`. Mostrar la insignia "Ctrl K" dentro del botón.

---

## 8. FASE 2 — Proxy inverso en Forte (modo B)

> Especificación de referencia. Las APIs citadas deben **verificarse contra la versión de paquetes instalada** antes de usarlas.

### 8.1 Qué se logra
- El iframe carga `/agent/` (mismo origen) → no hay problemas de CORS, de cookies de terceros ni de `frame-ancestors` (el agente se sirve desde el origen de Forte; en el agente basta `frame-ancestors 'self'`).
- El agente queda **privado**: solo la cuenta de servicio de Forte puede invocarlo.

### 8.2 Pasos
1. Agregar el paquete `Yarp.ReverseProxy`.
2. En `Program.cs`, registrar el proxy con una ruta `/agent/{**catch-all}` hacia `Agent:Upstream` (URL real del servicio en Cloud Run) quitando el prefijo `/agent` (`PathRemovePrefix`), y mapear con `app.MapReverseProxy()` **antes** de `app.MapFallbackToPage("/_Host")`.
3. Añadir una transformación de request que:
   - Obtenga un **ID token** de la cuenta de servicio de Forte con audiencia = URL del agente y lo envíe como `Authorization: Bearer ...`. Patrón con `Google.Apis.Auth` (ya llega transitivamente con `Google.Cloud.BigQuery.V2`):
     ```csharp
     var cred = await GoogleCredential.GetApplicationDefaultAsync();
     var oidc = await cred.GetOidcTokenAsync(OidcTokenOptions.FromTargetAudience(agentUrl));
     var token = await oidc.GetAccessTokenAsync();   // reutilizar el objeto oidc (singleton); se refresca solo
     ```
     Verificar que ADC en Cloud Run use la cuenta de servicio del runtime (Forte hoy también usa un archivo de credenciales para BigQuery; no debe interferir con esto).
   - Envíe la identidad del usuario al agente en un header propio (por ejemplo `X-Forte-User`), **nunca en query string ni en la ruta**.
4. **Proteger la ruta `/agent/**` dentro de Forte.** Como no hay middleware de autenticación de ASP.NET, el endpoint del proxy debe, antes de reenviar:
   - exigir el header `X-Goog-Authenticated-User-Email` (inyectado por IAP) y responder `401` si falta;
   - comprobar el rol permitido reutilizando la lógica de `IapAuthenticationStateProvider` / `IUserRolesService` (revisar sus firmas reales);
   - idealmente, **validar el JWT de IAP** (`x-goog-iap-jwt-assertion`) siguiendo la guía de Google "Securing your app with signed headers", porque mientras el servicio de Forte siga con `--ingress=all` un cliente podría falsificar el header simple.
5. Cambiar la configuración: `Agent:Url = "/agent/"` y `Agent:Upstream = <URL del agente>`.
6. Asegurar que el agente funcione bajo el prefijo `/agent/` (rutas de assets relativas o *base path* configurable); si sirve assets con rutas absolutas (`/static/...`), fallará.
7. Verificar que streaming (SSE/WebSocket) atraviesa el proxy sin buffering.

### 8.3 Permisos (los ejecuta una persona con acceso, con aprobación)
```bash
# Agente privado
gcloud run deploy <SERVICIO_AGENTE> --image=<IMAGEN> --region=us-east4 \
  --no-allow-unauthenticated --min-instances=1

# Permitir que SOLO la cuenta de servicio de Forte lo invoque
gcloud run services add-iam-policy-binding <SERVICIO_AGENTE> --region=us-east4 \
  --member="serviceAccount:<CUENTA_SERVICIO_RUNTIME_DE_FORTE>" \
  --role="roles/run.invoker"
```

---

## 9. Configuración y despliegue (requiere aprobación humana)

Forte ya lee configuración del secreto `appsettings_forte` y de variables de entorno. Para habilitar el asistente, **una persona autorizada** puede elegir una de estas vías:

**Vía 1 — variables de entorno (la URL no es secreta):**
```bash
gcloud run services update forte --region=us-east4 \
  --update-env-vars=Agent__Enabled=true,Agent__Url=<URL_DEL_AGENTE>
```

**Vía 2 — nueva versión del secreto `appsettings_forte`** con la sección `Agent` completa.

Recordatorios del README de Forte:
- Un `deploy/update` fallido puede dejar variables "pegadas": verificar con `gcloud run services describe forte` y limpiar con `--remove-env-vars` si hace falta.
- Nunca poner `ASPNETCORE_ENVIRONMENT=Development` en Cloud Run.
- No subir URLs internas, credenciales ni secretos al repo; el `appsettings.json` del repo queda sanitizado (`Enabled: false`).

---

## 10. Seguridad (checklist obligatorio)

- [ ] El botón **no se renderiza** si `Agent:Enabled=false` o la URL no es válida (https, loopback http o ruta relativa).
- [ ] Iframe con `sandbox` mínimo y `referrerpolicy="strict-origin"`.
- [ ] **Sin datos personales** (email, documentos, etc.) en URLs, nombres de archivo ni parámetros del iframe.
- [ ] Sin secretos/tokens en el front ni en el repo.
- [ ] El agente define `frame-ancestors` con los orígenes exactos de Forte y no envía `X-Frame-Options` restrictivo.
- [ ] En modo B: ruta `/agent/**` protegida, agente privado, `roles/run.invoker` solo para la cuenta de servicio de Forte.
- [ ] Si más adelante Forte añade una CSP, incluir `frame-src 'self' <ORIGEN_DEL_AGENTE>`.
- [ ] El agente no registra contenido sensible en logs.

---

## 11. Orden de ejecución sugerido

1. Resolver D1–D6 con el dueño del proyecto (o usar valores por defecto).
2. Crear `AgentOptions`, `AgentLauncher.razor` y `AgentLauncher.razor.css`.
3. Registrar opciones en `Program.cs`; añadir la sección `Agent` a `appsettings.json`; montar `<AgentLauncher />` en `MainLayout.razor`.
4. Probar en local con un agente de prueba (o una página HTML simple servida con `frame-ancestors` permitido).
5. Actualizar `README.md` de Forte con la sección `Agent`.
6. Abrir PR para revisión. **Despliegue y cambios de configuración en Cloud Run solo con aprobación.**
7. (Opcional) Atajo de teclado (sección 7).
8. Fase 2: proxy (sección 8) antes de exponer datos internos reales.

---

## 12. Criterios de aceptación y pruebas

**Funcionales**
- [ ] El botón flotante aparece abajo a la derecha en **todas** las vistas (Inicio, Exclusiones, etc.) para usuarios autorizados, y no aparece en la pantalla de "Acceso no autorizado".
- [ ] Clic en el botón → el panel entra desde la derecha; el botón se oculta; se ve "Conectando…" y luego la UI del agente.
- [ ] Cerrar (X o `Esc`) oculta el panel y devuelve el botón; al reabrir **la conversación sigue ahí** (el iframe no se recargó).
- [ ] Navegar entre vistas con el panel abierto **no** lo cierra ni reinicia la conversación.
- [ ] Con `Agent:Enabled=false` o URL vacía/inválida no se ve ningún botón y no hay errores en consola.
- [ ] Con `AllowedRoles` definido, un usuario sin ese rol no ve el botón.

**Visuales / UX**
- [ ] Un modal de Bootstrap o el offcanvas de Exclusiones se muestra **por encima** del botón.
- [ ] En pantallas ≤ 576 px el botón queda como círculo (sin texto) y el panel ocupa el 100 % del ancho.
- [ ] Foco visible en botón y cierre; con el panel cerrado no se puede tabular hacia su contenido.

**Técnicas**
- [ ] El iframe no se crea hasta el primer clic (verificar en pestaña Network).
- [ ] No hay errores de `frame-ancestors` / `X-Frame-Options` en consola al abrir el panel.
- [ ] Sin contenido mixto (Forte y agente con el mismo esquema en local).
- [ ] `dotnet build` sin advertencias nuevas.

---

## 13. Fuera de alcance

- Construir el agente, su lógica de IA o su UI de chat.
- Cambios de infraestructura (Load Balancer, IAP, IAM) — los ejecuta quien tenga permisos, con aprobación.
- Guardar historial de conversaciones dentro de Forte.

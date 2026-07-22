using BlazorS7Upload.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BlazorS7Upload.Authentication
{
    /// <summary>
    /// Proveedor de autenticación basado en Google IAP (Identity-Aware Proxy).
    /// En producción lee el header X-Goog-Authenticated-User-Email inyectado por IAP.
    /// En Development otorga acceso libre con rol administrador.
    /// </summary>
    public class IapAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;
        private readonly IUserRolesService _userRolesService;
        private readonly IConfiguration _configuration;

        public IapAuthenticationStateProvider(
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment env,
            IUserRolesService userRolesService,
            IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _env = env;
            _userRolesService = userRolesService;
            _configuration = configuration;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // ── Modo Development: acceso libre con usuario mock ──────────────
            if (_env.IsDevelopment())
            {
                var devClaims = new[]
                {
                    new Claim(ClaimTypes.Email, "dev@local"),
                    new Claim(ClaimTypes.Name, "Developer"),
                    new Claim(ClaimTypes.Role, "administrador"),
                    new Claim(ClaimTypes.Role, "exclusiones"),
                    new Claim(ClaimTypes.Role, "supervisor"),
                    new Claim(ClaimTypes.Role, "configuracion"),
                };
                var devIdentity = new ClaimsIdentity(devClaims, "IapDev");
                return new AuthenticationState(new ClaimsPrincipal(devIdentity));
            }

            // ── Producción: leer header de IAP ───────────────────────────────
            var anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            var iapHeader = _httpContextAccessor.HttpContext?
                .Request.Headers["X-Goog-Authenticated-User-Email"]
                .FirstOrDefault();

            if (string.IsNullOrEmpty(iapHeader))
                return anonymous;

            // El header tiene formato "accounts.google.com:user@domain.com"
            var email = iapHeader.Contains(':') ? iapHeader.Split(':')[1] : iapHeader;

            if (string.IsNullOrEmpty(email))
                return anonymous;

            // ── Bypass temporal para pruebas en Cloud Run mientras la conexión a
            // Postgres no esté lista. Solo aplica al email exacto configurado en
            // Dev:BypassEmail — nadie más se ve afectado. QUITAR estas env vars
            // (Dev__BypassEmail / Dev__BypassRole) apenas termine la prueba.
            var bypassEmail = _configuration["Dev:BypassEmail"];
            var bypassRole = _configuration["Dev:BypassRole"];

            List<string> roles;
            if (!string.IsNullOrEmpty(bypassEmail) && !string.IsNullOrEmpty(bypassRole)
                && string.Equals(email, bypassEmail, StringComparison.OrdinalIgnoreCase))
            {
                roles = new List<string> { bypassRole };
            }
            else
            {
                // Obtener roles desde PostgreSQL
                try
                {
                    roles = await _userRolesService.GetListRolesByUser(email);
                }
                catch
                {
                    roles = new List<string>();
                }
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, email),
            };
            claims.AddRange(roles.Where(r => !string.IsNullOrEmpty(r))
                                 .Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, "IapAuth");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
    }
}

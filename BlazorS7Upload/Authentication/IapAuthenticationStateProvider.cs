using BlazorS7Upload.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BlazorS7Upload.Authentication
{
    /// <summary>
    /// Proveedor de autenticación basado en Google IAP (Identity-Aware Proxy).
    /// En producción lee el header X-Goog-Authenticated-User-Email inyectado por IAP
    /// y resuelve el rol funcional consultando accesos.* (gestionado desde Overture).
    /// En Development otorga acceso libre con rol "operator" mock, sin tocar la base.
    /// </summary>
    public class IapAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;
        private readonly IUserRolesService _userRolesService;

        public IapAuthenticationStateProvider(
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment env,
            IUserRolesService userRolesService)
        {
            _httpContextAccessor = httpContextAccessor;
            _env = env;
            _userRolesService = userRolesService;
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
                    new Claim(ClaimTypes.Role, "operator"),
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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, email),
            };

            // Rol funcional segun accesos.usuarios_roles (Overture). Si el usuario no tiene
            // fila asignada para esta app, la lista viene vacia => sin rol => sin acceso.
            var roles = await _userRolesService.GetListRolesByUser(email);
            foreach (var rol in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                claims.Add(new Claim(ClaimTypes.Role, rol));
            }

            var identity = new ClaimsIdentity(claims, "IapAuth");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
    }
}

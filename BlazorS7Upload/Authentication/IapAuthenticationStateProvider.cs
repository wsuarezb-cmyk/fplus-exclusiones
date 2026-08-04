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

        public IapAuthenticationStateProvider(
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment env)
        {
            _httpContextAccessor = httpContextAccessor;
            _env = env;
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

            var identity = new ClaimsIdentity(claims, "IapAuth");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
    }
}

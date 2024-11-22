// PhoneShop/frontend/Authentication/CustomAuthenticationStateProvider.cs
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Blazored.LocalStorage;
using System.Threading.Tasks;
using System.Linq;

namespace frontend.Authentication
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;

        public CustomAuthenticationStateProvider(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                // No token found
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            try
            {
                // Parse the JWT token to extract claims
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var claims = jwtToken.Claims.ToList();

                var identity = new ClaimsIdentity(claims, "Bearer");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch
            {
                // If token is invalid or parsing fails, mark as logged out
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public void MarkUserAsAuthenticated(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var claims = jwtToken.Claims.ToList();
                var identity = new ClaimsIdentity(claims, "Bearer");
                var user = new ClaimsPrincipal(identity);
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
            }
            catch
            {
                MarkUserAsLoggedOut();
            }
        }

        public void MarkUserAsLoggedOut()
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
        }
    }
}
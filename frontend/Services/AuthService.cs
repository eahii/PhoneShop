using Shared.Models;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using frontend.Authentication;
using Blazored.LocalStorage;
using System.Net.Http.Headers;

namespace frontend.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly ILocalStorageService _localStorage;

        public AuthService(HttpClient httpClient, AuthenticationStateProvider authenticationStateProvider, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _authenticationStateProvider = authenticationStateProvider;
            _localStorage = localStorage;
        }

        public async Task<bool> Login(UserModel user)
        {
            // Create a separate object to send only Email and Password
            var loginData = new
            {
                Email = user.Email,
                Password = user.Password
            };

            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginData);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    await _localStorage.SetItemAsync("authToken", result.Token);
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
                    ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(result.Token);
                    return true;
                }
            }
            return false;
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            _httpClient.DefaultRequestHeaders.Authorization = null;
            ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();
        }

        public async Task<UserModel?> GetCurrentUser()
        {
            var response = await _httpClient.GetAsync("/api/auth/currentuser");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserModel>();
            }
            return null;
        }
    }

    public class LoginResult
    {
        public string Token { get; set; }
        public DateTime? Expiration { get; set; }
    }
}
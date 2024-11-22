// PhoneShop/frontend/Handlers/TokenAuthorizationHandler.cs
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Blazored.LocalStorage;

namespace frontend.Handlers
{
    public class TokenAuthorizationHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;

        public TokenAuthorizationHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
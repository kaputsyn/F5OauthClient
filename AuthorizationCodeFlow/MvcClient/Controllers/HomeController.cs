using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MvcClient.Controllers
{
    public class HomeController: Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public IActionResult Index() 
        {
            return View();
        }
        [Authorize]
        public async Task<IActionResult> Secret() 
        {
            return await HandleAuthorizedRequest(async () =>
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                //retrieve secret data
                var apiClient = _httpClientFactory.CreateClient();

                apiClient.SetBearerToken(accessToken);
                return await apiClient.GetAsync("https://api.perevershnyk.fozzy.ua:4435/example/values/2");

            });

        }
        private async Task<IActionResult> HandleAuthorizedRequest(Func<Task<HttpResponseMessage>> func) 
        {
            var response = await func();
            string message = "Ok";

            if (response.StatusCode == HttpStatusCode.Unauthorized) 
            {
                await RefreshTokens();
                message += " -> token refreshed";
                response = await func();
            }
            var content = await response.Content.ReadAsStringAsync();

            return new ObjectResult(new
            {
                message = message,
                content = content
            });
        }
        private async Task RefreshTokens() 
        {
            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

            var serverClient = _httpClientFactory.CreateClient();
            var document = await serverClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Policy = new DiscoveryPolicy { ValidateIssuerName = false },
                Address = "https://oauth-f5.fozzy.ua/f5-oauth2/v1/"
            });


            var refreshTokenClient = _httpClientFactory.CreateClient();
            var refreshTokenResponse = await refreshTokenClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = document.TokenEndpoint,
                RefreshToken = refreshToken,
                ClientId = "eaba049788f44f627cd56e85f4d50094a127f402bfa56b5e",
                ClientSecret = "9ba393df7ff074c1fb2976d631b9a5602171af6ccda20094a127f402bfa56b5e"
            });

            var authInfo = await HttpContext.AuthenticateAsync("Cookie");
            authInfo.Properties.UpdateTokenValue("access_token", refreshTokenResponse.AccessToken);
            authInfo.Properties.UpdateTokenValue("refresh_token", refreshTokenResponse.RefreshToken);

            await HttpContext.SignInAsync("Cookie", authInfo.Principal, authInfo.Properties);
        }
    }
}

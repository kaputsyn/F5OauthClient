using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace FZADClient.Controllers
{
    public class HomeController : Controller
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

           
            var res =  await HandleSecuredRequest(async () => await SecuredGetRequest("https://api.perevershnyk.fozzy.ua:4435/example/values/2/2"));
            if (res.IsSuccessStatusCode) 
            {
                return View();
            }
            return RedirectToAction("Index");
            
        }
        private async Task<HttpResponseMessage> SecuredGetRequest(string url) 
        {
            var token = await HttpContext.GetTokenAsync("access_token");
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return await client.GetAsync(url);
        }
        private async Task<HttpResponseMessage> HandleSecuredRequest(Func<Task<HttpResponseMessage>> request) 
        {
            var res = await request();


            if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await RefreshToken();
                
                return await request();
                
            }
            else 
            {
                return res;
            }
        }
        private async Task RefreshToken() 
        {
            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
            var data = new Dictionary<string, string>()
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth-f5.fozzy.ua/f5-oauth2/v1/token")
            {
                Content = new FormUrlEncodedContent(data)
            };

            var basicCredentials = "eaba049788f44f627cd56e85f4d50094a127f402bfa56b5e:9ba393df7ff074c1fb2976d631b9a5602171af6ccda20094a127f402bfa56b5e";
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(basicCredentials));
            var client =  _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {encoded}");

            var res = await client.SendAsync(request);

            if (res.IsSuccessStatusCode)
            {
                var response = await res.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

                var newAccessToken = responseData.GetValueOrDefault("access_token");
                var newRefreshToken = responseData.GetValueOrDefault("refresh_token");

                var authInfo = await HttpContext.AuthenticateAsync("ClientCookie");

                authInfo.Properties.UpdateTokenValue("access_token", newAccessToken);
                authInfo.Properties.UpdateTokenValue("refresh_token", newRefreshToken);
                await HttpContext.SignInAsync("ClientCookie", authInfo.Principal, authInfo.Properties);
            }
            else 
            {
                await HttpContext.SignOutAsync();
            }
        }
    }
}

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
        public async Task<string> Secret()
        {

            var res =  await HandleSecuredRequest(async () => await SecuredGetRequest("https://oauth-f5.fozzy.ua/f5-oauth2/v1/userinfo"));
            var body = await res.Content.ReadAsStringAsync();
            
            
             return body;
            
        }

        public async Task<string> Revoke() 
        {
            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
            var data = new Dictionary<string, string>()
            {
                ["token_type_hint"] = "refresh_token",
                ["token"] = refreshToken
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth-f5.fozzy.ua/f5-oauth2/v1/token/revoke")
            {
                Content = new FormUrlEncodedContent(data)
            };

            var basicCredentials = "f2bded84ec96e658296d29a6fd110094a127f4025cc99e5e:72ea78fcb5d4791950f169fcdf648994a0a9eac760fd0094a127f4025cc99e5e";
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(basicCredentials));
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {encoded}");

            var res = await client.SendAsync(request);


            if (res.IsSuccessStatusCode)
            {
                await HttpContext.SignOutAsync();
                return "Ok";
            }
            else
            {
                return "Failed";
            }
        }
        private async Task<HttpResponseMessage> SecuredGetRequest(string url) 
        {
            var token = await HttpContext.GetTokenAsync("access_token");
            var rt = await HttpContext.GetTokenAsync("refresh_token");
            var idt = await HttpContext.GetTokenAsync("id_token");
            var lot = await HttpContext.GetTokenAsync("logout_token");

            var _access_token =  new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
            var _id_token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(idt);

            
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            var res =  await client.GetAsync(url);
            return res;
        }
        private async Task<HttpResponseMessage> HandleSecuredRequest(Func<Task<HttpResponseMessage>> request) 
        {
            var res = await request();


            
            if (
                true
                &&res.StatusCode == System.Net.HttpStatusCode.Unauthorized
                )
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

            var basicCredentials = "f2bded84ec96e658296d29a6fd110094a127f4025cc99e5e:72ea78fcb5d4791950f169fcdf648994a0a9eac760fd0094a127f4025cc99e5e";
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(basicCredentials));
            var client =  _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {encoded}");

            var res = await client.SendAsync(request);

            
            if (res.IsSuccessStatusCode)
            {
                var response = await res.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

                var newAccessToken = responseData.GetValueOrDefault("access_token");
              //  var newIdToken = responseData.GetValueOrDefault("id_token");
               // var newRefreshToken = responseData.GetValueOrDefault("refresh_token");

                var authInfo = await HttpContext.AuthenticateAsync("ClientCookie");

                authInfo.Properties.UpdateTokenValue("access_token", newAccessToken);
               // authInfo.Properties.UpdateTokenValue("id_token", newIdToken);
                // authInfo.Properties.UpdateTokenValue("refresh_token", newRefreshToken);
                await HttpContext.SignInAsync("ClientCookie", authInfo.Principal, authInfo.Properties);
            }
            else 
            {
                await HttpContext.SignOutAsync();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MvcClient
{
    public class Startup
    {
         public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(config =>
            {
                config.DefaultScheme = "Cookie";
                config.DefaultChallengeScheme = "oidc";

            })
                .AddJwtBearer("Cookie", opt => opt.Authority = "https://oauth-f5.fozzy.ua/f5-oauth2/v1/")

                .AddOpenIdConnect("oidc", config =>
                {

                    config.Authority = "https://oauth-f5.fozzy.ua/f5-oauth2/v1/authorization";
                    config.ClientId = "eaba049788f44f627cd56e85f4d50094a127f402bfa56b5e";
                    config.ClientSecret = "9ba393df7ff074c1fb2976d631b9a5602171af6ccda20094a127f402bfa56b5e";
                    config.SaveTokens = true;

                    config.ResponseType = "code";
                    config.CallbackPath = "/callback";
                    config.GetClaimsFromUserInfoEndpoint = false;
                });
            services.AddControllersWithViews();
            services.AddHttpClient();
        }

      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}

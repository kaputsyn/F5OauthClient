using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace F5OpaqueClient
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(config =>
            {
                config.DefaultAuthenticateScheme = "ClientCookie";
                config.DefaultSignInScheme = "ClientCookie";
                config.DefaultChallengeScheme = "OurServer";
            })
                .AddCookie("ClientCookie")
                .AddOpenIdConnect("OurServer", config =>
                {
                    // config.Scope.Add("openid");
                    config.Scope.Clear();
                    config.Scope.Add("profile");
                    config.CallbackPath = "/callback";
                    config.ClientId = "f2bded84ec96e658296d29a6fd110094a127f4025cc99e5e";
                    config.ClientSecret = "72ea78fcb5d4791950f169fcdf648994a0a9eac760fd0094a127f4025cc99e5e";
                    config.Authority = "https://oauth-f5.fozzy.ua/f5-oauth2/v1/";
                    config.ResponseType = "code";

                    config.SaveTokens = true;
                });

            services.AddControllersWithViews();
            services.AddHttpClient();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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

#region using
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;
using Common;
using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
#endregion

namespace EasySampleBlazorv2.Server
{
    public class Startup
    {
        private static Type T = typeof(Startup);

        private ILogger<Startup> _logger;
        public IConfiguration Configuration { get; }


        public Startup(IConfiguration configuration)
        {
            using var scope = _logger.BeginMethodScope(new { configuration = configuration .GetLogString()});

            Configuration = configuration; 
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            using var scope = _logger.BeginMethodScope(new { services });

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IParallelService, ParallelService>();
            services.AddClassConfiguration();
            services.AddObservability(Configuration); scope.LogDebug($"services.AddRazorPages();");

            services.AddControllersWithViews(); scope.LogDebug($"services.AddControllersWithViews();");
            services.AddRazorPages(); scope.LogDebug($"services.AddRazorPages();");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            using var scope = _logger.BeginMethodScope(new { app, env = env.GetLogString() });

            scope.LogDebug(new { env.EnvironmentName });
            var isDevelopment = env.IsDevelopment(); scope.LogDebug($"env.IsDevelopment(); returned {isDevelopment}");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage(); scope.LogDebug($"app.UseDeveloperExceptionPage();");
                app.UseWebAssemblyDebugging(); scope.LogDebug($"app.UseWebAssemblyDebugging();");
            }
            else
            {
                app.UseExceptionHandler("/Error"); scope.LogDebug($"app.UseExceptionHandler('/Error');");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts(); scope.LogDebug($"app.UseHsts();");
            }

            app.UseHttpsRedirection(); scope.LogDebug($"app.UseHttpsRedirection();");
            app.UseBlazorFrameworkFiles(); scope.LogDebug($"app.UseBlazorFrameworkFiles();");
            app.UseStaticFiles(); scope.LogDebug($"app.UseStaticFiles();");

            app.UseRouting(); scope.LogDebug($"app.UseRouting();");

            app.UseEndpoints(endpoints =>
            {
                using var scopeInner = TraceLogger.BeginNamedScope(T, "UseEndpoints.ConfigureCallback");
                //using var scopeInner = _logger.BeginNamedScope("ConfigureAppConfigurationCallback");

                endpoints.MapRazorPages(); scopeInner.LogDebug($"endpoints.MapRazorPages();");
                endpoints.MapControllers(); scopeInner.LogDebug($"endpoints.MapControllers();");
                endpoints.MapFallbackToFile("index.html"); scopeInner.LogDebug($"endpoints.MapFallbackToFile('index.html');");
            });
        }
    }
}

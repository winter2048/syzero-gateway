using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SyZero.Gateway
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOpenTelemetry()
            .WithTracing(b => b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(AppConfig.ServerOptions.Name)).AddSource("*")
                .AddAspNetCoreInstrumentation(opt =>
                {
                    opt.Filter = context =>
                    {
                        return context.Request.Path.ToString().StartsWith("/api/");
                    };
                })
                .AddHttpClientInstrumentation().AddConsoleExporter()
                .AddSource("Microsoft.AspNetCore.Hosting"))
            .WithMetrics(b => b.AddMeter("*")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddPrometheusExporter())
            .WithLogging()
            .UseOtlpExporter(OpenTelemetry.Exporter.OtlpExportProtocol.Grpc, new System.Uri("http://aspire-dashboard:18889"));

            services.AddOcelot()//Ocelot如何处理
                .AddConsul<ConsulServiceBuilder>()//支持Consul
                .AddCacheManager(x =>
                {
                    x.WithDictionaryHandle();//默认字典存储
                })
                .AddPolly()
                .AddConfigStoredInConsul();

            services.AddSignalR();

            services.AddSwaggerForOcelot(Configuration);

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors(builder =>
            {
                builder.AllowAnyMethod()
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
            app.UseRouting();
            app.UseSwagger(); 

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.UseStaticFiles();
            app.UseSwaggerForOcelotUI(opt =>
            {
                opt.PathToSwaggerGenerator = "/swagger/docs";
            });
            app.UseWebSockets();
            app.UseOcelot().Wait();
        }
    }
}

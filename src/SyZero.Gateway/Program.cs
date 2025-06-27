using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;
using System.Threading;

namespace SyZero.Gateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var builder = WebApplication.CreateBuilder(args);

            //ʹ��SyZero
            builder.AddSyZero();

            builder.Configuration.AddJsonFile("configuration.json", optional: false, reloadOnChange: true);

            builder.WebHost.UseUrls($"{AppConfig.ServerOptions.Protocol}://*:{AppConfig.ServerOptions.Port}");

            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            }).AddSyZeroLog4Net();

            //ʹ��OpenTelemetryң��2
            builder.Services.AddSyZeroOpenTelemetry();

            builder.Services.AddOcelot() //Ocelot��δ���
             .AddConsul<ConsulServiceBuilder>() //֧��Consul
             .AddCacheManager(x =>
             {
                 x.WithDictionaryHandle(); //Ĭ���ֵ�洢
             })
             .AddPolly()
             .AddConfigStoredInConsul();

            builder.Services.AddSignalR();

            builder.Services.AddSwaggerForOcelot(builder.Configuration);

            builder.Services.AddControllers();

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });

            var app = builder.Build();

            app.UseSyZero();
            if (app.Environment.IsDevelopment())
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
            app.UseStaticFiles();
            app.MapControllers();
            app.UseSwaggerForOcelotUI(opt =>
            {
                opt.PathToSwaggerGenerator = "/swagger/docs";
            });
            app.UseWebSockets();
            app.UseOcelot().Wait();

            app.Run();
        }
    }
}

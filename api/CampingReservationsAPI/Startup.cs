using CampingReservationsAPI.Models;
using CampingReservationsAPI.Services;
using CampingReservationsAPI.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CampingReservationsAPI
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
            services.AddCors();
            services.AddControllers().AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

            // get connection string from env var
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

            // DB models service
            if (connectionString == null)
            {
                services.AddEntityFrameworkNpgsql().AddDbContext<avtokampiContext>(options =>
                    options.UseNpgsql(Configuration.GetConnectionString("Avtokampi"))
                );
            }
            else
            {
                services.AddEntityFrameworkNpgsql().AddDbContext<avtokampiContext>(options =>
                    options.UseNpgsql(connectionString)
                );
            }

            // Repository services
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IRezervacijeRepository, RezervacijeRepository>();

            services.AddRouting(options => options.LowercaseUrls = true);

            // Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Camping reservations microservice API",
                    Version = "v1",
                    Description = "Web API for camping reservations."
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, httpReq) =>
                {
                    var servers = new List<OpenApiServer>();

                    servers.Add(new OpenApiServer { Url = $"http://{httpReq.Host.Value}/camping-reservations" });

                    swagger.Servers = servers;
                });
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/camping-reservations/swagger/v1/swagger.json", "Avtokampi");
                c.RoutePrefix = string.Empty;
            });

            app.UseCors(
                options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
            );

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

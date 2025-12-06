using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Ocelot Konfigürasyonu
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular adresi
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); 
    });
});

// 2. Ocelot Servisleri
builder.Services.AddOcelot(builder.Configuration);




var app = builder.Build();

app.UseCors("AllowAngular");
await app.UseOcelot();

app.Run();

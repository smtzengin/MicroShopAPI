using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Ocelot Konfigürasyonu
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// 2. Ocelot Servisleri
builder.Services.AddOcelot(builder.Configuration);


var app = builder.Build();

// 3. Ocelot Middleware'i
await app.UseOcelot();

app.Run();

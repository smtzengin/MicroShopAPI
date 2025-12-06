using MicroShop.BasketAPI.Services;
using MicroShop.Shared.Extensions; 

var builder = WebApplication.CreateBuilder(args);

// 1. Loglama (Shared Extension)
builder.AddCustomLogging("BasketAPI");

// 2. Authentication (JWT - Shared Extension)
builder.AddCustomJwtAuthentication();

// 3. Redis Cache Entegrasyonu
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MicroShop_"; 
});

// 4. Servis Kaydı
builder.Services.AddScoped<BasketService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5. Middleware Sıralaması
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); 
app.UseAuthorization(); 

app.MapControllers();

app.Run();
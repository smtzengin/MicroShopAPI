using MicroShop.BasketAPI.Services;
using MicroShop.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Loglama
builder.AddCustomLogging("BasketAPI");

// 2. Redis Baðlantýsý
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// 3. Basket Service Kaydý
builder.Services.AddScoped<BasketService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

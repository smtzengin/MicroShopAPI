using MicroShop.Shared.Data;
using MicroShop.Shared.Extensions;
using MicroShop.Shared.Interfaces;
using MicroShop.StockAPI.Data;
using MicroShop.StockAPI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomLogging("StockAPI");

// 1. Veritabanı Bağlantısı
builder.Services.AddDbContext<StockDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(sp =>
{
    var context = sp.GetRequiredService<StockDbContext>();
    return new UnitOfWork(context);
});

// 3. Servisler
builder.Services.AddScoped<StockService>();
builder.Services.AddSingleton<IMessageProducer, RabbitMQProducer>();

// 4. Worker (Arkaplan dinleyicisi)
builder.Services.AddHostedService<StockWorker>();
builder.Services.AddHostedService<ApprovalWorker>();

// 5. CORS (Angular İçin)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Eğer Swashbuckle eklediysen

var app = builder.Build();

// Seed Data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    context.Database.Migrate();
    StockDbSeed.Seed(context); // Verileri bas
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAngular");
app.MapControllers();

app.Run();
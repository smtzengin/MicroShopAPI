using MicroShop.PaymentAPI.Data;
using MicroShop.PaymentAPI.Services;
using MicroShop.Shared.Data;
using MicroShop.Shared.Extensions;
using MicroShop.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomLogging("PaymentAPI");

// 1. DbContext
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(sp =>
{
    var context = sp.GetRequiredService<PaymentDbContext>();
    return new UnitOfWork(context);
});

// 3. Servisler
builder.Services.AddScoped<PaymentService>();
builder.Services.AddHostedService<PaymentWorker>();

builder.Services.AddControllers();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

// Otomatik Migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    context.Database.Migrate();
    PaymentDbSeed.Seed(context);
}

app.Run();
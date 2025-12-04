using MicroShop.OrderAPI.Data;
using MicroShop.OrderAPI.Services;
using MicroShop.Shared.Data;
using MicroShop.Shared.Extensions;
using MicroShop.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine($"[Serilog Hatası]: {msg}"));
var builder = WebApplication.CreateBuilder(args);
builder.AddCustomLogging("OrderAPI");
builder.AddCustomJwtAuthentication();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(sp =>
{
    var context = sp.GetRequiredService<OrderDbContext>();
    return new UnitOfWork(context);
});

builder.Services.AddSingleton<IMessageProducer, RabbitMQProducer>();
builder.Services.AddHostedService<OrderWorker>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAngular"); 

app.UseAuthorization();
app.UseAuthentication();
app.MapControllers();

app.Run();

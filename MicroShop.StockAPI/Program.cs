using MicroShop.Shared.Data;
using MicroShop.Shared.Extensions;
using MicroShop.Shared.Interfaces;
using MicroShop.StockAPI.Data;
using MicroShop.StockAPI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomLogging("StockAPI");
builder.AddCustomJwtAuthentication();

builder.Services.AddDbContext<StockDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(sp =>
{
    var context = sp.GetRequiredService<StockDbContext>();
    return new UnitOfWork(context);
});

builder.Services.AddScoped<StockService>();
builder.Services.AddSingleton<IMessageProducer, RabbitMQProducer>();

builder.Services.AddHostedService<StockWorker>();
builder.Services.AddHostedService<ApprovalWorker>();

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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    context.Database.Migrate();
    StockDbSeed.Seed(context);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAngular");
app.UseAuthorization();
app.UseAuthentication();
app.MapControllers();

app.Run();
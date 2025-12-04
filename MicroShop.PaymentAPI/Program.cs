using MicroShop.PaymentAPI.Data;
using MicroShop.PaymentAPI.Services;
using MicroShop.Shared.Data;
using MicroShop.Shared.Extensions;
using MicroShop.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomLogging("PaymentAPI");
builder.AddCustomJwtAuthentication();

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(sp =>
{
    var context = sp.GetRequiredService<PaymentDbContext>();
    return new UnitOfWork(context);
});

builder.Services.AddScoped<PaymentService>();
builder.Services.AddHostedService<PaymentWorker>();
builder.Services.AddHostedService<IdentityWorker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.UseAuthentication();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    context.Database.Migrate();
    PaymentDbSeed.Seed(context);
}

app.Run();
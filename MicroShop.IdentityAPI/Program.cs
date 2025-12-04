using MicroShop.IdentityAPI.Data;
using MicroShop.IdentityAPI.Entities;
using MicroShop.IdentityAPI.Services;
using MicroShop.Shared.Extensions;
using MicroShop.Shared.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//  Loglama
builder.AddCustomLogging("IdentityAPI");
builder.AddCustomJwtAuthentication();

// Veritabanı
builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5, // 5 kere dene
            maxRetryDelay: TimeSpan.FromSeconds(10), // Her deneme arası 10 sn bekle
            errorNumbersToAdd: null);
    }));

builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<IMessageProducer, RabbitMQProducer>();

// Identity Core Ayarları
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppIdentityDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
    context.Database.Migrate();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    IdentityDbSeed.SeedAsync(userManager, roleManager).Wait();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseAuthentication(); 
app.UseAuthorization();  

app.MapControllers();

app.Run();

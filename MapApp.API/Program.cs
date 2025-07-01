using MapApp.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using MediatR;
using MapApp.Application.Features.MapPoints.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MapApp.API.Services;
using MapApp.Application.Common.Interfaces;
using MapApp.Domain.Entities; // Add this line to include the User class
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// CORS policy adı
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// CORS servisini ekle (test için açık)
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// EF Core + PostGIS
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.UseNetTopologySuite()
    )
);

// MediatR servisi
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(Program).Assembly,
        typeof(CreateMapPointCommand).Assembly
    );
});

// 🔥 JSON ayarları: camelCase verileri karşılamak için case-insensitive yapı
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy
            {
                ProcessDictionaryKeys = true,
                OverrideSpecifiedNames = true
            }
        };
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    });

// JWT Authentication ayarları
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// Kullanıcı servisi
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MapApp API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Örnek: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// CORS middleware en üstte olmalı
app.UseCors(MyAllowSpecificOrigins);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Database.Migrate(); // varsa migration'ları uygula

    if (!db.Users.Any(u => u.Role == "admin"))
    {
        var admin = new AppUser
        {
            
            Username = "admin",
            Email = "admin@mapapp.local",
            PasswordHash = Convert.ToBase64String(Encoding.UTF8.GetBytes("Admin123!")), // ⚠️ Gerçek hash fonksiyonunla değiştir
            Role = "admin"
        };

        db.Users.Add(admin);
        db.SaveChanges();
        Console.WriteLine("✅ Admin user oluşturuldu: admin / Admin123!");
    }
    else
    {
        Console.WriteLine("ℹ️ Admin zaten var.");
    }
}


// Swagger UI sadece Development'ta aktif
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MapApp API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.Run();

using MapApp.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using MediatR;
using MapApp.Application.Features.MapPoints.Commands;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MapApp.API.Services;
using MapApp.Application.Common.Interfaces;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// CORS policy adı
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
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
              .MigrationsAssembly("MapApp.Persistence") // Burada Migration Assembly belirtiyoruz
    )
);

// JWT Authentication
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

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(Program).Assembly,
        typeof(CreateMapPointCommand).Assembly
    );
});

// JSON ayarları
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
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

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

// CurrentUser Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();

// CORS
app.UseCors(MyAllowSpecificOrigins);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Async admin kullanıcı oluşturma fonksiyonu
async Task CreateAdminUserAsync(IServiceProvider services)
{
    var context = services.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();

    var adminEmail = "admin@mapapp.local";
    var adminUsername = "admin";
    var adminRole = "admin";
    var adminPassword = "Admin123!";

    var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail || u.UserName == adminUsername);

    if (adminUser == null)
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword);

        var newAdmin = new MapApp.Domain.Entities.AppUser
        {
            Email = adminEmail,
            UserName = adminUsername,
            Role = adminRole,
            PasswordHash = hashedPassword
        };

        context.Users.Add(newAdmin);
        await context.SaveChangesAsync();

        Console.WriteLine($"✅ Admin kullanıcı oluşturuldu: {adminUsername} / {adminPassword}");
    }
    else
    {
        // Eğer admin zaten varsa şifresini güncelle
        adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
        context.Users.Update(adminUser);
        await context.SaveChangesAsync();

        Console.WriteLine("ℹ️ Admin kullanıcı zaten mevcut, şifre güncellendi.");
    }
}

// Program başlar başlamaz admin kullanıcı oluştur
using (var scope = app.Services.CreateScope())
{
    await CreateAdminUserAsync(scope.ServiceProvider);
}

// Swagger UI sadece Development ortamında aktif olsun
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

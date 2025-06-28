using MapApp.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using MediatR;
using MapApp.Application.Features.MapPoints.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 🌐 EF Core + PostGIS
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.UseNetTopologySuite()
    )
);

// 🧠 MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(Program).Assembly,
        typeof(CreateMapPointCommand).Assembly
    );
});

// ✅ JSON ayarları (Polygon deserialize edilsin diye Newtonsoft kullanılıyor)
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🚀 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MapApp API v1");
        options.RoutePrefix = string.Empty;
    });
}

// ⚙️ Middleware
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 🏁 Uygulamayı başlat
app.Run();

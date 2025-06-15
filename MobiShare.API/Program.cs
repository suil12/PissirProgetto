using Microsoft.EntityFrameworkCore;
using MobiShare.Core.Interfaces;
using MobiShare.Core.Services;
using MobiShare.Infrastructure.Data;
using MobiShare.Infrastructure.Repositories;
using MobiShare.Infrastructure.Services;
using MobiShare.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "MobiShare API", 
        Version = "v1",
        Description = "API per il sistema di gestione sharing mezzi MobiShare"
    });

    // Configurazione JWT per Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Inserisci il token JWT nel formato: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Abilita commenti XML per documentazione
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add Entity Framework
builder.Services.AddDbContext<MobiShareDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<IUtenteRepository, UtenteRepository>();
builder.Services.AddScoped<ICorsaRepository, CorsaRepository>();
builder.Services.AddScoped<IMezzoRepository, MezzoRepository>();
builder.Services.AddScoped<IParcheggioRepository, ParcheggioRepository>();
builder.Services.AddScoped<ISlotRepository, SlotRepository>();

// Register services
builder.Services.AddScoped<IUtenteService, UtenteService>();
builder.Services.AddScoped<ICorsaService, CorsaService>();
builder.Services.AddScoped<IMezzoService, MezzoService>();
builder.Services.AddScoped<IParcheggioService, ParcheggioService>();
builder.Services.AddScoped<IPuntiEcoService, PuntiEcoService>();
builder.Services.AddScoped<ISlotService, SlotService>();
builder.Services.AddScoped<IDeviceMappingService, DeviceMappingService>();

// Configure IoT Gateway settings
builder.Services.Configure<MobiShare.API.Services.IoTGatewayConfig>(
    builder.Configuration.GetSection("IoTGateway"));

// Register IoT Command Service
builder.Services.AddHttpClient<MobiShare.API.Services.IIoTCommandService, MobiShare.API.Services.IoTCommandService>();

// Register MQTT service (HTTP-based communication with IoT Gateway)
builder.Services.AddHttpClient<IMqttService, MobiShare.Infrastructure.Services.HttpMqttService>();

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Serve static files
app.UseStaticFiles();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MobiShareDbContext>();
    try
    {
        context.Database.EnsureCreated();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database creato e popolato con dati di test");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Errore durante la creazione del database");
    }
}

app.Run();
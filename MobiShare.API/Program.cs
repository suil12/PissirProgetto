using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MobiShare.Infrastructure.Data;
using MobiShare.Infrastructure.Repositories;
using MobiShare.Core.Interfaces;
using MobiShare.Core.Services;
using MobiShare.IoT.Services;
using Microsoft.OpenApi.Models;
using MobiShare.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Entity Framework
builder.Services.AddDbContext<MobiShareDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories con nomi italiani
builder.Services.AddScoped<IUtenteRepository, UtenteRepository>();
builder.Services.AddScoped<IMezzoRepository, MezzoRepository>();
builder.Services.AddScoped<IParcheggioRepository, ParcheggioRepository>();
builder.Services.AddScoped<ICorsaRepository, CorsaRepository>();
builder.Services.AddScoped<ISlotRepository, SlotRepository>();

// Services con nomi italiani
builder.Services.AddScoped<IUtenteService, UtenteService>();
builder.Services.AddScoped<IMezzoService, MezzoService>();
builder.Services.AddScoped<IParcheggioService, ParcheggioService>();
builder.Services.AddScoped<ICorsaService, CorsaService>();
builder.Services.AddScoped<IPuntiEcoService, PuntiEcoService>();

// MQTT Services
builder.Services.Configure<MqttConfig>(builder.Configuration.GetSection("Mqtt"));
builder.Services.AddSingleton<IMqttService, ServizioMqtt>();
builder.Services.AddSingleton<EmulatoreHue>();
builder.Services.AddScoped<GestoreEventiIoT>();

// Background Services
builder.Services.AddHostedService<SimulatoreMezzi>();
builder.Services.AddHostedService<SimulatoreSlot>();
builder.Services.AddHostedService<GestoreEventiMqtt>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-secret-key-here-should-be-at-least-32-characters-long";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MobiShare API",
        Version = "v1",
        Description = "API per il sistema di gestione sharing mezzi MobiShare con nomenclatura italiana",
        Contact = new OpenApiContact
        {
            Name = "MobiShare Team",
            Email = "info@mobishare.org"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Esempio: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MobiShare API V1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.DocumentTitle = "MobiShare API - Documentazione";
    });
}

// Middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Serve static files for frontend
app.UseStaticFiles();

// Default route to frontend
app.MapGet("/", () => Results.Redirect("/index.html"));

// Health check endpoint
app.MapGet("/health", () => new { 
    Stato = "Funzionante", 
    Timestamp = DateTime.UtcNow,
    Sistema = "MobiShare v1.0",
    Database = "SQLite",
    MQTT = "Mosquitto"
});

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MobiShareDbContext>();
    
    // Ensure database is created
    await context.Database.EnsureCreatedAsync();
    
    // Start MQTT service
    var mqttService = scope.ServiceProvider.GetRequiredService<IMqttService>();
    await mqttService.AvviaAsync();
    await mqttService.SottoscriviAggiornamentoMezziAsync();
    await mqttService.SottoscriviSensoriSlotAsync();
    
    // Initialize Hue lights for all slots
    var emulatoreHue = scope.ServiceProvider.GetRequiredService<EmulatoreHue>();
    var slots = await context.Slots.Include(s => s.Parcheggio).ToListAsync();
    foreach (var slot in slots)
    {
        emulatoreHue.CreaLampada(slot.Id, $"Slot {slot.Numero} - {slot.Parcheggio.Nome}");
    }
    
    Console.WriteLine("🚀 MobiShare avviato con successo!");
    Console.WriteLine("📋 Entità del sistema:");
    Console.WriteLine("   • Utenti (Clienti e Gestori)");
    Console.WriteLine("   • Mezzi (Bici Muscolari, Bici Elettriche, Monopattini)");
    Console.WriteLine("   • Parcheggi con Slots");
    Console.WriteLine("   • Corse e Punti Eco");
    Console.WriteLine("   • Buoni Sconto");
    Console.WriteLine($"📱 Frontend: http://localhost:{app.Environment.IsDevelopment() ? "5000" : "80"}");
    Console.WriteLine($"📚 API Docs: http://localhost:{app.Environment.IsDevelopment() ? "5000" : "80"}/swagger");
    Console.WriteLine("📡 MQTT Broker: localhost:1883");
    Console.WriteLine("💡 LED Slots: Emulazione Philips Hue attiva");
}

app.Run();

using Microsoft.EntityFrameworkCore;
using MobiShare.Core.Interfaces;
using MobiShare.Core.Services;
using MobiShare.Infrastructure.Data;
using MobiShare.Infrastructure.Repositories;
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
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
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

// Register MQTT service
builder.Services.AddScoped<IMqttService, MobiShare.IoT.Services.ServizioMqtt>();

// Configure MQTT settings
builder.Services.Configure<MobiShare.IoT.Models.MqttConfig>(options =>
{
    options.Server = "localhost";
    options.Port = 1883;
    options.ClientId = "MobiShare-API";
    options.CleanSession = true;
});

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

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MobiShareDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
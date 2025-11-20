using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using QuestPDF.Infrastructure;
using LabClinic.Api.Common;
using LabClinic.Api.Services; 

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// === Conexión a la BD ===
var conn = builder.Configuration.GetConnectionString("LabConn")
           ?? "server=localhost;database=dblaboratorio;user=root;password=;";
builder.Services.AddDbContext<LabDbContext>(opt =>
    opt.UseMySql(conn, ServerVersion.AutoDetect(conn)));

// === Registro de servicio multisucursal ===
builder.Services.AddScoped<ISucursalContext, SucursalContext>();

// === Autenticación JWT ===
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"] ?? "dev-key-12345678901234567890"
                )
            ),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

// === Autorización (con policies para IA) ===
builder.Services.AddAuthorization(options =>
{
    // Lectura del asistente (chat, resúmenes JSON)
    options.AddPolicy("IA.Read", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx =>
                     ctx.User.IsInRole("Administrador")
                  || ctx.User.IsInRole("Médico")
                  || ctx.User.IsInRole("Recepcionista")
                  || ctx.User.HasClaim("perm", "ia.read")));

    // Acciones que ejecutan efectos (enviar correos, jobs, etc.)
    options.AddPolicy("IA.Write", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx =>
                     ctx.User.IsInRole("Administrador")
                  || ctx.User.HasClaim("perm", "ia.write")));

    // Exclusivo de administrador
    options.AddPolicy("IA.Admin", policy =>
        policy.RequireRole("Administrador"));
});

// === Servicio de correo SMTP ===
builder.Services.AddScoped<IEmailService, EmailService>();

// === CORS: política abierta para desarrollo ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy
            .WithOrigins("http://localhost:4200", "https://tusitio.com")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddHttpClient(); //  Requerido para IAController

builder.Services.AddEndpointsApiExplorer();

// === SWAGGER con soporte para JWT ===
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LabClinic.Api",
        Version = "v1",
        Description = "API del sistema de laboratorio clínico"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce tu token JWT en el formato: **Bearer {tu_token}**"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Mostrar encabezado de sucursal en Swagger
    c.OperationFilter<AddSucursalHeaderParameter>();
});

var app = builder.Build();

// Forzar entorno de desarrollo para ver errores completos
app.Environment.EnvironmentName = Environments.Development;

// === Middleware de errores detallados ===
app.UseDeveloperExceptionPage();

// === Swagger ===
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LabClinic API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseRouting();

// CORS SIEMPRE antes de auth
app.UseCors("DevCors");

// Middleware de sucursal DEBE ir antes de la autenticación
app.UseMiddleware<SucursalMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

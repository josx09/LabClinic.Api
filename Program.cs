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

// ============================
// 🔗 CONEXIÓN A LA BASE DE DATOS
// ============================

var conn = builder.Configuration.GetConnectionString("LabConn");

if (string.IsNullOrWhiteSpace(conn))
{
    conn = "server=localhost;database=dblaboratorio;user=root;password=;";
}

builder.Services.AddDbContext<LabDbContext>(opt =>
    opt.UseMySql(conn, ServerVersion.AutoDetect(conn)));


// ============================
// 🏥 MULTISUCURSAL
// ============================

builder.Services.AddScoped<ISucursalContext, SucursalContext>();


// ============================
// 🔐 JWT
// ============================

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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ??
                                       "dev-key-12345678901234567890")
            ),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });


// ============================
// 👮 AUTORIZACIÓN
// ============================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IA.Read", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx =>
                     ctx.User.IsInRole("Administrador")
                  || ctx.User.IsInRole("Médico")
                  || ctx.User.IsInRole("Recepcionista")
                  || ctx.User.HasClaim("perm", "ia.read")));

    options.AddPolicy("IA.Write", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx =>
                     ctx.User.IsInRole("Administrador")
                  || ctx.User.HasClaim("perm", "ia.write")));

    options.AddPolicy("IA.Admin", policy =>
        policy.RequireRole("Administrador"));
});


// ============================
// 📧 CORREO SMTP
// ============================

builder.Services.AddScoped<IEmailService, EmailService>();


// ============================
// 🌍 CORS PARA ANGULAR + AZURE STATIC WEB APPS
// ============================

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://green-water-07b46ba10.3.azurestaticapps.net"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});


// ============================
// ⚙ CONTROLLERS / JSON
// ============================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();


// ============================
// 📘 SWAGGER + JWT
// ============================

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
        Description = "Introduce: Bearer {token}"
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

    c.OperationFilter<AddSucursalHeaderParameter>();
});


// ============================
// 🚀 BUILD
// ============================

var app = builder.Build();


// ============================
// 📘 SWAGGER (FUNCIONA EN PROD + DEV)
// ============================

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LabClinic API v1");
    c.RoutePrefix = "swagger";
});


// ============================
// 📍 EXCEPCIONES SOLO EN DEV
// ============================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}


// ============================
// 🛣 ROUTING
// ============================

app.UseRouting();


// ============================
// 🌍 CORS ANTES DE AUTH
// ============================

app.UseCors("DevCors");


// ============================
// 🏥 MULTISUCURSAL MIDDLEWARE
// ============================

app.UseMiddleware<SucursalMiddleware>();


// ============================
// 🔐 AUTH
// ============================

app.UseAuthentication();
app.UseAuthorization();


// ============================
// 📌 ENDPOINTS
// ============================

app.MapControllers();


// ============================
// ▶ RUN
// ============================

app.Run();

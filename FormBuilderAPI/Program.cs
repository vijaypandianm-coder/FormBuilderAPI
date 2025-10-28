using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Diagnostics.CodeAnalysis;

using FormBuilderAPI.Data;
using FormBuilderAPI.Services;
using FormBuilderAPI.Application.Interfaces;
using FormBuilderAPI.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// Connection strings / settings
// ===============================
var mysqlCs = builder.Configuration.GetConnectionString("MySqlConnection")
                 ?? throw new InvalidOperationException("Missing connection string 'MySqlConnection'.");

// =======================================
// 1) MySQL (EF Core) – Users & Responses
// =======================================
builder.Services.AddDbContext<SqlDbContext>(opt =>
    opt.UseMySql(mysqlCs, ServerVersion.AutoDetect(mysqlCs)));

// =======================================
// 2) MongoDB – Form Layouts
// =======================================
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoSettings"));
builder.Services.AddSingleton<MongoDbContext>();

// =======================================
// 3) App Services (DI)
// =======================================
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FormService>();
builder.Services.AddScoped<ResponseService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AssignmentService>();

builder.Services.AddScoped<IFormService, FormService>();
builder.Services.AddScoped<IFormAppService, FormAppService>();
builder.Services.AddScoped<IResponseAppService, ResponseAppService>();

builder.Services.AddScoped<IResponsesRepository>(_ => new ResponsesRepository(mysqlCs));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// =======================================
// 4) Swagger + JWT "Authorize" button
// =======================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FormBuilder API",
        Version = "v1",
        Description = "Forms in MongoDB, Users/Responses in MySQL"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.\n\nEnter: **Bearer {your_token}**",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type  = ReferenceType.SecurityScheme,
                    Id    = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// =======================================
// 5) JWT Authentication & Authorization
// =======================================
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
    opts.AddPolicy("RequireLearnerOrAdmin", p => p.RequireRole("Admin", "Learner"));
});

// =======================================
// CORS >>> allow Vite dev server (http & https)
// =======================================
var allowedOrigins = new[]
{
    "http://localhost:5173",
    "https://localhost:5173",
    "http://localhost:5174",
    "https://localhost:5174"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()   // fine to keep even if you use Bearer
    );
});
// =======================================
// CORS <<<
// =======================================

// =======================
// 6) Build the app
// =======================
var app = builder.Build();

// =======================================
// 7) Global, single-line JSON error handling
// =======================================
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = ex.Message });
    }
});

app.UseStatusCodePages(async statusCtx =>
{
    var resp = statusCtx.HttpContext.Response;
    resp.ContentType = "application/json";
    await resp.WriteAsJsonAsync(new
    {
        message = resp.StatusCode switch
        {
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            _ => $"HTTP {resp.StatusCode}"
        }
    });
});

// =======================================
// 8) Pipeline
// =======================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DocumentTitle = "FormBuilder API Docs";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FormBuilder API v1");
    });
}

// If your frontend is http://localhost:5173 but this forces HTTPS,
// keep it – it just redirects; CORS policy above includes https://5173 too.
//app.UseHttpsRedirection();

// CORS must be BEFORE auth if you want preflight (OPTIONS) to succeed
app.UseCors("DevCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FormBuilderAPI.Data;
using FormBuilderAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// =======================================
// 1) MySQL (EF Core) – Users & Responses
// =======================================
builder.Services.AddDbContext<SqlDbContext>(opt =>
    opt.UseMySql(
        builder.Configuration.GetConnectionString("MySqlConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MySqlConnection"))
    ));

// =======================================
// 2) MongoDB – Form Layouts
// =======================================
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoSettings"));
builder.Services.AddSingleton<MongoDbContext>(); // singleton client/context for forms

// =======================================
// 3) App Services (DI)
// =======================================
builder.Services.AddScoped<AuthService>();       // SQL users
builder.Services.AddScoped<FormService>();       // Mongo forms
builder.Services.AddScoped<ResponseService>();   // SQL form responses
builder.Services.AddScoped<AuditService>();      // SQL audit logs (optional)

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
var jwtKey     = builder.Configuration["Jwt:Key"]!;
var jwtIssuer  = builder.Configuration["Jwt:Issuer"];
var jwtAudience= builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("RequireAdmin",          p => p.RequireRole("Admin"));
    opts.AddPolicy("RequireLearnerOrAdmin", p => p.RequireRole("Admin", "Learner"));
});

var app = builder.Build();

// =======================================
// 6) Global, single-line JSON error handling
// =======================================
// Converts *any* unhandled exception into: { "message": "..." }
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // choose a sensible status (you can branch on exception types if you like)
        context.Response.StatusCode  = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = ex.Message });
    }
});

// Also return tidy JSON for common status codes (401/403/404 etc.)
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
            _   => $"HTTP {resp.StatusCode}"
        }
    });
});

// =======================================
// 7) Pipeline
// =======================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DocumentTitle  = "FormBuilder API Docs";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FormBuilder API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();   // must be before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
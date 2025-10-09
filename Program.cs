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
        Microsoft.EntityFrameworkCore.ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("MySqlConnection"))
    )
);

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

// MVC & Swagger plumbing
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();      // <- required for Swagger

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

    // JWT Bearer definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.\n\nEnter: Bearer {your_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    // Require token for secured endpoints
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
    // Admin-only policy
    opts.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
    // Learner OR Admin can access
    opts.AddPolicy("RequireLearnerOrAdmin", p => p.RequireRole("Admin", "Learner"));
});

// =======================================
// 6) Build & Pipeline
// =======================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DocumentTitle = "FormBuilder API Docs";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FormBuilder API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();   // <- must be before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
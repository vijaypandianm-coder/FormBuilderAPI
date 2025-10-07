using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;                 // ✅ for Swagger security
using FormBuilderAPI.Data;
using FormBuilderAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// 1) MySQL (Users + Responses)
// ===============================
builder.Services.AddDbContext<SqlDbContext>(opt =>
    opt.UseMySql(
        builder.Configuration.GetConnectionString("MySqlConnection"),
        Microsoft.EntityFrameworkCore.ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("MySqlConnection"))
    ));

// ===============================
// 2) MongoDB (Form Layouts only)
// ===============================
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoSettings"));
builder.Services.AddSingleton<MongoDbContext>();   // used by FormService

// ===============================
// 3) App Services
// ===============================
builder.Services.AddScoped<AuthService>();       // SQL users
builder.Services.AddScoped<FormService>();       // Mongo forms
builder.Services.AddScoped<ResponseService>();   // SQL responses
builder.Services.AddScoped<AuditService>();      // SQL audit (optional)

// ✅ Seeder (creates a default Admin in SQL if missing)
builder.Services.AddScoped<DatabaseSeeder>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ===============================
// 4) Swagger + JWT
// ===============================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FormBuilder API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token.\n\nExample: Bearer eyJhbGciOi..."
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
            Array.Empty<string>()
        }
    });
});

// ===============================
// 5) JWT Auth
// ===============================
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
    opts.AddPolicy("RequireLearnerOrAdmin", p => p.RequireRole("Admin", "Learner"));
});

var app = builder.Build();

// ===============================
// 6) Seed default Admin in SQL
// ===============================
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAdminAsync(); // uses appsettings: Seed:AdminEmail/Password if provided
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
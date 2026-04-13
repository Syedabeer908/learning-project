using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using StackExchange.Redis;
using WebApplication1.Entities;
using WebApplication1.Middlewares;
using WebApplication1.Repository.Implementations;
using WebApplication1.Repository.Interfaces;
using WebApplication1.Seeders;
using WebApplication1.Services;
using WebApplication1.Settings;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adding DbContext with configuration using appsettings.json
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Redis ConnectionMultiplexer (Singleton)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetSection("Redis:ConnectionString")?.Value ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(configuration);
});

// Adding JwtSettings configuration
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("SecretKeys:Jwt")
);

// Adding StartupRoleSettings configuration
builder.Services.Configure<RoleSettings>(
    builder.Configuration.GetSection("StartUpRole"));

// Adding Authorization policies
var adminRole = builder.Configuration["StartUpRole:Admin"] ?? "Admin";

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(adminRole));
});

// Adding scoped (DI) 
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRepository<Risk>, RiskRepository>();
builder.Services.AddScoped<IRepository<Control>, ControlRepository>();
builder.Services.AddScoped<IRepository<RiskControl>, RiskControlRepository>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserDomainService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<RiskService>();
builder.Services.AddScoped<ControlService>();
builder.Services.AddScoped<RiskControlService>();
builder.Services.AddScoped<RedisService>();
builder.Services.AddScoped<RoleSettings>();

// Secret key for signing JWT (keep this safe!)
var jwtSettings = builder.Configuration
    .GetSection("SecretKeys:Jwt")
    .Get<JwtSettings>();

var jwtSettingsKey = jwtSettings?.Key ?? "3d7tt#JbeX!&FY3!%e+XQE8xtrHFcpqc";

var secretKey = Encoding.UTF8.GetBytes(jwtSettingsKey);

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.NameIdentifier
    };
});

// swagger configuration with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token as: Bearer {your token here}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();


var app = builder.Build();

//seeding data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleSettings = scope.ServiceProvider.GetRequiredService<IOptions<RoleSettings>>();
    var seeder = new DbSeeder(context, roleSettings);
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseMiddleware<UserValidationMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();


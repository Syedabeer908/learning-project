using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using StackExchange.Redis;
using System.Security.Claims;
using WebApplication1.Seeders;
using WebApplication1.Middlewares;
using WebApplication1.Entities;
using WebApplication1.Services;
using WebApplication1.Interfaces;
using WebApplication1.Repository;
using WebApplication1.Common.Constants;
using WebApplication1.Data.SoftDelete.Cascade;
using WebApplication1.Data.SoftDelete.Executor;
using WebApplication1.Data.SoftDelete.Services;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adding DbContext with configuration using appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Redis ConnectionMultiplexer (Singleton)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    //var configuration = "localhost:6376";
    var configuration = builder.Configuration.GetSection("Redis:ConnectionString").Value ?? "localhost:6379";
    var options = ConfigurationOptions.Parse(configuration);
    options.AbortOnConnectFail = false; 

    return ConnectionMultiplexer.Connect(options);
});

// Adding AuthConstants
builder.Services.Configure<AuthConstants>(
    builder.Configuration.GetSection("SecretKeys:Jwt")
);

// Adding RoleConstants
builder.Services.Configure<RoleConstants>(
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
builder.Services.AddScoped<RoleConstants>();

builder.Services.AddScoped<SoftDeleteExecutor>();
builder.Services.AddScoped<CascadePlanBuilder>();
builder.Services.AddScoped<SoftDeleteService>();

// Secret key for signing JWT (keep this safe!)
var jwtSettings = builder.Configuration
    .GetSection("SecretKeys:Jwt")
    .Get<AuthConstants>();

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

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/log-.txt",
        rollingInterval: RollingInterval.Day, // new file every day
        retainedFileCountLimit: 7             // keep last 7 days
    )
    .CreateLogger();

// Replace default logging
builder.Host.UseSerilog();

var app = builder.Build();

//seeding data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    context.Database.Migrate();

    var roleSettings = scope.ServiceProvider.GetRequiredService<IOptions<RoleConstants>>();
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

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();


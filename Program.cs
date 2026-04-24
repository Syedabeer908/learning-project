using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using WebApplication1;
using WebApplication1.Common.Constants;
using WebApplication1.Entities;
using WebApplication1.Interfaces;
using WebApplication1.Jobs;
using WebApplication1.Middlewares;
using WebApplication1.Repository;
using WebApplication1.Seeders;
using WebApplication1.Services;
using WebApplication1.Settings;
using WebApplication1.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add User Secret
builder.Configuration.AddUserSecrets<Program>();

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
    
    var configuration = builder.Configuration["Redis:ConnectionString"];
    var options = ConfigurationOptions.Parse(configuration);
    options.AbortOnConnectFail = false; 

    return ConnectionMultiplexer.Connect(options);
});

// Adding AuthConstants
builder.Services.Configure<AuthSettings>(
    builder.Configuration.GetSection("SecretKeys:Jwt")
);

// Adding Authorization policies
var adminRole = builder.Configuration["StartUpRole:Admin"];

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(adminRole));
});

builder.Services
    .AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();

builder.Services.AddValidatorsFromAssemblyContaining<UploadProfileImageRequestValidator>();

// Adding scoped (DI) 
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProviderRepository, ProviderRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRepository<Risk>, RiskRepository>();
builder.Services.AddScoped<IRepository<Control>, ControlRepository>();
builder.Services.AddScoped<IRepository<RiskControl>, RiskControlRepository>();
builder.Services.AddScoped<ISoftRepository, SoftRepository>();
builder.Services.AddScoped<IUserLoginHistoryRepository, UserLoginHistoryRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserDomainService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<RiskService>();
builder.Services.AddScoped<ControlService>();
builder.Services.AddScoped<RiskControlService>();
builder.Services.AddScoped<RedisService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<ProviderService>();
builder.Services.AddScoped<GoogleService>();
builder.Services.AddScoped<AuthBackgroundJobs>();

builder.Services.AddScoped<RoleSettings>();
builder.Services.AddScoped<EmailSettings>();
builder.Services.AddScoped<FileSettings>();
builder.Services.AddScoped<AuthSettings>();
builder.Services.AddScoped<GoogleSettings>();

builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddHangfireServer();

var jwtSettingsKey = builder.Configuration["SecretKeys:Jwt"];

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

// Adding Api Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// swagerr configuration class
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

var app = builder.Build();

//seeding data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    context.Database.Migrate();

    var roleSettings = scope.ServiceProvider.GetRequiredService<IOptions<RoleSettings>>();
    var seeder = new DbSeeder(context, roleSettings);

    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var desc in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{desc.GroupName}/swagger.json",
                desc.GroupName.ToUpperInvariant()
            );
        }
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseMiddleware<UserValidationMiddleware>();

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthorization();

// Adding Hangfire Dashboard
app.UseHangfireDashboard();

app.MapControllers();

app.Run();


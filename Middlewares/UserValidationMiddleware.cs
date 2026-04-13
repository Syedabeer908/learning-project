using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using WebApplication1.Entities;
using WebApplication1.Services;

namespace WebApplication1.Middlewares
{
    public class UserValidationMiddleware   
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _provider;
        private readonly ILogger<UserValidationMiddleware> _logger;

        public  UserValidationMiddleware(RequestDelegate next, IServiceProvider provider, ILogger<UserValidationMiddleware> logger)
        {
            _next = next;
            _provider = provider;
            _logger = logger;
        }

        private async Task<User?> GetUserWithCacheAsync(Guid userId, string prefix)
        {
            using var scope = _provider.CreateScope();
            var RedisService = scope.ServiceProvider.GetRequiredService<RedisService>();

            var cachedUser = await RedisService.GetAsync(prefix, userId);

            if (!string.IsNullOrEmpty(cachedUser))
            {
                _logger.LogInformation($"Cache hit for user {userId}");
                return JsonSerializer.Deserialize<User>(cachedUser);
            }

            var adminService = scope.ServiceProvider.GetRequiredService<AdminService>();

            var user = await adminService.GetByIdInEntityAsync(userId);

            if (user == null)
                return null;

           
            var json = JsonSerializer.Serialize(new
            {
                IsActive = user.IsActive,
                TokenVersion = user.TokenVersion
            });

            await RedisService.SetAsync(prefix, userId, json, TimeSpan.FromHours(1));

            return user;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tokenVersionClaim = context.User.FindFirst(ClaimTypes.Version)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId) || !int.TryParse(tokenVersionClaim, out var tokenVersionFromToken))
            {
                await UnauthorizedResponse(context, "Invalid token.");
                return;
            }

            var prefix = context.User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
            var user = await GetUserWithCacheAsync(userId, prefix);

            if (user == null || !user.IsActive)
            {
                await ForbiddenResponse(context, "You are not allowed to access this resource.");
                return;
            }

            if (user.TokenVersion != tokenVersionFromToken)
            {
                await UnauthorizedResponse(context, "Token has been invalidated. Please login again.");
                return;
            }

            await _next(context);
        }

        private static async Task WriteJsonResponse(HttpContext context, string message)
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = message }));
        }

        private static Task UnauthorizedResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = 401;
            return WriteJsonResponse(context, message);
        }

        private static Task ForbiddenResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = 403;
            return WriteJsonResponse(context, message);
        }
    }
}

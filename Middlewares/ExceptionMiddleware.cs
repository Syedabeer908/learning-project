using System.Text.Json;
using WebApplication1.Exceptions;

namespace WebApplication1.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (NotFoundException ex)
            {
                context.Response.StatusCode = 404;
                await WriteError(context, ex.Message);
            }
            catch (BadRequestException ex)
            {
                context.Response.StatusCode = 400;
                await WriteError(context, ex.Message);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await WriteError(context, $"Internal server error: {ex.Message}");
            }
        }

        private static async Task WriteError(HttpContext context, string message)
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = message }));
        }
    }
}

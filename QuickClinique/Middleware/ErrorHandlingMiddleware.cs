using System.Net;
using System.Text.Json;

namespace QuickClinique.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}\nStackTrace: {StackTrace}\nInnerException: {InnerException}", 
                    ex.Message, 
                    ex.StackTrace, 
                    ex.InnerException?.Message);
                
                // Log full exception details for debugging
                Console.WriteLine($"[ERROR] Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"[ERROR] Exception Message: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"[ERROR] Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Don't write to response if it has already started
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            
            var response = new
            {
                error = new
                {
                    message = "An error occurred while processing your request.",
                    details = isDevelopment ? exception.Message : "Internal server error",
                    type = isDevelopment ? exception.GetType().Name : null,
                    stackTrace = isDevelopment ? exception.StackTrace : null,
                    innerException = isDevelopment && exception.InnerException != null 
                        ? exception.InnerException.Message 
                        : null
                }
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}

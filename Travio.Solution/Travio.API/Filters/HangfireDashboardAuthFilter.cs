using Hangfire.Dashboard;
using System.Net.Http.Headers;
using System.Text;

namespace Travio.API.Filters;

public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    private readonly string _user;
    private readonly string _pass;

    public HangfireDashboardAuthFilter(string user, string pass)
    {
        _user = user;
        _pass = pass;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var authHeader = httpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            SetUnauthorizedResponse(httpContext);
            return false;
        }

        try
        {
            var credentials = Encoding.UTF8.GetString(
                Convert.FromBase64String(authHeader.Substring("Basic ".Length).Trim()));

            var parts = credentials.Split(':', 2);

            if (parts.Length == 2 && parts[0] == _user && parts[1] == _pass)
            {
                return true;
            }
        }
        catch
        {
            // Invalid base64 or format
        }

        SetUnauthorizedResponse(httpContext);
        return false;
    }

    private static void SetUnauthorizedResponse(HttpContext httpContext)
    {
        httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
        httpContext.Response.StatusCode = 401;
    }
}

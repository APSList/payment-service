namespace payment_service;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public interface IOrganizationContext
{
    int OrganizationId { get; }
    string? Role { get; }
    string? UserId { get; }
    string? Email { get; }
}

public class OrganizationContext : IOrganizationContext
{
    private readonly IHttpContextAccessor _http;

    public OrganizationContext(IHttpContextAccessor http)
    {
        _http = http;
    }

    public int OrganizationId
    {
        get
        {
            var user = _http.HttpContext?.User;
            var org = user?.FindFirstValue("organization_id");
            if (string.IsNullOrWhiteSpace(org) || !int.TryParse(org, out var orgId) || orgId <= 0)
                throw new InvalidOperationException("Missing organization_id in token.");
            return orgId;
        }
    }

    public string? Role => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
    public string? UserId => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? Email => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
}


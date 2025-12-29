using System.Security.Claims;
using BookingService.Api.Services.Interfaces;
using BookingService.Api.Exceptions;

namespace BookingService.Api.Services;

public class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
{
    private static readonly AsyncLocal<UserClaimsCache> _cache = new();

    private class UserClaimsCache
    {
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
        public string? Role { get; set; }
        public bool? IsCustomer { get; set; }
        public bool IsInitialized { get; set; }
    }

    private UserClaimsCache Cache => _cache.Value ??= new UserClaimsCache();
    private static class ClaimNames
    {
        public const string UserId = "user_id";
        public const string TenantId = "tenant_id";
        public const string Role = "role";
    }

    private static class RoleValues
    {
        public const string Customer = "Customer";
        public const string Provider = "Provider";
    }

    public Guid? GetTenantId()
    {
        if (Cache.IsInitialized && Cache.TenantId.HasValue)
        {
            return Cache.TenantId;
        }

        var tenantIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimNames.TenantId)?.Value;

        Guid? tenantId = null;
        if (!string.IsNullOrEmpty(tenantIdClaim))
        {
            if (Guid.TryParse(tenantIdClaim, out var parsedId))
            {
                tenantId = parsedId;
            }
        }

        Cache.TenantId = tenantId;
        Cache.IsInitialized = true;
        return tenantId;
    }

    public Guid GetUserId()
    {
        if (Cache.IsInitialized && Cache.UserId.HasValue)
        {
            return Cache.UserId.Value;
        }

        var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimNames.UserId)?.Value
                       ?? httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new AuthenticationException("JWT", "Invalid or missing user_id claim in token");
        }

        Cache.UserId = userId;
        Cache.IsInitialized = true;
        return userId;
    }

    public string GetRole()
    {
        if (Cache.IsInitialized && !string.IsNullOrEmpty(Cache.Role))
        {
            return Cache.Role;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User == null)
        {
            throw new AuthenticationException("JWT", "Invalid or missing user context");
        }

        var roleClaim = httpContext.User.FindFirst(ClaimNames.Role)?.Value
                  ?? httpContext.User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

        if (string.IsNullOrEmpty(roleClaim))
        {
            throw new AuthenticationException("JWT", "Invalid or missing role claim in token");
        }

        var role = roleClaim switch
        {
            "1" => RoleValues.Customer,
            "0" => RoleValues.Provider,
            _ => roleClaim
        };

        Cache.Role = role;
        Cache.IsInitialized = true;
        return role;
    }

    public bool IsCustomer()
    {
        if (Cache.IsInitialized && Cache.IsCustomer.HasValue)
        {
            return Cache.IsCustomer.Value;
        }

        var isCustomer = GetRole().Equals(RoleValues.Customer, StringComparison.OrdinalIgnoreCase);
        Cache.IsCustomer = isCustomer;
        return isCustomer;
    }

    public void ValidateCustomerAccess()
    {
        if (!IsCustomer())
        {
            throw new AuthorizationException("Booking", "write", "Access denied. Customer operations not allowed for Providers.");
        }
    }

    public void ValidateTenantAccess(Guid tenantId, string resource)
    {
        var userTenantId = GetTenantId();
        if (userTenantId == null)
        {
            throw new AuthorizationException(resource, "read", "Access denied. Customers cannot access tenant-specific resources.");
        }
        if (userTenantId.Value != tenantId)
        {
            throw new AuthorizationException(resource, "read", $"Access denied. You are not authorized to access this tenant's {resource}.");
        }
    }
}
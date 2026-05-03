using HireOps.Domain.Interfaces;

namespace HireOps.Api.Context;

public class HttpContextTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _accessor;
    public HttpContextTenantContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid GetTenantId()
    {
        var header = _accessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return Guid.TryParse(header, out var id) ? id : Guid.NewGuid();
    }
}
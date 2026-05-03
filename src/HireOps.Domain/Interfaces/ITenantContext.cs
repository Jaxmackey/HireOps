namespace HireOps.Domain.Interfaces;

public interface ITenantContext
{
    Guid GetTenantId();
}
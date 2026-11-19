namespace LightWeight.DeviceIntegration.Domain.Uow;

public interface IDeviceIntegrationUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

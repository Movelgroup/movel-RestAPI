## Create services to handle business logic:

IEmablerService: Interface for handling Emabler data
EmablerService: Implementation of IEmablerService
INotificationService: Interface for sending data to front-end portals
NotificationService: Implementation of INotificationService

```cs
public interface IEmablerService
{
    Task ProcessChargerState(ChargerState chargerState);
    Task ProcessMeasurements(Measurements measurements);
}

public class EmablerService : IEmablerService
{
    private readonly IRepository _repository;
    private readonly INotificationService _notificationService;

    public EmablerService(IRepository repository, INotificationService notificationService)
    {
        _repository = repository;
        _notificationService = notificationService;
    }

    public async Task ProcessChargerState(ChargerState chargerState)
    {
        await _notificationService.NotifyFrontEnd(chargerState);
        await _repository.StoreChargerState(chargerState);
    }

    // Implementation for ProcessMeasurements
}
```
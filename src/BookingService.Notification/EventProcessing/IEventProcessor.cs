namespace BookingService.Notification.EventProcessing;

public interface IEventProcessor
{
    Task ProcessEventAsync(string message);
}

using BookingService.Notification.Dtos;
using BookingService.Notification.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace BookingService.Notification.EventProcessing;

public class EventProcessor : IEventProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NotificationHub> _hubContext;

    public EventProcessor(IServiceScopeFactory scopeFactory, IHubContext<NotificationHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    public async Task ProcessEventAsync(string message)
    {
        var eventType = DetermineEvent(message);

        switch (eventType)
        {
            case EventType.BookingCreated:
                await SendBookingCreatedNotificationSignal(message);
                break;
            default:
                break;
        }
    }

    private static EventType DetermineEvent(string notificationMessage)
    {
        Console.WriteLine("--> Determining Event");

        var eventType = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);

        switch (eventType.Event)
        {
            case "Booking_Created":
                Console.WriteLine("Booking Created Event Detected");
                return EventType.BookingCreated;
            default:
                Console.WriteLine("--> Could not determine the event type");
                return EventType.Undetermined;
        }
    }

    private async Task SendBookingCreatedNotificationSignal(string bookingCreatedMessage)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var bookingCreated = JsonSerializer.Deserialize<BookingCreatedMessage>(bookingCreatedMessage);

            try
            {
                await _hubContext.Clients.All.SendAsync("BookingCreated", bookingCreated);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not send booking created notification signal {ex.Message}");
            }
        }
    }
}

enum EventType
{
    BookingCreated,
    Undetermined
}

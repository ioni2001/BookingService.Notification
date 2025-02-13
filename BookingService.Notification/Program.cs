using BookingService.Notification.AsyncDataServices;
using BookingService.Notification.EventProcessing;
using BookingService.Notification.Hubs;
public class Program
{
    private static ILogger<Program>? _logger;

    static async Task Main(string[] args)
    {
        IHost appHost = Host
            .CreateDefaultBuilder(args)
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = true;
            })
            .ConfigureLogging((hbc, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices((hbc, services) =>
                {
                    // Add SignalR
                    services.AddSignalR();

                    // Add other services
                    services.AddHostedService<MessageBusSubscriber>();
                    services.AddSingleton<IEventProcessor, EventProcessor>();
                });

                webBuilder.Configure((context, app) =>
                {
                    // Configure the middleware pipeline
                    var env = context.HostingEnvironment;

                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        // Map the SignalR hub
                        endpoints.MapHub<NotificationHub>("/notifications");

                        // Add other endpoint mappings if needed
                    });
                });
            })
            .Build();

        _logger = appHost.Services.GetRequiredService<ILogger<Program>>();
        _logger.LogInformation("App Host created successfully");

        await appHost.RunAsync();
    }
}
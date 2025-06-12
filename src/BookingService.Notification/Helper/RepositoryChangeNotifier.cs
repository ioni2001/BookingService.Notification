using BookingService.Notification.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text;

namespace BookingService.Notification.Helper
{
    public class RepositoryChangeNotifier
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
        private readonly ILogger<RepositoryChangeNotifier> _logger;

        public RepositoryChangeNotifier(
            IServiceScopeFactory scopeFactory,
            IHubContext<NotificationHub, INotificationClient> hubContext,
            ILogger<RepositoryChangeNotifier> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Processes a list of changed file paths and notifies clients about relevant .cs file changes.
        /// </summary>
        public async Task NotifyRelevantChangesAsync(IEnumerable<string> changedFiles)
        {
            var relevant = changedFiles
                .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!relevant.Any())
            {
                _logger.LogInformation("No C# file changes detected—no notifications sent.");
                return;
            }

            await SafeBroadcastAsync("RelevantFilesChanged", new
            {
                Files = relevant,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task SafeBroadcastAsync(string method, object payload)
        {
            try
            {
                await _hubContext.Clients.All.ReceiveNotificationAsync(method, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification broadcast failed on '{Method}'", method);
            }
        }

        /// <summary>
        /// Helper to get file content from a GitHub repo via REST API.
        /// </summary>
        public async Task<string?> FetchFileContentAsync(string owner, string repo, string path, string commitSha)
        {
            using var scope = _scopeFactory.CreateScope();
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            var url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}?ref={commitSha}";
            http.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp");

            try
            {
                var resp = await http.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var content = doc.RootElement.GetProperty("content").GetString();
                return content is null ? null : Encoding.UTF8.GetString(Convert.FromBase64String(content));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed retrieving file '{Path}' at SHA {Sha}", path, commitSha);
                return null;
            }
        }
    }

}

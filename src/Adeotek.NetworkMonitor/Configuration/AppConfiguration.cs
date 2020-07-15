using System.Text.Json;

namespace Adeotek.NetworkMonitor.Configuration
{
    public class AppConfiguration
    {
        public PingConfiguration Ping { get; set; }
        public SpeedTestConfiguration SpeedTest { get; set; }
        public string GoogleSpreadsheetId { get; set; }
        public GoogleCredentials GoogleCredentials { get; set; }

        public string GetGoogleCredentialsJson()
        {
            return JsonSerializer.Serialize(GoogleCredentials);
        }
    }
}

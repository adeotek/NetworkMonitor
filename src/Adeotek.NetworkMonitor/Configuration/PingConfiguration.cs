using System.Collections.Generic;

namespace Adeotek.NetworkMonitor.Configuration
{
    public class PingConfiguration
    {
        public bool Enabled { get; set; } = false;
        public List<string> Targets { get; set; }
        public bool SendToGoogleSpreadsheet { get; set; } = false;
        public string GoogleSheetName { get; set; }
    }
}

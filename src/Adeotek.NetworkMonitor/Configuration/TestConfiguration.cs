using System.Collections.Generic;

namespace Adeotek.NetworkMonitor.Configuration
{
    public class TestConfiguration
    {
        public string Type { get; set; }
        public string Collection { get; set; }
        public string Group { get; set; }
        public List<Dictionary<string, string>> Targets { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace Adeotek.NetworkMonitor.Results
{
    public class OpenedPortResult : ITestResult
    {
        private static readonly Dictionary<string, string> _outputFields = new Dictionary<string, string>
        {
            {"timestamp", "\"timestamp\" timestamp not null"}, 
            {"group", "\"group\" varchar(100) null"},
            {"name", "\"name\" varchar(100) not null"},
            {"host", "\"host\" varchar(100) not null"}, 
            {"port", "\"port\" int not null"}, 
            {"opened", "\"opened\" int not null"}, 
            {"message", "\"message\" varchar(1000) null"}
        };

        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Message { get; set; }

        public OpenedPortResult()
        {
            Success = false;
            Timestamp = DateTime.Now;
            Port = 0;
        }
        
        public OpenedPortResult(string host, int port, string group = null, string name = null)
        {
            Success = false;
            Timestamp = DateTime.Now;
            Group = group;
            Name = name;
            Host = host;
            Port = port;
        }

        public IEnumerable<string> GetFields() =>  _outputFields.Keys.ToList();
        public List<string> GetFieldsForTableCreate() => _outputFields.Values.ToList();
        public bool IsSuccessful() => Success;
        public object GetResult() => Success ? 1 : 0;
        public object GetMessage()=> Message;
        public string ToJson() => JsonSerializer.Serialize(this);
        public string ToCsvLine() => 
            $"\"{Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{Group}\",\"{Name ?? Host}\",\"{Host}\",{Port.ToString()},{(Success ? "1" : "0")},\"{Message}\"";
        public string ToSqlInsertString() => 
            $"('{Timestamp:yyyy-MM-dd HH:mm:ss}',{(Group != null ? $"'{Group}'" : "null")},'{Name ?? Host}','{Host}',{Port.ToString()},{(Success ? "1" : "0")},'{Message}')";
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace Adeotek.NetworkMonitor.Results
{
    public class PingResult : ITestResult
    {
        private static readonly Dictionary<string, string> _outputFields = new Dictionary<string, string>
        {
            {"timestamp", "\"timestamp\" timestamp not null"}, 
            {"group", "\"group\" varchar(100) null"},
            {"name", "\"name\" varchar(100) not null"},
            {"host", "\"host\" varchar(100) not null"}, 
            {"duration", "\"duration\" bigint null"}, 
            {"message", "\"message\" varchar(1000) null"},
            {"address", "\"address\" varchar(50) null"}
        };

        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }
        public long? Duration { get; set; }
        public string Message { get; set; }
        public string Address { get; set; }

        public PingResult()
        {
            Success = false;
            Timestamp = DateTime.UtcNow;
        }
        
        public PingResult(string host, string group = null, string name = null)
        {
            Success = false;
            Timestamp = DateTime.UtcNow;
            Group = group;
            Name = name;
            Host = host;
        }

        public PingResult(PingReply reply, string host = null, string group = null, string name = null)
        {
            Timestamp = DateTime.UtcNow;
            Group = group;
            Name = name;
            Host = host;
            if (reply == null)
            {
                Success = false;
                Message = "No reply received";
                return;
            }

            if (reply.Status != IPStatus.Success)
            {
                Success = false;
                Message = reply.Status.ToString();
            }
            else
            {
                Success = true;
                Duration = reply.RoundtripTime;
            }

            Address = reply.Address?.ToString();
        }

        public IEnumerable<string> GetFields() =>  _outputFields.Keys.ToList();
        public List<string> GetFieldsForTableCreate() => _outputFields.Values.ToList();
        public bool IsSuccessful() => Success;
        public object GetResult() => Success ? Duration : null;
        public object GetMessage()=> Success ? (object) null : Message;
        public string ToJson() => JsonSerializer.Serialize(this);
        public string ToCsvLine() => 
            $"\"{Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{Group}\",\"{Name ?? Host}\",\"{Host}\",{Duration?.ToString() ?? string.Empty},\"{Message}\",\"{Address}\"";
        public string ToSqlInsertString() => 
            $"('{Timestamp:yyyy-MM-dd HH:mm:ss}',{(Group != null ? $"'{Group}'" : "null")},'{Name ?? Host}','{Host}',{(Success && Duration != null ? Duration.ToString() : "null")},'{Message ?? string.Empty}',{(Address != null ? $"'{Address}'" : "null")})";
    }
}
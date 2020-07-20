using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace Adeotek.NetworkMonitor.Results
{
    public class UptimeResult : ITestResult
    {
        private static readonly Dictionary<string, string> _outputFields = new Dictionary<string, string>
        {
            {"timestamp", "\"timestamp\" timestamp not null"}, 
            {"group", "\"group\" varchar(100) null"},
            {"name", "\"name\" varchar(255) not null"},
            {"url", "\"url\" varchar(255) not null"},
            {"up", "\"up\" int not null"},
            {"code", "\"code\" int null"}, 
            {"message", "\"message\" varchar(1000) null"}
        };
        
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }

        public UptimeResult()
        {
            Success = false;
            Timestamp = DateTime.Now;
            Code = 0;
        }
        
        public UptimeResult(string url, string group = null, string name = null)
        {
            Success = false;
            Timestamp = DateTime.Now;
            Group = group;
            Name = name;
            Url = url;
            Code = 0;
        }

        public IEnumerable<string> GetFields() =>  _outputFields.Keys.ToList();
        public List<string> GetFieldsForTableCreate() => _outputFields.Values.ToList();
        public bool IsSuccessful() => Success;
        public object GetResult() => Success ? 1 : 0;
        public object GetMessage()=> $"[{Code.ToString()}] {Message}";
        public string ToJson() => JsonSerializer.Serialize(this);
        public string ToCsvLine() => 
            $"\"{Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{Group}\",\"{Name ?? Url}\",\"{Url}\",{(Success ? "1" : "0")},{Code.ToString()},\"{Message}\"";
        public string ToSqlInsertString() => 
            $"('{Timestamp:yyyy-MM-dd HH:mm:ss}',{(Group != null ? $"'{Group}'" : "null")},'{Name ?? Url}','{Url}',{(Success ? "1" : "0")},{Code.ToString()},'{Message ?? string.Empty}')";
    }
}
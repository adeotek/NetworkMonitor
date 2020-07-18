using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace Adeotek.NetworkMonitor
{
    public class PingResult : ITestResult
    {
        private static readonly string[] _outputFields = {"timestamp", "target", "duration", "message"};
        
        public string Target { get; set; }
        public bool Success { get; set; }
        public long Time { get; set; }
        public string Message { get; set; }
        public string Address { get; set; }

        public PingResult()
        {
            Success = false;
            Time = -1;
        }
        
        public PingResult(string target)
        {
            Target = target;
            Success = false;
            Time = -1;
        }

        public PingResult(PingReply reply, string target = null)
        {
            Target = target;
            if (reply == null)
            {
                Success = false;
                Time = -1;
                Message = "No reply received";
                return;
            }

            if (reply.Status != IPStatus.Success)
            {
                Success = false;
                Time = -1;
                Message = reply.Status.ToString();
            }
            else
            {
                Success = true;
                Time = reply.RoundtripTime;
            }

            Address = reply.Address?.ToString();
        }

        public List<string> GetOutputFields()
        {
            return _outputFields.ToList();
        }
        
        public bool IsSuccessful()
        {
            return Success;
        }
        
        public object GetResult()
        {
            return Success ? Time : (object) null;
        }

        public object GetMessage()
        {
            return Success ? (object) null : Message;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public string ToCsvLine()
        {
            return $"\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\"{Target}\",{Time.ToString()},\"{Message}\"";
        }

        public string ToSqlInsertString()
        {
            return
                $"('{DateTime.Now:yyyy-MM-dd HH:mm:ss}','{Target}',{(Success ? Time.ToString() : "null")},'{Message ?? string.Empty}')";
        }
    }
}
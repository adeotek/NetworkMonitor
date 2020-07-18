using System.Collections.Generic;

namespace Adeotek.NetworkMonitor
{
    public interface ITestResult
    {
        List<string> GetOutputFields();
        bool IsSuccessful();
        object GetResult();
        object GetMessage();
        string ToJson();
        string ToCsvLine();
        string ToSqlInsertString();
    }
}
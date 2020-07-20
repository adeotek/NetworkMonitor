using System.Collections.Generic;

namespace Adeotek.NetworkMonitor.Results
{
    public interface ITestResult
    {
        IEnumerable<string> GetFields();
        List<string> GetFieldsForTableCreate();
        bool IsSuccessful();
        object GetResult();
        object GetMessage();
        string ToJson();
        string ToCsvLine();
        string ToSqlInsertString();
    }
}
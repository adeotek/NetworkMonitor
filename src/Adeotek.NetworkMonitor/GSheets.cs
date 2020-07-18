using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Adeotek.NetworkMonitor
{
    public class GSheets
    {
        private const string ApplicationName = "AdeoTEK Network Monitor";

        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        private readonly string[] _scopes = {SheetsService.Scope.Spreadsheets};
        private readonly SheetsService _sheetsService;

        public GSheets(Dictionary<string, string> config, string appPath, string spreadsheetId = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            var credentialsFile = config.ContainsKey("CredentialsFile") ? config["CredentialsFile"] : null;
            if (credentialsFile == null || credentialsFile.Trim().Length == 0)
            {
                throw new Exception($"Invalid credentials file name/path: {credentialsFile}");
            }

            if (!Path.IsPathRooted(credentialsFile))
            {
                credentialsFile = Path.Join(appPath, credentialsFile);
            }

            if (!File.Exists(credentialsFile))
            {
                throw new Exception($"Invalid or missing credentials file: {credentialsFile}");
            }

            using var stream = new FileStream(credentialsFile, FileMode.Open, FileAccess.Read);
            var serviceInitializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = GoogleCredential.FromStream(stream).CreateScoped(_scopes)
            };
            _sheetsService = new SheetsService(serviceInitializer);
            SpreadsheetId = spreadsheetId ?? (config.ContainsKey("SpreadsheetId") ? config["SpreadsheetId"] : null);
        }

        public GSheets(string credentialsJson, string spreadsheetId)
        {
            var serviceInitializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = GoogleCredential.FromJson(credentialsJson).CreateScoped(_scopes)
            };
            _sheetsService = new SheetsService(serviceInitializer);
            SpreadsheetId = spreadsheetId;
        }

        public string SpreadsheetId { get; set; }

        public bool CreateSheetIfMissing(string sheetName, string spreadsheetId = null)
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                throw new Exception("Invalid or empty sheet name!");
            }

            if (string.IsNullOrEmpty(spreadsheetId ?? SpreadsheetId))
            {
                throw new Exception("Invalid or empty Spreadsheet ID!");
            }

            try
            {
                var spreadsheetRequest = _sheetsService.Spreadsheets.Get(spreadsheetId ?? SpreadsheetId);
                if (spreadsheetRequest == null)
                {
                    throw new Exception("Spreadsheet not found!");
                }

                var spreadsheet = spreadsheetRequest.Execute();
                var sheet = (from s in spreadsheet.Sheets
                    where s.Properties.Title == sheetName
                    select s).FirstOrDefault();
                if (sheet?.Properties.SheetId != null)
                {
                    return true;
                }

                var addSheetRequest = new AddSheetRequest
                {
                    Properties = new SheetProperties
                        {Title = sheetName, GridProperties = new GridProperties {RowCount = 60000, ColumnCount = 26}}
                };
                var batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request> {new Request {AddSheet = addSheetRequest}}
                };
                var batchUpdateRequest =
                    _sheetsService.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest,
                        spreadsheetId ?? SpreadsheetId);
                var response = batchUpdateRequest.Execute();
                return response != null;
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to open spreadsheet [{spreadsheetId ?? SpreadsheetId}]: {e.Message}");
            }
        }

        public IList<IList<object>> ReadRange(string range, string sheetName, string spreadsheetId = null)
        {
            if (string.IsNullOrEmpty(spreadsheetId ?? SpreadsheetId))
            {
                throw new Exception("Invalid or empty Spreadsheet ID!");
            }

            try
            {
                var request =
                    _sheetsService.Spreadsheets.Values.Get(spreadsheetId ?? SpreadsheetId, GetRange(range, sheetName));
                return request.Execute().Values;
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Unable to read range [{GetRange(range, sheetName)}] from spreadsheet [{spreadsheetId ?? SpreadsheetId}]: {e.Message}");
            }
        }

        public int WriteRange(IList<IList<object>> values, string range, string sheetName, string spreadsheetId = null)
        {
            if (string.IsNullOrEmpty(spreadsheetId ?? SpreadsheetId))
            {
                throw new Exception("Invalid or empty Spreadsheet ID!");
            }

            try
            {
                var update = _sheetsService.Spreadsheets.Values.Update(new ValueRange {Values = values},
                    spreadsheetId ?? SpreadsheetId, GetRange(range, sheetName));
                update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                return update.Execute().UpdatedRows ?? 0;
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Unable to write data to range [{GetRange(range, sheetName)}] in spreadsheet [{spreadsheetId ?? SpreadsheetId}]: {e.Message}");
            }
        }

        private static string GetRange(string range, string sheetName)
        {
            return $"'{sheetName}'!{range}";
        }

        public static char GetNextLetter(char currentLetter, int offset = 1)
        {
            return currentLetter switch
            {
                'z' => 'a',
                'Z' => 'A',
                _ => (char) (currentLetter + offset)
            };
        }
    }
}
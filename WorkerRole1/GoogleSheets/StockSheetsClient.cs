using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;

namespace WorkerRole1.GoogleSheets
{
    public class StockSheetsClient
    {
        public const string ApplicationName = "CW3StockTelegramBot";
        
        public const string BeginColumnRegexGroupName = "beginColumn";
        public const string BeginRowRegexGroupName = "beginRow";
        public const string EndColumnRegexGroupName = "endColumn";

        public const string UserNameRegexGroupName = "userName";
        public const string UserIdRegexGroupName = "userId";

        private const string KnownUserColumnTemplate = "@{0} ({1})";
        private const string UnknownUserColumnTemplate = "@{0}";

        private const string ConfigCellTemplate = "{0}:{1}";

        // TODO: Numbers should begins with non-zero
        private static readonly Regex ConfigCellRegex = new Regex(
            $"^(?<{BeginColumnRegexGroupName}>[A-Z,a-z]+)" +
            $"(?<{BeginRowRegexGroupName}>[0-9]+)" + ":" +
            $"(?<{EndColumnRegexGroupName}>[A-Z,a-z]+)");

        // TODO: Numbers should begins with non-zero
        private static readonly Regex RegexKnownUserName = new Regex(
            $"^@(?<{UserNameRegexGroupName}>[A-Z,a-z,0-9,_,-]+)" + "(?:[\\s]+)" +
            $"\\((?<{UserIdRegexGroupName}>[0-9]+)\\)");
        private static readonly Regex RegexUnknownUserName = new Regex(
            $"^@(?<{UserNameRegexGroupName}>[A-Z,a-z,0-9,_,-]+)");

        private readonly SheetClient sheetClient;
        private readonly Cell beginCell;
        private readonly string endColumn;

        public DataTable table;
        private Dictionary<int, string> knownUsers;
        private List<string> unknownUsers;
        public List<string> rowNames;

        public StockSheetsClient(
            SecureString jsonCredentials,
            string sheetId,
            string sheetPage,
            Cell configCell)
        {
            this.sheetClient = new SheetClient(ApplicationName, jsonCredentials, sheetId, sheetPage);
            Tuple<Cell, string> configCells = this.LoadConfigCell(configCell);
            this.beginCell = configCells.Item1;
            this.endColumn = configCells.Item2;
            this.table = this.LoadStockTable(
                this.beginCell,
                this.endColumn,
                out this.knownUsers,
                out this.unknownUsers,
                out this.rowNames);
        }

        public void AddResource(
            int userId,
            string userName,
            string resourceName,
            int resourceCount,
            out List<string> messagesToLog)
        {
            messagesToLog = new List<string>();
            string messageToLog;
            this.RecognizeUser(userId, userName, out messageToLog);
            if (!string.IsNullOrWhiteSpace(messageToLog))
            {
                messagesToLog.Add(messageToLog);
            }

            this.RecognizeResource(resourceName, out messageToLog);
            if (!string.IsNullOrWhiteSpace(messageToLog))
            {
                messagesToLog.Add(messageToLog);
            }

            int colInd = this.GetColumnIndex(userName);
            int rowInd = this.GetRowIndex(resourceName);
            this.table.Rows[rowInd][colInd] = Convert.ToInt32(this.table.Rows[rowInd][colInd]) + resourceCount;
        }

        /// <returns>Saved table to log</returns>
        public IList<IList<object>> Save()
        {
            IList<IList<object>> cells = this.ConvertToWrite();
            this.sheetClient.Write(this.beginCell, cells);
            return cells;
        }

        public string ToLogMessage()
        {
            string message = string.Empty;
            message += "Row names: " + string.Join(";", this.rowNames) + "\n";
            message += "Known users: " + 
                string.Join(";", this.knownUsers.Select(x => x.Key + "=" + x.Value).ToArray()) + "\n";
            message += "Unknown users: " + string.Join(";", this.rowNames) + "\n";
            message += "Talbe: ";
            foreach (DataRow row in this.table.Rows)
            {
                foreach (DataColumn col in table.Columns)
                {
                    message += row[col].ToString() + "\t";
                }

                message += "\n";
            }
            message += "\n";

            return message;
        }

        private void RecognizeUser(int id, string name, out string messageToLog)
        {
            messageToLog = null;

            if (this.knownUsers.ContainsKey(id))
            {
                if (!this.knownUsers[id].Equals(name))
                {
                    messageToLog = $"User name was changed: @{this.knownUsers[id]} => @{name}";
                    this.knownUsers[id] = name;
                }
                return;
            }

            if (this.unknownUsers.Contains(name))
            {
                this.unknownUsers.Remove(name);
                this.knownUsers.Add(id, name);
            }
            else
            {
                messageToLog = $"New user @{name} ({id}) was added";
                this.knownUsers.Add(id, name);
                this.table.Columns.Add(new DataColumn(name, typeof(int))
                {
                    DefaultValue = 0
                });
                StockSheetsClient.IncrementColumnName(this.endColumn);

            }
        }

        private void RecognizeResource(string name, out string messageToLog)
        {
            messageToLog = null;

            if (this.rowNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            messageToLog = $"New resource {name} was added.";
            this.rowNames.Add(name);
            this.table.Rows.Add(this.table.NewRow());
        }

        private int GetColumnIndex(string userName)
        {
            return this.table.Columns[userName].Ordinal;
        }

        private int GetRowIndex(string resourceName)
        {
            return this.rowNames.FindIndex(x => x.Equals(resourceName, StringComparison.OrdinalIgnoreCase));
        }

        private Tuple<Cell,string> LoadConfigCell(Cell configCell)
        {
            string value = this.sheetClient.Read(configCell).ToString();
            Match match = StockSheetsClient.ConfigCellRegex.Match(value);
            if (!match.Success)
            {
                throw new StockSheetsClientException(
                    $"Unable to parse config cell {configCell} with value {value}." +
                    $" Value should match the next regex: {StockSheetsClient.ConfigCellRegex} (ex: A2:M).");
            }

            return new Tuple<Cell, string>(
                new Cell(
                    match.Groups[BeginColumnRegexGroupName].Value.ToUpper(),
                    int.Parse(match.Groups[BeginRowRegexGroupName].Value)),
                match.Groups[EndColumnRegexGroupName].Value.ToUpper());
        }

        private DataTable LoadStockTable(
            Cell beginCell,
            string endColumn,
            out Dictionary<int, string> knownUsers,
            out List<string> unknownUsers,
            out List<string> rowNames)
        {
            DataTable result = new DataTable();
            knownUsers = new Dictionary<int, string>();
            unknownUsers = new List<string>();
            rowNames = new List<string>();

            List<IList<object>> cells = this.sheetClient.Read(beginCell, endColumn).ToList();
            if (!cells.Any() || !cells[0].Any())
            {
                return result;
            }

            List<string> columnNames = cells[0].Select(x => x.ToString()).ToList();
            columnNames.RemoveAt(0);
            List<string> dataTableColumnNames =
                this.ParseUserDictionary(columnNames, out knownUsers, out unknownUsers);
            result.Columns.AddRange(dataTableColumnNames
                .Select(x => new DataColumn(x, typeof(int)) { DefaultValue = 0 }).ToArray());

            cells.RemoveAt(0);

            rowNames = cells.Select(x => x[0].ToString().Trim()).ToList();
            cells.ForEach(x => x.RemoveAt(0));
            foreach (int[] row in cells.Select(x => x.Select(
                c => c.ToString() == string.Empty ? 0 : int.Parse(c.ToString())).ToArray()))
            {
                result.Rows.Add(row.Select(x => x as object).ToArray());
            }

            return result;
        }

        private List<string> ParseUserDictionary(
            IList<string> userList,
            out Dictionary<int, string> knownUsers,
            out List<string> unknownUsers)
        {
            List<string> result = new List<string>();
            knownUsers = new Dictionary<int, string>();
            unknownUsers = new List<string>();

            if (!userList.Any())
            {
                return result;
            }

            foreach (string name in userList)
            {
                Match match = StockSheetsClient.RegexKnownUserName.Match(name);
                if (match.Success)
                {
                    knownUsers[int.Parse(match.Groups[UserIdRegexGroupName].Value)] =
                        match.Groups[UserNameRegexGroupName].Value;
                    result.Add(match.Groups[UserNameRegexGroupName].Value);
                    continue;
                }

                match = StockSheetsClient.RegexUnknownUserName.Match(name);
                if(match.Success)
                {
                    unknownUsers.Add(match.Groups[UserNameRegexGroupName].Value);
                    result.Add(match.Groups[UserNameRegexGroupName].Value);
                    continue;
                }

                throw new StockSheetsClientException(
                    $"Unable to parse column name {name}. " +
                    $"Column name should match the next regex: {StockSheetsClient.RegexUnknownUserName} (ex: @userName (123))" +
                    $"or {StockSheetsClient.RegexUnknownUserName} (ex: @userName)).");
            }

            return result;
        }

        private IList<IList<object>> ConvertToWrite()
        {
            List<IList<object>> cells = new List<IList<object>>();

            List<object> columnNames = 
                this.table.Columns.Cast<DataColumn>().Select(x => x.ColumnName as object).ToList();
            for (int i = 0; i < columnNames.Count; i++)
            {
                KeyValuePair<int, string> user = this.knownUsers.FirstOrDefault(x => x.Value.Equals(columnNames[i]));
                if (user.Equals(default(KeyValuePair<int, string>)))
                {
                    columnNames[i] = string.Format(UnknownUserColumnTemplate, columnNames[i]);
                }
                else
                {
                    columnNames[i] = string.Format(KnownUserColumnTemplate, user.Value, user.Key);
                }
            }
            cells.Add(columnNames);

            cells.AddRange(from row in this.table.AsEnumerable()
                           select row.ItemArray.Select(x => (x.ToString().Equals("0") ? string.Empty : x.ToString())).ToList<object>());

            cells[0].Insert(0, this.GetNewConfigCellValue());
            for (int i = 1; i < cells.Count; i++)
            {
                cells[i].Insert(0, this.rowNames[i - 1]);
            }

            return cells;
        }

        private string GetNewConfigCellValue()
        {
            return string.Format(
                ConfigCellTemplate,
                this.beginCell,
                GoogeSheetsColumnName(this.table.Columns.Count + 1));
        }

        private static string IncrementColumnName(string name)
        {
            if(name.Length == 0)
            {
                return "A";
            }

            if (name[name.Length - 1] == 'Z')
            {
                return IncrementColumnName(name.Substring(0, name.Length - 1)) + 'A';
            }

            name = name.Substring(0, name.Length - 1) + (char)((int)(name[name.Length - 1]) + 1);
            return name;
        }

        private static string GoogeSheetsColumnName(int ind)
        {
            int a = (int)'A';
            string result = string.Empty;
            while (ind > 0)
            {
                int temp = (ind - 1) % 26;
                result = (char)(temp + a) + result;
                ind = (ind - temp - 1) / 26;
            }
            return result;
        }
    }
}

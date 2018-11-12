using System.Text.RegularExpressions;

namespace HeroBot.GoogleSheets
{
    public class Cell
    {
        public const string ColumnRegexGroupName = "column";
        public const string RowRegexGroupName = "row";

        // TODO: Numbers should begins with non-zero
        private static Regex CellRegex = new Regex($"(?<{ColumnRegexGroupName}>[A-Z,a-z]*)(?<{RowRegexGroupName}>[0-9]*)");

        public Cell(string column, int row)
        {
            this.Column = column.ToUpper();
            this.Row = row;
        }

        public Cell(string cellString)
        {
            Match match = Cell.CellRegex.Match(cellString);
            this.Column = match.Groups[ColumnRegexGroupName].Value.ToUpper();
            this.Row = int.Parse(match.Groups[RowRegexGroupName].Value);
        }

        public string Column { get; set; }

        public int Row { get; set; }

        public override string ToString()
        {
            return $"{this.Column}{this.Row}";
        }
    }
}

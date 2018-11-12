using System;

namespace HeroBot.GoogleSheets
{
    public class StockSheetsClientException : Exception
    {
        public StockSheetsClientException()
            : base()
        {
        }

        public StockSheetsClientException(string message)
            : base(message)
        {
        }
    }
}

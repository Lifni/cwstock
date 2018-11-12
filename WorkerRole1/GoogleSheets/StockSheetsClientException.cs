using System;

namespace WorkerRole1.GoogleSheets
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

using System;

namespace WorkerRole1.GoogleSheets
{
    public class SheetsClientException : Exception
    {
        public SheetsClientException()
            : base()
        {
        }

        public SheetsClientException(string message)
            : base(message)
        {
        }
    }
}

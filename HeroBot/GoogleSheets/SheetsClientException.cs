using System;

namespace HeroBot.GoogleSheets
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

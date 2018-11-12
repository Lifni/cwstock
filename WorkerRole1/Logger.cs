using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Security;
using Telegram.Bot.Types;
using WorkerRole1.Azure;

namespace WorkerRole1
{
    public class Logger
    {
        private const string FolderName = "Logs";
        private readonly string fileName;

        private readonly BlobContainerManager blobManager;

        public Logger(SecureString blobConnectionString, string containerName)
        {
            this.blobManager = new BlobContainerManager(
                blobConnectionString,
                containerName);
            this.fileName = Logger.GetLogFileName();
            this.blobManager.CreateAppendBlob(Path.Combine(FolderName, fileName));
        }

        public void Log(int eventId, long chatId, Message message)
        {
            this.Log(eventId, chatId, GetLogMessage(message));
        }

        public void Log(int eventId, long chatId, IList<IList<object>> cells)
        {
            this.Log(eventId, chatId, GetLogMessage(cells));
        }

        public void LogError(int eventId, long chatId,  string message, Exception ex)
        {
            this.Log(eventId, chatId, GetLogErrorMessage(message, ex, true));
        }

        public void Log(int eventId, long chatId, string message)
        {
            string timeStamp = DateTime.Now.ToUniversalTime().ToString("hh:mm:ss MM/dd/yyyy");
            this.blobManager.Append(
                FolderName,
                this.fileName,
                $"[{timeStamp}] {chatId}: {eventId}: {message}{Environment.NewLine}");
        }

        public static string GetLogMessage(Message message)
        {
            return $"Message from ({message.From.Id}, {message.From.Username}): {message.Text}" +
                            $" (forwarded from: {message.ForwardFrom}).";
        }
        
        public static string GetLogMessage(IList<IList<object>> cells)
        {
            return string.Join("\n", cells.Select(x => string.Join("\t", x.OfType<string>())));
        }

        public static string GetLogErrorMessage(string message, Exception ex, bool includeStackTrace = false)
        {
            string text = "ERROR: " + message;
            if (ex != null)
            {
                text += $" Exception: {ex.GetType()}: {ex.Message}." +
                    (includeStackTrace ? $"StackTrace: {ex.StackTrace}." : string.Empty);
                if (ex.InnerException != null)
                {
                    text += $"Inner exception: {ex.InnerException.GetType()}: {ex.InnerException.Message}." +
                    (includeStackTrace ? $"StackTrace: {ex.InnerException.StackTrace}." : string.Empty);
                }
            }

            return text;
        }

        private static string GetLogFileName()
        {
            return $"logs-{DateTime.Now.ToUniversalTime().ToString("MM-dd-yyyy--hh-mm-ss")}.txt";
        }
    }
}
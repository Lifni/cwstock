using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using WorkerRole1;

namespace HeroBot.Telegram
{
    public class TelegramBot
    {
        public const string ResourceNameRegexGroupName = "resourceName";
        public const string ResourceCountRegexGroupName = "resourceCount";

        public const long ChatWarsChatId = 265204902;

        // TODO: Numbers should begins with non-zero
        public readonly Regex regex = new Regex(
            "(?:Deposited )" +
            $"(?<{ResourceNameRegexGroupName}>.+)" +
            $"(?:\\()(?<{ResourceCountRegexGroupName}>[0-9]+)(?:\\))" +
            "(?: successfully)");


        private List<Tuple<long, string>> messagesToSend = new List<Tuple<long, string>>();
        private Dictionary<long, int> messagesProcessed = new Dictionary<long, int>();

        private readonly Logger logger;
        private readonly TelegramBotClient telegramBotClient;

        private Dictionary<long, StockSheetsClient> stockClients;

        public TelegramBot(Logger logger, SecureString authToken)
        {
            this.logger = logger;
            this.telegramBotClient = new TelegramBotClient(authToken.ToUnsecureString());
            this.telegramBotClient.OnMessage += Bot_OnMessage;
        }

        public bool MessageReceieved { get; set; }

        public void Start(Dictionary<long, StockSheetsClient> stockSheetsClients)
        {
            this.stockClients = stockSheetsClients;
            this.messagesProcessed = this.stockClients.Keys
                                            .Select(x => new KeyValuePair<long, int>(x, 0))
                                            .ToDictionary(x => x.Key, x => x.Value);
            this.telegramBotClient.StartReceiving();
        }

        public void Stop()
        {
            this.telegramBotClient.StopReceiving();
        }

        public void SendMessage(long chatId, string message)
        {
            try
            {
                this.telegramBotClient.SendTextMessageAsync(chatId, message).Wait();
            }
            catch (Exception ex)
            {
                this.logger.LogError(108, chatId, $"Failed to send message: {message}", ex);
            }
        }

        public void SendAllLogMessages()
        {
            this.messagesToSend?.ForEach(x => this.SendMessage(x.Item1, x.Item2));
            this.messagesToSend = new List<Tuple<long, string>>();
            
            foreach(KeyValuePair<long, int> chat in this.messagesProcessed ?? Enumerable.Empty<KeyValuePair<long, int>>())
            {
                this.SendMessage(chat.Key, $"Were processed {chat.Value} messages.");
            }
        }

        private void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            this.MessageReceieved = true;

            this.logger.Log(100, e.Message.Chat.Id, e.Message);

            if (e.Message.Text == null)
            {
                return;
            }

            if (!this.stockClients.ContainsKey(e.Message.Chat.Id))
            {
                return;
            }

            try
            {
                this.ProcessMessage(e.Message, this.stockClients[e.Message.Chat.Id]);
                this.messagesProcessed[e.Message.Chat.Id]++;
            }
            catch (Exception ex)
            {
                string logMessage = $"Failed to process message from {e.Message.From.Username}: {e.Message.Text}.";
                this.logger.LogError(104, e.Message.Chat.Id, logMessage, ex);
                this.messagesToSend.Add(
                    new Tuple<long, string> (e.Message.Chat.Id, Logger.GetLogErrorMessage(logMessage, ex)));
            }
        }

        private void ProcessMessage(Message message, StockSheetsClient stockClient)
        {
            if(message.ForwardFrom?.Id != ChatWarsChatId)
            {
                return;
            }

            Match match = regex.Match(message.Text);
            if (!match.Success)
            {
                string logMessage = $"Failed to parse message from {message.From}: {message.Text}.";
                this.logger.Log(101, message.Chat.Id, logMessage);
                this.messagesToSend.Add(new Tuple<long, string> (message.Chat.Id, logMessage));
                return;
            }
            
            string resourceName = match.Groups[ResourceNameRegexGroupName].Value.Trim();
            int resourceCount = int.Parse(match.Groups[ResourceCountRegexGroupName].Value);

            List<string> messagesToLog;
            stockClient.AddResource(
                message.From.Id,
                message.From.Username,
                resourceName,
                resourceCount,
                out messagesToLog);
            messagesToLog.ForEach(x =>
           {
               this.logger.Log(102, message.Chat.Id, x);
               this.messagesToSend.Add(new Tuple<long, string> (message.Chat.Id, x));
           });

            this.logger.Log(
                103,
                message.Chat.Id,
                $"User {message.From.Username} ({message.From.Id}) deposited {resourceName} ({resourceCount}).");
        }
    }
}

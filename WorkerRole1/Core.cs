using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using WorkerRole1.GoogleSheets;
using WorkerRole1.Telegram;

namespace WorkerRole1
{
    public class Core
    {
        private readonly TimeSpan timeToWait = TimeSpan.FromSeconds(30);

        private readonly Logger logger;
        private readonly Dictionary<long, StockSheetsClient> stockClients;
        private readonly TelegramBot telegramBot;

        public Core(
            Logger logger,
            List<OnboardClient> onboardClients,
            SecureString googleSheetsConfig,
            SecureString telegramToken)
        {
            this.logger = logger;
            this.telegramBot = new TelegramBot(this.logger, telegramToken);
            this.stockClients = new Dictionary<long, StockSheetsClient>();
            foreach (OnboardClient onboardClient in onboardClients)
            {
                try
                {
                    StockSheetsClient stockClient = new StockSheetsClient(
                            googleSheetsConfig,
                            onboardClient.SheetId,
                            onboardClient.SheetPage,
                            new Cell(onboardClient.ConfigCell));
                    this.stockClients.Add(onboardClient.ChatId, stockClient);
                    this.logger.Log(109, onboardClient.ChatId, "Previous data:" + stockClient.ToLogMessage());
                }
                catch (Exception ex)
                {
                    this.logger.LogError(105, onboardClient.ChatId, $"Something wrong.", ex);
                    this.telegramBot.SendMessage(onboardClient.ChatId, Logger.GetLogErrorMessage(
                        "There is an error in your configuration or in your table." +
                        " Bot will not calculate your guild stock this time.", ex));
                }
            }
        }

        public void Run()
        {
            try
            {
                this.telegramBot.Start(this.stockClients);

                do
                {
                    this.telegramBot.MessageReceieved = false;
                    Thread.Sleep(this.timeToWait);
                }
                while (this.telegramBot.MessageReceieved);

                this.telegramBot.Stop();
                Thread.Sleep(this.timeToWait);

                this.telegramBot.SendAllLogMessages();

                foreach (KeyValuePair<long, StockSheetsClient> stockClient in this.stockClients)
                {
                    try
                    {
                        this.logger.Log(109, stockClient.Key, stockClient.Value.ToLogMessage());
                        IList<IList<object>> table = stockClient.Value.Save();
                        this.logger.Log(104, stockClient.Key, table);
                        this.telegramBot.SendMessage(stockClient.Key, "Table was updated.");
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(107, stockClient.Key, $"Something wrong in saving results for chat {stockClient.Key}", ex);
                        this.telegramBot.SendMessage(stockClient.Key, Logger.GetLogErrorMessage(
                            "There is an error in saving your results into the google table." +
                            " Please, write to @lifni about this error.", ex));
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(106, 0, $"Something went wrong in general.", ex);
            }
        }
    }
}

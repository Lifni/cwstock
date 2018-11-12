using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly TimeSpan timeToWait = TimeSpan.FromMinutes(30);

        private List<Timer> timers;

        private Logger logger;
        private SecretProvider secretProvider;
        private FileConfigs fileConfigs;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");
            
            this.fileConfigs.TimesToRun.ForEach(x => this.SetUpTimer(x));
            while (true)
            {
                this.logger.Log(987, 0, "I'm alive!");
                Thread.Sleep(this.timeToWait);
            }
        }

        public override bool OnStart()
        {
            Thread.Sleep(Timeout.InfiniteTimeSpan);

            this.secretProvider = new SecretProvider();
            this.logger = new Logger(this.secretProvider.BlobStorageConnectionString, FileConfigs.BlobContainerName);
            this.fileConfigs = new FileConfigs(this.secretProvider.BlobStorageConnectionString);
            this.timers = new List<Timer>();

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");
            
            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }
        
        private void SetUpTimer(TimeSpan alertTime)
        {
            TimeSpan timeToGo = alertTime - DateTime.Now.ToUniversalTime().TimeOfDay;
            if (timeToGo < TimeSpan.Zero)
            {
                return;//time already passed
            }
            this.timers.Add(new Timer(x =>
            {
                this.RunProcessing();
            }, null, timeToGo, TimeSpan.FromDays(1)));
        }

        private void RunProcessing()
        {
            this.logger = new Logger(this.secretProvider.BlobStorageConnectionString, FileConfigs.BlobContainerName);
            Core core = new Core(
                this.logger,
                this.fileConfigs.GetOnboardedClients(),
                this.secretProvider.GoogleSheetsConfig,
                this.secretProvider.TelegramBotToken);
            core.Run(); 
        }
    }
}

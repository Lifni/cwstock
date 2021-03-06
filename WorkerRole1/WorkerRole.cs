using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly TimeSpan timeToWait = TimeSpan.FromMinutes(30);
        private readonly TimeSpan timeBetweenButtles = TimeSpan.FromHours(8);
        private Timer timer;

        private Logger logger;
        private SecretProvider secretProvider;
        private FileConfigs fileConfigs;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            while (true)
            {
                this.logger.Log(987, 0, "I'm alive!");
                Thread.Sleep(this.timeToWait);
            }
        }

        public override bool OnStart()
        {
            //Thread.Sleep(Timeout.InfiniteTimeSpan);

            this.secretProvider = new SecretProvider();
            this.logger = new Logger(this.secretProvider.BlobStorageConnectionString, FileConfigs.BlobContainerName);
            this.fileConfigs = new FileConfigs(this.secretProvider.BlobStorageConnectionString);
            this.SetUpTimer();
            //this.timer = new Timer(x => { this.RunProcessing(); }, null, TimeSpan.FromSeconds(3), TimeSpan.FromDays(1));

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
        
        private void SetUpTimer()
        {
            TimeSpan timeSpan = TimeSpan.MaxValue;
            DateTime timeNow = DateTime.UtcNow;
            if (timeNow.TimeOfDay < TimeSpan.FromHours(6))
            {
                timeSpan = TimeSpan.FromHours(6) - DateTime.Now.ToUniversalTime().TimeOfDay;
            }
            else if (timeNow.TimeOfDay < TimeSpan.FromHours(14))
            {
                timeSpan = TimeSpan.FromHours(14) - DateTime.Now.ToUniversalTime().TimeOfDay;
            }
            else if (timeNow.TimeOfDay < TimeSpan.FromHours(22))
            {
                timeSpan = TimeSpan.FromHours(22) - DateTime.Now.ToUniversalTime().TimeOfDay;
            }
            else
            {
                timeSpan = TimeSpan.FromHours(6) + TimeSpan.FromDays(1) - DateTime.Now.ToUniversalTime().TimeOfDay;
            }

            this.timer = new Timer(x => { this.RunProcessing(); }, null, timeSpan, TimeSpan.FromDays(1));
        }

        private void RunProcessing()
        {
            this.timer = new Timer(x => { this.RunProcessing(); }, null, this.timeBetweenButtles, TimeSpan.FromDays(1));
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

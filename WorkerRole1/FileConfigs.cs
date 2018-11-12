using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using WorkerRole1.Azure;

namespace WorkerRole1
{
    public class FileConfigs
    {
        public const string BlobContainerName = "stockbotfiles";

        private const string FolderName = "OnboardedCilents";
        private const string ClientNamesFileName = "ClientFiles.json";

        private readonly BlobContainerManager blobManager;

        public FileConfigs(SecureString blobConnectionString)
        {
            this.blobManager = new BlobContainerManager(blobConnectionString, BlobContainerName);
        }

        public List<TimeSpan> TimesToRun { get; protected set; }

        public List<OnboardClient> GetOnboardedClients()
        {
            List<string> configFiles = JsonConvert.DeserializeObject<List<string>>(
                this.blobManager.GetFileContent(ClientNamesFileName));
            List<OnboardClient>  onboardedClients = new List<OnboardClient>();
            ////onboardedClients.Add(JsonConvert.DeserializeObject<OnboardClient>(
            ////        this.blobManager.GetFileContent(FolderName, "TestToMe.json")));
            configFiles.ForEach(fileName =>
            {
                onboardedClients.Add(JsonConvert.DeserializeObject<OnboardClient>(
                    this.blobManager.GetFileContent(FolderName, fileName)));
            });
            return onboardedClients;
        }
    }
}

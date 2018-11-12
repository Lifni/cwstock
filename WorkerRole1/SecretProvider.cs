using System.Security;
using WorkerRole1.Azure;

namespace WorkerRole1
{
    public class SecretProvider
    {
        private const string GoogleSheetsConfigSecretName = "GoogleSheetsConfig";
        private const string TelegramTokenSecretName = "TelegramBotToken";
        private const string BlobStorageConnectionStringSecretName = "BlobStorageConnectionString";

        private const string AadClientId = "b3b01c3e-2446-4c88-8aeb-2874254bb222";
        private const string AadClientUrl = "https://stockBotWebAad";
        private const string CertificateThumbprint = "995EBED618908E1FF33B906BFC9F1C757EFDC6AB";

        private readonly KeyVaultManager keyvaultManager;

        public SecretProvider()
        {
            this.keyvaultManager = new KeyVaultManager(
                AadClientId,
                AadClientUrl,
                CertificateThumbprint);
            this.GoogleSheetsConfig = this.keyvaultManager.GetSecret(GoogleSheetsConfigSecretName);
            this.TelegramBotToken = this.keyvaultManager.GetSecret(TelegramTokenSecretName);
            this.BlobStorageConnectionString = this.keyvaultManager.GetSecret(BlobStorageConnectionStringSecretName);

        }

        public SecureString GoogleSheetsConfig { get; protected set; }

        public SecureString TelegramBotToken { get; protected set; }

        public SecureString BlobStorageConnectionString { get; protected set; }
    }
}

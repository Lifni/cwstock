using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security;

namespace HeroBot.Azure
{
    public class KeyVaultManager
    {
        private const string Authority = "https://login.microsoftonline.com/lifniklzgmail.onmicrosoft.com";
        private const string VaultBesaUrl = "https://stockbotsecrets.vault.azure.net/";

        private readonly string clientId;
        private readonly string clientUrl;
        private readonly X509Certificate2 certificate;
        private readonly KeyVaultClient keyVaultClient;

        public KeyVaultManager(string clientId, string clientUrl, string certThumbprint)
        {
            this.clientId = clientId;
            this.clientUrl = clientUrl;
            this.certificate = this.GetCertificate(certThumbprint);
            this.keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessToken));
        }

        public SecureString GetSecret(string secretName)
        {
            return this.keyVaultClient.GetSecretAsync(VaultBesaUrl, secretName)
                .Result.Value.ToSecureString();
        }

        private async Task<string> GetAccessToken(string authority, string resource, string scope)
        {
            AuthenticationContext context = new AuthenticationContext(authority);
            AuthenticationResult authResult = await context.AcquireTokenAsync(
                resource,
                new ClientAssertionCertificate(this.clientId, this.certificate));
            return authResult.AccessToken;
        }

        private X509Certificate2 GetCertificate(string thumbprint)
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var cert = store.Certificates.Find(
                                X509FindType.FindByThumbprint,
                                 thumbprint,
                                false);
            store.Close();
            return cert[0];
        }
    }
}

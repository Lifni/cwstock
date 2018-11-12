using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Security;

namespace WorkerRole1.Azure
{
    public class BlobContainerManager
    {
        private readonly CloudBlobContainer cloudBlobContainer;

        public BlobContainerManager(SecureString connectionString, string blobContainerName)
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(connectionString.ToUnsecureString());
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            this.cloudBlobContainer = cloudBlobClient.GetContainerReference(blobContainerName);
        }

        public string GetFileContent(string folderPath, string fileName)
        {
            return this.GetFileContent(Path.Combine(folderPath, fileName));
        }

        public string GetFileContent(string fileName)
        {
            return this.cloudBlobContainer.GetBlockBlobReference(fileName).DownloadText();
        }

        public void CreateAppendBlob(string filePath)
        {
            CloudAppendBlob blockBlob = this.cloudBlobContainer.GetAppendBlobReference(filePath);
            if (!blockBlob.Exists())
            {
                blockBlob.CreateOrReplace();
            }
        }

        public void Append(string folderPath, string fileName, string content)
        {
            this.Append(Path.Combine(folderPath, fileName), content);
        }

        public void Append(string fileName, string content)
        {
            CloudAppendBlob blockBlob = this.cloudBlobContainer.GetAppendBlobReference(fileName);
            blockBlob.AppendText(content);
        }
    }
}

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace WorkerRole1.GoogleSheets
{
    public class SheetClient
    {
        private readonly string Scope = SheetsService.Scope.Spreadsheets;
        private readonly string ReadonlyScope = SheetsService.Scope.SpreadsheetsReadonly;

        private readonly SheetsService service;
        private readonly string sheetId;
        private readonly string sheetPage;

        public SheetClient(string applicationName,
            SecureString jsonCredentials,
            string sheetId,
            string sheetPage,
            bool isReadonly = false)
        {
            this.service = this.CreateService(applicationName, jsonCredentials.ToUnsecureString(), isReadonly);
            this.sheetId = sheetId;
            this.sheetPage = sheetPage;
        }

        public object Read(Cell cell)
        {
            SpreadsheetsResource.ValuesResource.GetRequest request =
                       this.service.Spreadsheets.Values.Get(this.sheetId, $"{this.sheetPage}!{cell}");
            ValueRange response = request.Execute();
            IList<IList<object>> values = response.Values;
            if (values == null || !values.Any() || values[0] == null || !values[0].Any())
            {
                throw new SheetsClientException($"Unable to read cell {cell}.");
            }

            return response.Values[0][0];
        }

        public IList<IList<object>> Read(Cell beginCell, string endColumn)
        {
            SpreadsheetsResource.ValuesResource.GetRequest request =
                       this.service.Spreadsheets.Values.Get(
                           this.sheetId,
                           $"{this.sheetPage}!{beginCell}:{endColumn}");
            ValueRange response = request.Execute();
            if (response.Values == null || response.Values.Any(x => x == null))
            {
                throw new SheetsClientException($"Unable to read table {this.sheetPage} ({beginCell}:{endColumn}).");
            }

            return response.Values;
        }

        public void Write(Cell beginCell, IList<IList<object>> cells)
        {
            ValueRange valueRange = new ValueRange()
            {
                Values = cells
            };

            SpreadsheetsResource.ValuesResource.UpdateRequest request =
                        this.service.Spreadsheets.Values.Update(
                            valueRange,
                            this.sheetId,
                            $"{this.sheetPage}!{beginCell}");
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

            UpdateValuesResponse result = request.Execute();
        }

        private SheetsService CreateService(string applicationName, string jsonCredentials, bool isReadonly)
        {
            GoogleCredential googleCredential = GoogleCredential.FromJson(jsonCredentials)
                .CreateScoped(isReadonly ? ReadonlyScope : Scope);

            return new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = googleCredential,
                ApplicationName = applicationName,
            });
        }

    }
}

using System;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using RecursiveMapper;
using UnityEngine;

namespace Example
{
    public class ExampleComponent : MonoBehaviour
    {
        [SerializeField] ExampleTargetComponent target = null;
        [SerializeField] string testSpreadsheetID = null;
        [SerializeField] ExampleData data = new ExampleData ();
        [SerializeField] SuperclassData someSheetsData = new SuperclassData ();

        [SerializeField] TextAsset key = null;

        private MapperService service;

        private void Start()
        {
            var initializer = new BaseClientService.Initializer
                              {
                                  HttpClientInitializer = GoogleCredential.FromJson (key.text)
                                                                          .CreateScoped ("https://spreadsheets.google.com/feeds", "https://docs.google.com/feeds"),
                                  ApplicationName       = "SheetsSerialization",
                                  GZipEnabled           = Application.isEditor || Application.platform != RuntimePlatform.Android
                              };
            service = new MapperService (initializer);
        }

        public void WriteData() => Write(data);
        public void WriteSheets() => Write (someSheetsData);

        public void ReadData() => Read<ExampleData> (x => target.data = x);
        public void ReadSheets() => Read<SuperclassData> (x => target.someSheetsData = x);

        private async void Write<T>(T obj)
        {
            await service.WriteAsync (obj, testSpreadsheetID);
        }

        private async void Read<T>(Action<T> callback)
            where T : new()
        {
            var result = await service.ReadAsync<T> (testSpreadsheetID);
            callback.Invoke (result);
        }
    }
}
using System;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using RecursiveMapper;
using UnityEngine;

namespace Example
{
    public class ExampleComponent : MonoBehaviour
    {
        [SerializeField] ExampleTargetComponent target;
        [SerializeField] string testSpreadsheetID;
        [SerializeField] ExampleData data = new ExampleData ();
        [SerializeField] SuperclassData someSheetsData = new SuperclassData ();

        [SerializeField] TextAsset key;
        [SerializeField] private EasyData ezData; // todo - remove

        private MapperService service;

        private void Start()
        {
            var httpInit = GoogleCredential.FromJson (key.text).CreateScoped ("https://spreadsheets.google.com/feeds", "https://docs.google.com/feeds");
            service = new MapperService (new BaseClientService.Initializer
                                         {
                                             HttpClientInitializer = httpInit,
                                             ApplicationName       = "SheetsSerialization",
                                             GZipEnabled           = Application.isEditor || Application.platform != RuntimePlatform.Android
                                         },
                                         Debug.Log);
        }

        public void WriteData() => Write(ezData);
        public void WriteSheets() => Write (someSheetsData);

        public void ReadData() => Read<ExampleData> (x => target.data = x);
        public void ReadSheets() => Read<SuperclassData> (x => target.someSheetsData = x);

        private async void Write<T>(T obj)
        {
            try
            {
                var result = await service.WriteAsync (obj, testSpreadsheetID);
                print (result ? "Write task succeeded" : "Write task failed");
            }
            catch (Exception e)
            {
                print (e);
            }

        }

        private async void Read<T>(Action<T> callback)
            where T : new()
        {
            try
            {
                var result = await service.ReadAsync<T> (testSpreadsheetID);
                callback.Invoke (result);
            }
            catch (Exception e)
            {
                print (e);
            }
        }
    }
}
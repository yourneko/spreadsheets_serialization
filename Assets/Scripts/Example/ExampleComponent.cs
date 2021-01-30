using Mimimi.SpreadsheetsSerialization.Core;
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

        private SerializationService service;

        private void Start()
        {
            service = SerializationService.StartService (key.text);
        }

        public void WriteData() => Write (ref data);
        public void WriteSheets() => Write (ref someSheetsData);

        public void ReadData() => Read ((ExampleData x) => target.data = x, false);
        public void ReadSheets() => Read ((SuperclassData x) => target.someSheetsData = x, true);

        private void Write<T>(ref T obj)
        {
            if (ClassMapping.IsMappableType (typeof (T)))
            {
                CustomBatchUpdateRequest request = service.BatchUpdate (testSpreadsheetID);
                request.Parse (ref obj);
                request.Enqueue ();
            }
            else Debug.LogError ($"Writing cancelled. Type {typeof (T).Name} is not mappable.");
        }

        private void Read<T>(System.Action<T> _writeResults, bool indexed)
        {
            if (ClassMapping.IsMappableType (typeof (T)))
            {
                CustomBatchGetRequest rq = new CustomBatchGetRequest (testSpreadsheetID, indexed);
                rq.Add (_writeResults);
                rq.Enqueue ();
            }
            else Debug.LogError ($"Writing cancelled. Type {typeof (T).Name} is not mappable.");
        }
    }
}

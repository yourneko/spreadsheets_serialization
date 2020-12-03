using Mimimi.SpreadsheetsSerialization.Core;
using UnityEngine;

namespace Example
{
    public class ExampleComponent : MonoBehaviour
    {
        [SerializeField] ExampleTargetComponent target;
        [SerializeField] string testSpreadsheetID;
        [SerializeField] ExampleData data;
        [SerializeField] SuperclassData someSheetsData;

        [SerializeField] TextAsset key;

        private void Start()
        {
            SerializationService.StartService (key.text);
        }

        public void WriteData() => Write (data, false);
        public void WriteSheets() => Write (someSheetsData, true);

        public void ReadData() => Read ((ExampleData x) => target.data = x, false);
        public void ReadSheets() => Read ((SuperclassData x) => target.someSheetsData = x, true);

        private void Write<T>(T obj, bool _createMissingRanges)
        {
            if (ClassMapping.IsMappableType (typeof (T)))
            {
                CustomBatchUpdateRequest request = new CustomBatchUpdateRequest (testSpreadsheetID, _createMissingRanges);
                request.Add (obj);
                request.Enqueue (null);
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

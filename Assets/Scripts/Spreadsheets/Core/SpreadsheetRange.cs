using System.Collections.Generic;
using System.Linq;
using Google.Apis.Sheets.v4.Data;
using Mimimi.Tools.A1Notation;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public class SpreadsheetRange
    {
        private SpreadsheetRangePath path;
        private readonly List<List<string>> data;
        private string range;

        private SpreadsheetRange()
        {
            data = new List<List<string>> () { new List<string> () };
            path = new SpreadsheetRangePath ();
            // adding a place for final path
            path.WriteTitle ();
        }

#region Export

        public ValueRange GetValueRange()
        {
            UnityEngine.Debug.Log ($"Created range {range}");
            return new ValueRange ()
            {
                MajorDimension = "ROWS",
                Range = range,
                Values = data.Select (sub => sub.ToArray ()).ToArray () // it won't accept lists 
            };
        }

        /// <remarks> NOTE: Use only with Mapped containers of single sheet size or smaller. </remarks>
        public static SpreadsheetRange FromMap(FlexibleArray<Map> _source, A1Point _rangePivot, string _sheet)
        {
            var result = new SpreadsheetRange ();

            // Start recursive process of writing 
            result.SortArrays (_source);

            // Done. Pop out the last line and read it
            result.data[0][0] = result.path.GetPath ();

            // Assemble the range definition. Then, move it to given pivot and add a sheet name.
            result.range = $"'{_sheet}'!{result.path.GetRange().TranslateTo (_rangePivot)}";
            return result;
        }

#endregion
#region Write mapped content to data lists

        private void SortArrays<T>(FlexibleArray<T> _array)
        {
            if (_array.IsValue)
                ProcessValue (_array);
            else
                ProcessDimension (_array);
        }

        private void ProcessDimension<T>(FlexibleArray<T> _data)
        {
            path.OpenDimension (_data.dimensionInfo.direction);
            foreach (var entry in _data.Enumerate ())
                SortArrays (entry);
            path.CloseLastDimension ();
        }

        private void ProcessValue<T>(FlexibleArray<T> _data)  
        {
            switch (_data.FirstValue)
            {
                case MapRange r:
                    WriteHeaderIfNotEmpty (r);
                    SortArrays (r.ExpandRange ());
                    return;
                case MapValue v:
                    WriteHeaderIfNotEmpty (v);
                    SortArrays (v.GetStringValues ());
                    return;
                case string s: 
                    WriteValue (s);
                    path.WriteValue ();
                    return;
            }
        }

        private void WriteHeaderIfNotEmpty(Map _map)
        {
            if (!string.IsNullOrEmpty (_map.Header))
            {
                WriteValue (_map.Header);
                path.WriteTitle ();
            }
        } 

        private void WriteValue(string _value)
        {
            //UnityEngine.Debug.Log ($">> [{path.NextPoint.x},{path.NextPoint.y}] > {_value}");
            for (int i = data.Count; i <= path.NextPoint.y; i++)
                data.Add (new List<string> ());
            List<string> line = data[path.NextPoint.y];
            for (int i = line.Count; i <= path.NextPoint.x; i++)
                line.Add (null);
            line[path.NextPoint.x] = _value;
        }

#endregion
    }
}
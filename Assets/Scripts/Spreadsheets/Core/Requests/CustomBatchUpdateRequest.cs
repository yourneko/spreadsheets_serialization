using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public class CustomBatchUpdateRequest : CustomBatchRequest
    {
        private readonly List<string> sheetsRequired;
        private readonly bool createMissingSheets;
        private Action callback;

        public override List<ValueRange> ValueRanges { get; protected set; }

        /// <summary> Specify the spreadsheet to update. </summary>
        public CustomBatchUpdateRequest(string _spreadsheetID, bool _createMissingSheets = false)
        {
            SpreadsheetID = _spreadsheetID;
            ValueRanges = new List<ValueRange> ();
            sheetsRequired = new List<string> ();
            createMissingSheets = _createMissingSheets;
        }

#region Assembling

        /// <remarks> Please avoid passing in a collection as one piece. </remarks>
        public void Add<T>(T obj)
        {
            UnityEngine.Debug.Assert (ClassMapping.IsMappableType(typeof(T)), $"Type {typeof(T).Name} is not mappable. To write a collection type, pass objects in one by one.");
            SortByMapSize (ClassMapping.GetClassFields (typeof(T)).Bind (obj.ObjectToMap), typeof (T), ClassNaming.PARAMETER_PLACE);
        }

        protected void EnumerateDimension(FlexibleArray<Map> _array, Type _type, int _dimensionCount, string _parametrizedName)
        {
            if (_dimensionCount == 0)
                SortByMapSize (_array, _type, _parametrizedName);
            else
            {
                UnityEngine.Debug.Assert (_array.IsDimension);
                int index = 0;
                foreach (var element in _array.Enumerate ())
                {
                    index += 1;
                    EnumerateDimension (element, _type, _dimensionCount - 1, $"{_parametrizedName} {index}");
                }
            }
        }

        // this method separates SheetsRanges from Sheets. Do not pass smaller maps in it
        protected void SortByMapSize(FlexibleArray<Map> _array, Type _type, string _parametrizedName)
        {
            switch (ClassMapping.GetTypeSpaceRequirement (_type))
            {
                case SpaceRequired.Sheet:
                    CreateSheet (_array, _type, _parametrizedName);
                    return;
                case SpaceRequired.SheetsGroup:
                    ProcessSheetsRange (_array, _type, _parametrizedName);
                    return;
                default:
                    throw new InvalidOperationException ();
            }
        }

        // TypeSpace == Sheet in case of separate sheet
        // TypeSpace == SheetsGroup if array was assembled from smaller parts of SheetsGroup
        protected void CreateSheet(FlexibleArray<Map> _mapped, Type _type, string _parametrizedName) 
        {
            UnityEngine.Debug.Assert (ClassMapping.GetTypeSpaceRequirement (_type) >= SpaceRequired.Sheet);
            string name = ClassNaming.AssembleSheetName(_type, _parametrizedName);
            var sheetRange = SpreadsheetRange.FromMap (_source:     _mapped, 
                                                       _sheet:      name, 
                                                       _rangePivot: ClassMapping.GetPivotPoint(_type));
            UnityEngine.Debug.Assert (sheetRange != null);
            sheetsRequired.Add (name);
            ValueRanges.Add (sheetRange.GetValueRange ());
        }

        protected void ProcessSheetsRange(FlexibleArray<Map> _mapped, Type _type, string _parametrizedName)
        {
            UnityEngine.Debug.Assert (ClassMapping.GetTypeSpaceRequirement (_type) == SpaceRequired.SheetsGroup);

            // If T contains Ranges or SingleValues, group it into the separate sheet. 
            if (_mapped.GetValues ().Any (x => x.SpaceRequirement <= SpaceRequired.Range))
                CreateSheet (_mapped:           _mapped.Filter (x => x.SpaceRequirement <= SpaceRequired.Range),
                             _type:             _type,
                             _parametrizedName: ClassNaming.AssembleSheetName(_type, ClassNaming.AssembleGroupName(_type, _parametrizedName)));

            // Ignoring Maps of Ranges and SingleValues, which are batched before. So, every remaining Map has to be a MapRange. Expand all maps and sort them again.
            foreach (var map in _mapped.GetValues ().Where (x => x.SpaceRequirement >= SpaceRequired.Sheet))
            {
                if (map is MapRange mr)
                {
                    EnumerateDimension (_array: mr.ExpandRange (),
                                        _type: map.ContainedType,
                                        _dimensionCount: mr.Types.Length - 1,
                                        _parametrizedName: ClassNaming.AssembleGroupName(_type, _parametrizedName));
                }
                else throw new Exception ();
            }
        }

#endregion
#region Queue

        /// <summary> Pass null to get no callback. </summary>
        public void Enqueue(Action _callback)
        {
            callback = _callback;
            UnityEngine.Debug.Log ($"Update request queued. Sheets included: {ValueRanges.Count}");

            if (createMissingSheets)
                SpreadsheetRequest.Send (SpreadsheetID, MakeRequestAddMissingSheets);
            else
                Enqueue ();
        }

        private void MakeRequestAddMissingSheets(Spreadsheet _spreadsheet)
        {
            if (_spreadsheet is null)
            {
                UnityEngine.Debug.Log ($"Request cancelled. Can't get the list of sheets in the spreadsheet {SpreadsheetID}");
            }
            else
            {
                var currentSheets = _spreadsheet.Sheets.Select(x => x.Properties.Title);
                var missingSheets = sheetsRequired.Distinct ()
                                                  .Except (currentSheets)
                                                  .ToArray ();
                if (missingSheets.Any ())
                    AddSheetsRequest.Send (SpreadsheetID, OnSheetsAdd, missingSheets);
                else
                    Enqueue ();
            }
        }

        private void OnSheetsAdd(bool _success)
        {
            if (_success)
                Enqueue ();
            else
                UnityEngine.Debug.Log ($"Request cancelled. Can't add missing sheets to {SpreadsheetID}");
        }

        private void Enqueue() => SerializationService.Enqueue (this);

        public void SetResponse(int _cellsUpdated)
        {
            callback?.Invoke ();
            ValueRanges.Clear ();
            UnityEngine.Debug.Log ($"SpreadsheetUpdate request was completed. Cells updated: {_cellsUpdated}");
        }

#endregion
    }
}
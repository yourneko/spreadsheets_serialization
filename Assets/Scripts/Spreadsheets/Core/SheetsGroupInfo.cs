using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    /// <summary> Objects for storing the information about requested class, selecting corresponding sheets and assembling ValueRanges in correct order. </summary>
    public class SheetsGroupInfo : IGetRequestInfo
    {
        private const string PARAM = "{0}";
        private static string IndicesString(int _dimensions) => string.Join ("", Enumerable.Repeat (" #", _dimensions));

        public Type type;
        public object callbackObject;
        public string parametrizedName; // name can't be indiced. all indices should be replaced by numbers when SheetsGroupInfo is created
        public bool hasSmallerElements;
        public string baseSheetName;

        private readonly Dictionary<FieldInfo, FieldMappingDetails> names;
        private readonly FlexibleArray<FieldInfo> fields;

        public string Name => $"Requested type '{type.Name}'";

        private IEnumerable<FieldMappingDetails> RequiredFields => names.Select (x => x.Value).Where (x => x.dimensions == 0);
        private IEnumerable<FieldMappingDetails> IndexedFields => names.Select (x => x.Value).Where (x => x.dimensions > 0);
        private IEnumerable<FieldMappingDetails> GetRequired(SpaceRequired _space) => RequiredFields.Where (x => x.space == _space);

        public SheetsGroupInfo(Type _type, string _parametrizedName = PARAM)
        {
            UnityEngine.Debug.Assert (!_parametrizedName.Contains ('#'));

            fields = ClassMapping.GetClassFields (_type);
            names = new Dictionary<FieldInfo, FieldMappingDetails> ();
            type = _type;
            parametrizedName = string.Format (_parametrizedName, ClassMapping.GetParametrizedSheetName (type));
            hasSmallerElements = fields.GetValues ().Any (x => ClassMapping.GetTypeSpaceRequirement (x.GetFieldDimensionsTypes ().Last ()) < SpaceRequired.Sheet);
            if (hasSmallerElements)
                baseSheetName = string.Format (_parametrizedName, ClassMapping.GetSheetName (_type));

            foreach (var field in fields.GetValues ())
                AddFieldDetails (field);
        }

        private void AddFieldDetails(FieldInfo field)
        {
            Type fieldType = field.GetFieldDimensionsTypes ().Last ();
            int fieldDimensions = field.GetDimensions ().Length;
            SpaceRequired sr = ClassMapping.GetTypeSpaceRequirement (fieldType);

            switch (sr)
            {
                case SpaceRequired.SheetsGroup:
                case SpaceRequired.Sheet:
                    names.Add (field, new FieldMappingDetails (fieldType,
                                                                sr,
                                                                parametrizedName.Replace (PARAM, PARAM + IndicesString (fieldDimensions)),
                                                                fieldDimensions));
                    break;
                default: break;
            }
        }

        // Looking for required sheets only. All collections are considered empty.
        public IEnumerable<string> GetUnindexedSheetList()
        {
            List<string> rangeList = GetRequired (SpaceRequired.Sheet).Select (sheet => SerializationHelpers.Range (sheet.parametrizedName, sheet.type)).ToList ();
            RequiredFields.Where (x => x.space == SpaceRequired.SheetsGroup)
                            .SelectMany (x => GetUnindexedSheetList ())
                            .ToList ()
                            .ForEach (rangeList.Add);
            if (hasSmallerElements)
                rangeList.Add (SerializationHelpers.Range (baseSheetName, type));
            return rangeList;
        }

        public IEnumerable<string> GetSheetsList(string[] _sheets) => GetRequestedSheets (_sheets);

        public void SetRequestedValues(ValueRange[] _values)
        {
            ClassMapping.InvokeGetCallback (type,
                                            callbackObject,
                                            AssembleValues(_values));
        }

        private FlexibleArray<string> AssembleValues(ValueRange[] _values)
        {

            // the first goal is to place FieldMappingDetails in the same order as related Fields are placed in class map
            FlexibleArray<string>[] smallerElements = (!hasSmallerElements) ? 
                                                        null : 
                                                        SpreadsheetRangePath.ReadSheet (FindByName (_values, baseSheetName).Values)
                                                                            .Enumerate ().ToArray ();
            FlexibleArray<FieldMappingDetails?> detailsArray = fields.Bind (GetDetails);

            // Creating FlexibleArray<string> from FieldMappingDetails. 
            // If details are null, substituting smallerElements instead (one by one, keeping their order)
            int i = 0;
            FlexibleArray<string> toStringArray (FieldMappingDetails? x)
            {
                return x.HasValue ? 
                       ToStringFlexibleArray (x.Value, _values) :                              
                       SheetGroupsExtensions.GetNextElement (ref i, z => smallerElements[z]);
            }
            return FlexibleArray<string>.Simplify (detailsArray.Bind (toStringArray));
        }

        private FieldMappingDetails? GetDetails(FieldInfo _info)
        {
            if (names.ContainsKey (_info))
                return names[_info];
            else
                return null;
        }

        // Finds required ValueRanges in given _values array. Parses them with SpreadsheetRangePath.ReadRange 
        // it might give any number of FlexibleArray<string>, which have to be connected in the right order
        private FlexibleArray<string> ToStringFlexibleArray(FieldMappingDetails _details, ValueRange[] _values)
        {
            switch (_details.space)
            {
                case SpaceRequired.Sheet:
                    if (_details.dimensions > 0)
                    {
                        var pairs = _details.fittingSheets.Select (x => (GetSortingIndices (_details.parametrizedName, x, _details.dimensions), x)).ToArray ();
                        var sorted = SortByIndices (pairs, _details.dimensions);
                        return FlexibleArray<string>.Simplify (sorted.Bind (x => SpreadsheetRangePath.ReadSheet (FindByName (_values, x).Values)));
                    }
                    else
                    {
                        UnityEngine.Debug.Assert (_details.fittingSheets.Count == 1);
                        return SpreadsheetRangePath.ReadSheet (FindByName (_values, _details.fittingSheets[0]).Values);
                    }
                case SpaceRequired.SheetsGroup:
                    if (_details.dimensions > 0)
                    {
                        var groups = _details.fittingGroups.Select (x => (GetSortingIndices (_details.parametrizedName, x.parametrizedName, _details.dimensions), x)).ToArray ();
                        var sortedGroups = SortByIndices (groups, _details.dimensions);
                        return FlexibleArray<string>.Simplify (sortedGroups.Bind (x => x.AssembleValues (_values)));
                    }
                    else
                    {
                        UnityEngine.Debug.Assert (_details.fittingGroups.Count == 1);
                        return _details.fittingGroups[0].AssembleValues (_values);
                    }
                default:
                    throw new Exception ("something is wrong here");
            }
        }

        // strange voices in the dark told me to create this...
        private static FlexibleArray<T> SortByIndices<T>((int[] indices, T value)[] _pairs, int _dimensions)
        {
            // Current step and next step of calculations
            // T[] is an array of values with equal set of checked indices. At the end, each array has to contain a single value
            FlexibleArray<T[]> current = new FlexibleArray<T[]> (_pairs.Select (x => x.value).ToArray ());
            FlexibleArray<T[]> result = null;
            
            // count is a pointer for indices array, grows +1 after each iteration
            int count = 0;
            // For given T value, finds corresponding element in _pairs array. Then, returns  indices[count]  from that element
            Func<T,int> sortValue = (T value) => _pairs.First (x => x.value.Equals (value)).indices[count];

            // each step adds a dimension to array by sorting elements of the array by  sortValue
            for (int i = 1; i < _dimensions; i++)
            {
                result = current.Expand (x => sortValue.ExpandSort (x), new DimensionInfo (Tools.A1Notation.A1Direction.Row));
                count = i;
                current = result;
            }
            // because each array has single value in it, replace every array with first element of it
            return result.Bind(x => x.First());
        }

        // _tested is equal to _indexed, but with '#' chars replaced by _d number of ints
        private static int[] GetSortingIndices(string _indexed, string _tested, int _d)
        {
            // _tested is _indexed with '#' replaced by some int values. we are trying to get those values
            var tmp = _tested;
            // get parts of string that remain the same both for _tested and _indexed
            var parts = _indexed.Split ('#');
            UnityEngine.Debug.Assert (parts.Length == _d + 1);
            // remove all of shared parts by replacing them with a character #
            foreach (var p in parts)
                tmp = tmp.Trim('#').Replace (p, "#");
            // after splitting by # we get all unique parts of _tested as separate strings
            var result = tmp.Split ('#');
            UnityEngine.Debug.Assert (result.Length == _d);
            // those unique parts have to be int values
            return result.Select (x => int.Parse (x)).ToArray ();
        }

        private static ValueRange FindByName(ValueRange[] _values, string _sheetName)
        {
            return _values.First (x => x.MatchSheetName(_sheetName));
        }

        private IEnumerable<string> GetRequestedSheets(string[] _sheets)
        {
            if (!HasRequiredSheets (this, _sheets))
                yield break;
                    
            foreach (var i in IndexedFields)
                CheckIndexedMatches (i, _sheets);

            if (baseSheetName != null) yield return SerializationHelpers.Range (baseSheetName, type);

            foreach (var n in names)
            {
                if (n.Value.space == SpaceRequired.Sheet)
                    foreach (var s in n.Value.fittingSheets)
                        yield return SerializationHelpers.Range (s, n.Value.type);
                if (n.Value.space == SpaceRequired.SheetsGroup)
                    foreach (var g in n.Value.fittingGroups)
                        foreach (var entry in g.GetRequestedSheets(_sheets))
                            yield return entry;
            }
        }

#region List of sheets

        // NOT PURE
        // check if _sheets contain the name of every sheet required to build this SheetsGroup object
        // meanwhile makes a list of required sheets for each field
        internal static bool HasRequiredSheets(SheetsGroupInfo _group, string[] _sheets)
        {
            foreach (var r in _group.RequiredFields)
            {
                switch (r.space)
                {
                    case SpaceRequired.Sheet:
                        if (!_sheets.MatchesAny (r.parametrizedName))
                            return false;
                        else
                            r.fittingSheets.Add (r.parametrizedName);
                        break;
                    case SpaceRequired.SheetsGroup:
                        var group = new SheetsGroupInfo (r.type, _group.parametrizedName);
                        if (!HasRequiredSheets (group, _sheets))
                            return false;
                        else
                            r.fittingGroups.Add (group);
                        break;
                    default: 
                        break;
                }
            }
            return true;
        }

        private static void CheckIndexedMatches(FieldMappingDetails _details, string[] _sheets)
        {
            UnityEngine.Debug.Assert (_details.dimensions > 0);
            UnityEngine.Debug.Assert (_details.space == SpaceRequired.SheetsGroup);

            switch (_details.space)
            {
                case SpaceRequired.Sheet:
                    var results = MultiIndexNumerator<string>.Enumerate (_details.dimensions, 
                                                                         1,
                                                                         _details.parametrizedName.SubstituteIndices,
                                                                         _sheets.MatchesAny);
                    foreach (var r in results)
                        _details.fittingSheets.Add(r);
                    return;
                case SpaceRequired.SheetsGroup:
                    var groups = MultiIndexNumerator<SheetsGroupInfo>.Enumerate (_details.dimensions,
                                                                                1,
                                                                                (ii) => new SheetsGroupInfo (_details.type, _details.parametrizedName.SubstituteIndices (ii)),
                                                                                _sheets.ContainsRequiredSheets);
                    foreach (var g in groups)
                        _details.fittingGroups.Add (g);
                    return;
                default:
                    return;
            }
        }

#endregion

        internal struct FieldMappingDetails
        {
            public readonly Type type;
            public readonly SpaceRequired space;
            public readonly string parametrizedName;
            public readonly int dimensions;
            public readonly List<string> fittingSheets;
            public readonly List<SheetsGroupInfo> fittingGroups;

            public FieldMappingDetails(Type _type, SpaceRequired _space, string _name, int _d)
            {
                type = _type;
                space = _space;
                parametrizedName = _name;
                dimensions = _d;
                fittingSheets = _space == SpaceRequired.Sheet ? new List<string> () : null;
                fittingGroups = _space == SpaceRequired.SheetsGroup ?  new List<SheetsGroupInfo> () : null;
            }
        }
    }
}
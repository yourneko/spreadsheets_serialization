using System;
using System.Linq;
using System.Reflection;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    internal class GroupInfo
    {
        private readonly Type type;
        private readonly string parametrizedName;
        private readonly FlexibleArray<FieldAssembler> fieldDetails;

        private readonly FlexibleArray<IDataPlacementInfo> partitionContainer;
        private FlexibleArray<IDataPlacementInfo> cachedRanges;

        public GroupInfo(Type _type, string _parentGroupName)
        {
            type = _type;
            parametrizedName = _parentGroupName;
            var fields = ClassMapping.GetClassFields (type);
            fieldDetails = fields.Bind (CreateDetails);

            var smallerElementsSheet = new SheetInfo (_name: ClassNaming.AssembleSheetName (type, parametrizedName),
                                                    _pivot: ClassMapping.GetPivotPoint (type).A1,
                                                    _end: SpreadsheetsHelpers.DEFAULT_RANGE_END);
            partitionContainer = new FlexibleArray<IDataPlacementInfo> ( new SheetPartitionInfo (smallerElementsSheet, type));
        }

        public bool HasRequiredSheets(string[] _sheets)
        {
            return GetCachedRanges (_sheets).GetValues ().All (_sheets.AnyMatches);
        }

        public FlexibleArray<IDataPlacementInfo> GetCachedRanges(string[] _sheets)
        {
            if (cachedRanges is null)
                cachedRanges = FlexibleArray<IDataPlacementInfo>.Simplify (fieldDetails.Bind (x => GetFieldDataPlacement(x, _sheets)));
            return cachedRanges;
        }

        public FlexibleArray<string> GetStringArray(ValueRange[] _values)
        {
            return FlexibleArray<string>.Simplify (cachedRanges.Bind (x => x.SelectRead (_values)));
        }

        /// <remarks> May return null. Also, not pure. </remarks>
        private FieldAssembler CreateDetails(FieldInfo _info)
        {
            if (ClassMapping.GetTypeSpaceRequirement (_info.BaseFieldType ()) >= SpaceRequired.Sheet)
                return new FieldAssembler (_info, parametrizedName);
            return null;
        }

        private FlexibleArray<IDataPlacementInfo> GetFieldDataPlacement(FieldAssembler _fieldAssembler, string[] _sheets)
        {
            return _fieldAssembler?.GetSpreadsheetRanges(_sheets) ?? partitionContainer;
        }

        /*
         * 
         *  -   ()              >   Get a list of ranges that aren't a collection 
         *                      Cancel everything on fail
         *                      No depending on data
         *  -   list of sheets  >   Get a list of maximum possible number of constructable ranges
         *                      Has required and optional threads. Cancelling up to optional connection if any of required steps can't be completed. 
         *                      Evaluate possible number of elements in arrays.
         *  -   ValueRange[]    >   Transform a list of sheets to FlexArray<string>
         *                      Cancel everything on fail
         *                      No depending on data
         * 
         * 
         * SheetInfo : IDataPlacementInfo
         * SheetPartitionInfo : IDataPlacementInfo
         * 
         * 
         *          * GETTING LIST OF SHEETS *
         * 
         *   *1 ClassMapping    Type                            ->  FlexibleArray <FieldInfo>
         *      
         *   *1 GroupInfo       FlexibleArray <FieldInfo>       ->  SheetInfo + FlexibleArray <FieldAssembler>
         *      
         *    1 FieldAssembler  FieldInfo                       ->  Either <GroupInfo, SheetInfo>       /no sheets checking/
         *      
         *   *2 FieldAssembler  FieldInfo                       ->  FlexibleArray <Either <GroupInfo, SheetInfo>>   /FlexibleArrayBuilder skips null values/
         *                      string[]
         *      
         *    3 FieldAssembler  Either <GroupInfo, SheetInfo>   ->  IEnumerable <SheetInfo>
         *    
         *   *5 FieldAssembler                                  ->  bool        /validation check/
         *      
         *    1 SheetInfo                                       ->  string
         *      
         * 
         *          * ASSEMBLING STRING ARRAY *
         * 
         *   *1 ClassMapping    Type                            ->  FlexibleArray <FieldInfo>
         *      
         *   *1 GroupInfo       FlexibleArray <FieldInfo>       ->  SheetInfo + FlexibleArray <FieldAssembler>
         *      
         *    2 GroupInfo       SheetInfo                       ->  SheetPartitionInfo
         *      
         *    3 GroupInfo       SheetPartitionInfo              ->  FlexibleArray <IDataPlacementInfo>
         *                      FlexibleArray <IDataPlacementInfo>
         *      
         *   *2 FieldAssembler  FieldInfo                       ->  FlexibleArray <Either <GroupInfo, SheetInfo>>
         *                      string[]
         *      
         *    4 FieldAssembler  Either <GroupInfo, SheetInfo>   ->  FlexibleArray <IDataPlacementInfo>
         *    
         *   *5 FieldAssembler                                  ->  bool        /validation check/
         *      
         *    1 SheetPartitionInfo                              ->  IEnumerable <FlexibleArray <string>>
         *      
         *    2 SheetInfo       ValueRange                      ->  FlexibleArray <string>
         *    
         *    1 ValueRange                                      ->  string
         *      
         */

    }
}
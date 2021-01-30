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

            var smallerElementsSheet = new SheetInfo (type, parametrizedName);
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
    }
}
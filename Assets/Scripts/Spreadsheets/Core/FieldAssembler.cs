using System;
using System.Linq;
using System.Reflection;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    internal class FieldAssembler
    {
        public readonly Type titleType;
        public readonly Type[] types;
        public readonly string parametrizedName;
        public FlexibleArray<IDataPlacementInfo> sheets;

        private readonly SpaceRequired space;
        private bool rangesCreated;

        /// <summary> Constructor for root. </summary>
        public FieldAssembler(Type _type)
        {
            types = new[] { _type };
            titleType = types.Last ();
            space = ClassMapping.GetTypeSpaceRequirement (titleType);
            parametrizedName = ClassNaming.PARAMETER_PLACE;
        }

        /// <summary> Constructor for fields. </summary>
        public FieldAssembler(FieldInfo _info, string _parametrizedName)
        {
            types = _info.GetFieldDimensionsTypes ();
            titleType = types.Last ();
            space = ClassMapping.GetTypeSpaceRequirement (titleType);
            parametrizedName = _parametrizedName;
        }

        // insert parsed string arrays to existing flexible array 
        public FlexibleArray<string> GetStringArray(ValueRange[] _ranges)
        {
            return FlexibleArray<string>.Simplify (sheets.Bind (x => x.SelectRead (_ranges)));
        }

        private Either<GroupInfo, SheetInfo> CreateEither(params int[] _indices)
        {
            switch (space)
            {
                case SpaceRequired.Sheet:
                    return new Either<GroupInfo, SheetInfo> ( new SheetInfo (_name: ClassNaming.AssembleSheetName(titleType, parametrizedName, _indices), 
                                                                             _pivot: ClassMapping.GetPivotPoint (titleType).A1, 
                                                                             _end: SpreadsheetsHelpers.DEFAULT_RANGE_END));
                case SpaceRequired.SheetsGroup:
                    return new Either<GroupInfo, SheetInfo> (new GroupInfo (titleType, ClassNaming.AssembleGroupName(titleType, parametrizedName, _indices)));
                default: throw new Exception ($"type of {titleType.Name} > space {space} is too small");
            }
        }

        public FlexibleArray<IDataPlacementInfo> GetSpreadsheetRanges (string[] _names)
        {
            if (!rangesCreated)
            {
                var eithers = types.Length > 1 ?
                              FlexibleArray<Either<GroupInfo, SheetInfo>>.CreateIndexed(CreateEither, new int[0], types.Length - 1, 1)
                                                                         .TakeWhile(_names.MatchesInfoRanges) :
                              new FlexibleArray<Either<GroupInfo, SheetInfo>> (CreateEither ());
                sheets = FlexibleArray<IDataPlacementInfo>.Simplify (eithers.Bind (_names.ToSheetsArray));
                rangesCreated = true;
            }
            return sheets;
        }
    }
}
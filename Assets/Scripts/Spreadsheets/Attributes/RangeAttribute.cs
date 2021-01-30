﻿namespace Mimimi.SpreadsheetsSerialization
{
    public class RangeAttribute : MapSpaceAttribute
    {
        public override SpaceRequired RequiredSpace => SpaceRequired.Range;

        public string DefaultSheet = string.Empty;
    }
}

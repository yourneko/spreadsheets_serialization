namespace Mimimi.SpreadsheetsSerialization
{
    public class SheetsGroupAttribute : MapSpaceAttribute
    {
        public override SpaceRequired RequiredSpace => SpaceRequired.SheetsGroup;

        /// <summary> 
        /// Parametrized name may contain only letters, digits, empty space and underscore. 
        /// It has to contain exactly one '{0}', to be replaced by a sheet name.
        /// Please, avoid placing digits right after '{0}', it might cause false negative errors.
        /// </summary>
        public string ParametrizedName = "{0}";
        public string DefaultSheetName { get; private set; }

        /// <summary>
        /// Mapped fields of size lesser than a Sheet contained by a SheetsGroup are composed to a new Sheet.
        /// </summary>
        // TODO: Add new attribute for fields. It should move a field to other sheet instead of default one.
        public SheetsGroupAttribute(string _defaultSheet) 
        {
            DefaultSheetName = _defaultSheet;
        }
    }
}

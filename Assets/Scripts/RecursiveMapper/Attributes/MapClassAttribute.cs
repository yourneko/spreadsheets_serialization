using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    /// <summary>Contains metadata of classes.</summary>
    [AttributeUsage (AttributeTargets.Class)]
    public sealed class MapClassAttribute : Attribute
    {
        public readonly string SheetName;

        internal bool Initialized { get; private set; }
        internal IReadOnlyList<MapFieldAttribute> CompactFields { get; private set; }
        internal IReadOnlyList<MapFieldAttribute> SheetsFields { get; private set; }
        internal V2Int Size { get; private set; }

        /// <summary>Map this class to Google Spreadsheets.</summary>
        /// <param name="sheetName">Types with a sheet name always occupy the whole sheet.</param>
        /// <remarks>Avoid loops in hierarchy of types. It will cause the stack overflow.</remarks>
        public MapClassAttribute(string sheetName = null)
        {
            SheetName = sheetName;
        }

        internal void CacheMeta(Type type)
        {
            Initialized = true;
            var allFields = type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                .Select (field => field.MapAttribute ())
                                .Where (x => x != null)
                                .ToArray ();
            SheetsFields = string.IsNullOrEmpty (SheetName)
                               ? throw new Exception ("Compact classes can't contain fields of Sheet content type.")
                               : allFields.Where (x => x.FrontType != null && !string.IsNullOrEmpty(x.FrontType.SheetName)).ToArray ();
            CompactFields = allFields.Where (x => x.FrontType is null || string.IsNullOrEmpty(x.FrontType.SheetName))
                                     .OrderBy (x => x.SortOrder)
                                     .ToArray ();
            Size = CompactFields.Aggregate(new V2Int(0, 0), (s, f) => s.Max(f.GetSize(s).BottomRight));
        }
    }
}

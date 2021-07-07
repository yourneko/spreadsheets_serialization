using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    /// <summary>
    /// Marks classes intended to be serialized.
    /// </summary>
    [AttributeUsage (AttributeTargets.Class)]
    public class MapClassAttribute : Attribute
    {
        public readonly string SheetName;
        string[] requiredSheets;

        internal bool Initialized { get; private set; }
        internal Type Type { get; private set; }
        internal IReadOnlyList<MapFieldAttribute> CompactFields { get; private set; }
        internal IReadOnlyList<MapFieldAttribute> SheetsFields { get; private set; }
        internal V2Int Size { get; private set; }

        internal IReadOnlyList<string> RequiredSheets => requiredSheets ??= SheetsFields.Where (x => x.HasFixedSize)
                                                                                        .SelectMany (x => x.FrontType.RequiredSheets)
                                                                                        .Select (SheetName.JoinSheetNames).ToArray ();

        /// <summary>
        /// Marks classes intended to be serialized.
        /// </summary>
        /// <param name="sheetName">Types with a sheet name always occupy the whole sheet.</param>
        /// <remarks>Avoid loops in hierarchy of types. It will cause the stack overflow.</remarks>
        public MapClassAttribute(string sheetName = null)
        {
            SheetName = sheetName;
        }

        internal void CacheMeta(Type type)
        {
            Type        = type;
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
            Size = CompactFields.Aggregate(V2Int.Zero, (s, f) => s.JoinRight(f.GetSize(s).Size));
        }
    }
}
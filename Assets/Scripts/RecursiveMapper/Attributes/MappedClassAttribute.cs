using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    [AttributeUsage (AttributeTargets.Class)]
    public class MappedClassAttribute : Attribute
    {
        public readonly string SheetName;
        internal readonly Type Type;
        string[] requiredSheets;

        internal bool Initialized { get; private set; }
        internal IReadOnlyList<MappedAttribute> CompactFields { get; private set; }
        internal IReadOnlyList<MappedAttribute> SheetsFields { get; private set; }
        internal IReadOnlyList<string> RequiredSheets => requiredSheets ??= SheetsFields.Where (x => x.Rank == 0)
                                                                                        .SelectMany (x => x.FrontType.RequiredSheets)
                                                                                        .Select (SheetName.JoinSheetNames).ToArray ();

        public MappedClassAttribute(string sheetName = null)
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
                               : allFields.Where (x => x.Content == ContentType.Sheet).ToArray ();
            CompactFields = allFields.Where (x => x.Content != ContentType.Sheet)
                                     .OrderBy (x => x.Position)
                                     .ToArray ();
        }
    }
}
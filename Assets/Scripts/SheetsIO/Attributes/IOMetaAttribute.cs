using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SheetsIO
{
    /// <summary>Contains metadata of classes.</summary>
    [AttributeUsage (AttributeTargets.Class)]
    public sealed class IOMetaAttribute : Attribute
    {
        internal readonly string SheetName;

        internal bool Initialized { get; private set; }
        internal Type Type { get; private set; }
        internal V2Int Size { get; private set; }
        internal IReadOnlyList<IOFieldAttribute> CompactFields { get; private set; }
        internal IReadOnlyList<IOFieldAttribute> SheetsFields { get; private set; }
        internal bool Optional { get; private set; }

        /// <summary>Map this class to Google Spreadsheets.</summary>
        /// <param name="sheetName">Types with a sheet name always occupy the whole sheet.</param>
        /// <remarks>Avoid loops in hierarchy of types. It will cause the stack overflow.</remarks>
        public IOMetaAttribute(string sheetName = null)
        {
            SheetName = sheetName;
        }

        internal void CacheMeta(Type type)
        {
            Initialized = true;
            Type        = type;
            var allFields = type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                .Select (field => field.GetIOAttribute ())
                                .Where (x => x != null)
                                .ToArray ();
            if (allFields.Length == 0) 
                throw new Exception($"Class {type.Name} has no MapFields! Add MapField attribute to some fields in class {type.Name}, or remove the MapClass attribute.");
            Optional = allFields.All(x => x.IsOptional);
            SheetsFields     = allFields.Where (x => !string.IsNullOrEmpty(x.FrontType?.SheetName ?? string.Empty)).ToArray ();
            CompactFields = allFields.Where (x => string.IsNullOrEmpty(x.FrontType?.SheetName ?? string.Empty))
                                     .OrderBy (f => f.Field.GetCustomAttribute<IOPlacementAttribute>()?.SortOrder
                                                 ?? (f.Rank == 0 || f.Rank == f.CollectionSize.Count ? 1000 : int.MaxValue + f.Rank - 2)) 
                                     .ToArray ();
            Size = V2Int.Zero;
            foreach (var f in CompactFields)
            {
                f.PosInType = new V2Int(Size.X, 0);
                Size        = new V2Int(Size.X + f.TypeSizes[0].X, Math.Max(Size.Y, f.TypeSizes[0].Y));
            }
        }
    }
}

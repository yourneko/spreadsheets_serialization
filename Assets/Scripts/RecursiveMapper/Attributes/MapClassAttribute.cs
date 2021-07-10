using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SpreadsheetsMapper
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
            SheetsFields = allFields.Where (x => !string.IsNullOrEmpty(x.FrontType?.SheetName ?? string.Empty)).ToArray ();
            CompactFields = allFields.Where (x => string.IsNullOrEmpty(x.FrontType?.SheetName ?? string.Empty))
                                     .OrderBy (f => f.Field.GetCustomAttribute<MapPlacementAttribute>()?.SortOrder
                                                 ?? (f.Rank == 0 || f.Rank == f.CollectionSize.Count ? 1000 : int.MaxValue + f.Rank - 2)) 
                                     .ToArray ();
            Size = new V2Int(CompactFields.Sum(x => x.TypeSizes[0].X), CompactFields.Max(x => x.TypeSizes[0].Y));
            MonoBehaviour.print($"Class {type.Name}: size {Size}");
        }

        internal V2Int GetFieldPos(MapFieldAttribute field) => new V2Int(CompactFields.TakeWhile(x => !Equals(x, field)).Sum(x => x.TypeSizes[0].X), 0);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    [AttributeUsage (AttributeTargets.Field)]
    public class MappedAttribute : Attribute
    {
        public readonly int Position; // todo - make a separate Attribute with validation
        public readonly int Rank;

        internal bool Initialized { get; private set; }
        internal FieldInfo Field { get; private set; }
        internal ContentType Content { get; private set; }
        internal IReadOnlyList<Type> ArrayTypes { get; private set; } // probably it gonna go
        internal MappedClassAttribute FrontType { get; private set; }

        public MappedAttribute(int position, int dimensions = 0)
        {
            Rank = dimensions;
            Position       = position;
        }

        internal void CacheMeta(FieldInfo field)
        {
            Initialized = true;
            Field       = field;
            var types = new List<Type> {field.FieldType};
            for (int i = 0; i < Rank; i++)
                types.Add (types[i].GetTypeInfo ().GetInterfaces ()
                            .FirstOrDefault (x => x.IsGenericType && x.GetGenericTypeDefinition () == typeof(IEnumerable<>))
                           ?.GetGenericArguments ()[0]);
            ArrayTypes = types;
            FrontType  = ArrayTypes[Rank].MapAttribute ();
            Content = FrontType is null
                          ? ContentType.Value
                          : string.IsNullOrEmpty (FrontType.SheetName)
                              ? ContentType.Object
                              : ContentType.Sheet;
        }
    }
}
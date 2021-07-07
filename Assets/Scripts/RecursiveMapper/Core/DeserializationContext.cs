using System;
using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper
{
    class DeserializationContext
    {
        public List<RequestedObject> Requests;
        public Dictionary<string, IList<IList<object>>> Values;
        public IValueSerializer Serializer;

        public object MakeSheets(MapClassAttribute ta, string name)
        {
            var result = Activator.CreateInstance (ta.Type);           // if result object is received, i can remove ta.Type property
            foreach (var field in ta.SheetsFields)
                field.Field.SetValue (result, field.Rank == 0
                                                  ? CreateSheetObject (field, name)
                                                  : AssembleRequestedObject (field, name));
            return result;
        }

        object AssembleRequestedObject(MapFieldAttribute field, string parentName)
        {
            var name = parentName.JoinSheetNames (field.FrontType.SheetName);
            var result = Activator.CreateInstance (field.ArrayTypes[0]);
            var request = Requests.FirstOrDefault (rq => StringComparer.Ordinal.Equals (rq.OwnName, name)) ?? throw new Exception ();
            Unwrap (result, request.MatchingSheets, field, 1);
            return result;
        }

        void Unwrap(object parent, object namesObj, MapFieldAttribute field, int rank)          // todo - reuse duplicating code
        {
            if (namesObj is List<object> namesList)
                foreach (var names in namesList)
                    Unwrap (field.AddChild (parent, rank), names, field, rank + 1);
            else
                field.AddChild (parent, rank, MakeSheets (field.FrontType, (string)namesObj));
        }

        object CreateSheetObject(MapFieldAttribute field, string name)
        {
            var type = field.FrontType;
            var obj = MakeSheets (type, name.JoinSheetNames (type.SheetName));
            if (type.CompactFields.Any ())
                ApplyValue (obj, Values[name], field);
            return obj;
        }

        void ApplyValue(object target, IList<IList<object>> values, MapFieldAttribute field)    // todo - make a non-void return type for ?:
        {
            if (field.FrontType is null) // values should have 1 element there
                field.AddChild (target, field.Rank, Serializer.Deserialize (field.ArrayTypes.Last (), (string)values[0][0]));
            else
                foreach (var f in field.FrontType.CompactFields)
                    Unwrap (f.AddChild (target, 0), values.Take (f.Borders.Size.X), f, 1);
        }

        void Unwrap(object target, IEnumerable<IList<object>> values, MapFieldAttribute field, int rank)
        {
            if (rank == field.Rank)
                ApplyValue (target, values.ToList (), field);
            else
                foreach (var part in Split (values, field, rank))
                    Unwrap (field.AddChild (target, rank), part, field, rank + 1);
        }

        IEnumerable<IEnumerable<IList<object>>> Split(IEnumerable<IList<object>> values, MapFieldAttribute field, int rank) // rank 1 is first, vertical
        {
            var size = field.FrontType.Size;    // todo - remove placeholder solution -> this
            return (rank & 1) > 0                           // todo - maybe with sizes of array known, it needs changes
                       ? values.Select (x => (IEnumerable<IList<object>>)x.ToChunks (size.Y))
                       : values.ToChunks (size.X);
        }
    }
}
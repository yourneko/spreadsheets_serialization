using System;

namespace RecursiveMapper
{
    class ReadContext
    {
        public MappedAttribute Field;
        public int Depth, Index;
        public object TargetObject;
        public V2Int First, Last = new V2Int(-1, -1);
        public ReadContext Back;
        public bool Horizontal, IsObject;

        public readonly Func<int, MappedAttribute> getNextField;
        private readonly Func<int, Type> getNextChildType;
        public readonly Action<object> addToObj, addToArray;

        public ReadContext(Func<int, MappedAttribute> getField = null, Func<int, Type> getChild = null)
        {
            getNextField     = getField ?? (i => Field.FrontType.CompactFields[i]);
            getNextChildType = getChild ?? (_ => Field.ArrayTypes[Depth]);
            addToObj         = child => getNextField (Index++).Field.SetValue (TargetObject, child);
            addToArray       = TargetObject.AddContent (getNextChildType(Index++));
        }

        public object NextObject()
        {
            var result = Activator.CreateInstance (getNextChildType (Index));
            (IsObject ? addToObj : addToArray).Invoke (result);
            return result;
        }
    }
}
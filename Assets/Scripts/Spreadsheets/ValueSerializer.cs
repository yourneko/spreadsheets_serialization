using System;
using Mimimi.SpreadsheetsSerialization.Core;
using UnityEngine;

namespace Mimimi.SpreadsheetsSerialization
{
    public static class ValueSerializer
    {
        const string NOT_IMPLEMENTED = "Values of Type '{0}' not supported";
        const string TRUE_LITERAL = "TRUE";
        const string FALSE_LITERAL = "FALSE";

        private static bool MatchIgnoreCase(string a, string b) => StringComparer.OrdinalIgnoreCase.Equals (a, b);

        private static string SerializeGeneric(object _target, Type _type)
        {
            switch (_type.GetGenericArguments ().Length)
            {
                case 1:
                    // Nullable
                    // Lazy
                    throw new NotImplementedException ();
                case 2:
                    // KeyValuePair     return $"{AsString (_pair.Key)}:{AsString (_pair.Value)}";
                    throw new NotImplementedException ();
                default:
                    throw new NotImplementedException ();
            }
        }

        public static string AsString<T>(this T _value)
        {
            Debug.Assert (!ClassMapping.IsMappableType (typeof (T)), $"Type '{typeof (T).Name}' is not a SingleValue type!");

            if (typeof (T).IsGenericType)
                return SerializeGeneric (_value, typeof (T));

            switch (_value)
            {
                case int i:
                    return i.ToString ();
                case string s:
                    return s;
                case bool b:
                    return b ? TRUE_LITERAL : FALSE_LITERAL;
                default:
                    return string.Format (NOT_IMPLEMENTED, typeof (T).Name);
            }
        }

        public static object FromString(string _value, Type _type)
        {
            Debug.Assert (!ClassMapping.IsMappableType (_type));

            // Switch requires constant values
            // It's possible to compare type names (strings), but i don't like this idea
            if (_type.Equals (typeof (int)))
                return int.Parse (_value);
            else if (_type.Equals (typeof (string)))
                return _value;
            else if (_type.Equals (typeof (bool)))
                return MatchIgnoreCase (_value, FALSE_LITERAL) ? false :
                       MatchIgnoreCase (_value, TRUE_LITERAL) ? (object)true :
                       throw new NotImplementedException (string.Format (NOT_IMPLEMENTED, _type.Name));
            else
                throw new NotImplementedException (string.Format (NOT_IMPLEMENTED, _type.Name));
        }

    }

}
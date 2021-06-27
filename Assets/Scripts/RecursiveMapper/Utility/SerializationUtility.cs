using System;
using System.Collections.Generic;
using System.Globalization;

namespace RecursiveMapper
{
    // To strings and back!
    static class SerializationUtility
    {
        private static readonly Dictionary<Type, Func<string, object>> @switch =
            new Dictionary<Type, Func<string, object>>
            {
                {typeof(string), s => s},
                {typeof(int), value => int.TryParse (value, out int i) ? i : 0},
                {typeof(float), value => float.TryParse (value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 0f},
                {typeof(double), value => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0},
                {typeof(bool), value => StringComparer.OrdinalIgnoreCase.Equals(value, "true")},
                {typeof(DateTime), s => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var a) ? a : DateTime.MinValue},

            };

        public static string SerializeValue<T>(T target) => target switch
                                                            {
                                                                int i      => i.ToString (),
                                                                bool b     => b ? "true" : "false",
                                                                string s   => s,
                                                                float f    => f.ToString (CultureInfo.InvariantCulture),
                                                                DateTime a => a.ToString (CultureInfo.InvariantCulture),
                                                                null       => string.Empty,
                                                                _          => target.ToString (),
                                                            };

        public static object DeserializeValue(this Type type, string value) => @switch.TryGetValue (type, out var func)
                                                                                   ? func.Invoke (value)
                                                                                   : throw new NotImplementedException ();
    }
}
using System;

namespace RecursiveMapper
{
    // To strings and back!
    static class SerializationUtility
    {
        public static string SerializeValue<T>(T target) => target switch
                                                            {
                                                                int i    => i.ToString (),
                                                                bool b   => b ? "true" : "false",
                                                                string s => s,
                                                                float f  => f.ToString ("0.000"),
                                                                null     => string.Empty,
                                                                _        => target.ToString (),
                                                            };

        public static object DeserializeValue(this Type type, string serialized) => type switch
                                                                                    {
                                                                                        _ => throw new NotImplementedException(),
                                                                                    };
    }
}
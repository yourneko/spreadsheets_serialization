using System;
using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper.Utility
{
    static class Helpers
    {
        // Sort of hardcoded stuff
        public static IEnumerable<T> SpawnWhile<T>(Predicate<T> condition, Func<int, T> produce)
        {
            int index = -1;
            T result;
            while (condition.Invoke (result = produce (++index)))
                yield return result;
        }

        public static bool SpawnCondition(this string[] sheets, RecursiveMap<bool> map) => map.IsLeft && sheets.Contains (map.Meta.Sheet)
                                                                                        || map.IsRight && map.Right.Any ();

        public static Meta CreateChildMeta(this Meta meta, Type[] types)
        {
            var mapRegionAttribute = types.Last().GetMappedAttribute ();
            return new Meta (meta.GetChildName(mapRegionAttribute?.SheetName), types);
        }

        public static string SerializeValue<T>(T target) => target switch
                                                     {
                                                         int i    => i.ToString (),
                                                         bool b   => b ? "true" : "false",
                                                         string s => s,
                                                         float f  => f.ToString ("0.000"),
                                                         null     => string.Empty,
                                                         _        => target.ToString (),
                                                     };

        public static RecursiveMap<string> JoinRecursive<T>(this IReadOnlyDictionary<string, IList<RecursiveMap<string>>> values,
                                                            IEnumerable<RecursiveMap<T>> right, Meta meta)
        {
            var rightArray = right.ToArray ();
            RecursiveMap<string>[] result = new RecursiveMap<string>[rightArray.Length];
            var dictValues = rightArray.Any (map => map.IsLeft)
                                 ? values[meta.FullName].ToArray ()
                                 : null;
            int dictValuesIndex = -1;

            for (int i = 0; i < rightArray.Length; i++)
                result[i] = rightArray[i].IsLeft
                                ? dictValues?[++dictValuesIndex]
                                : values.JoinRecursive (rightArray[i].Right, rightArray[i].Meta);

            return new RecursiveMap<string> (result, meta);
        }
    }
}
using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class SaveValueConversionRules
    {
        public static Godot.Collections.Dictionary<string, Variant> ToVariantDictionary(IReadOnlyDictionary<string, object> source)
        {
            var result = new Godot.Collections.Dictionary<string, Variant>();
            foreach (KeyValuePair<string, object> kv in source)
            {
                result[kv.Key] = ToVariantValue(kv.Value);
            }

            return result;
        }

        public static Dictionary<string, object> ToPlainDictionary(Godot.Collections.Dictionary<string, Variant> source)
        {
            var result = new Dictionary<string, object>();
            foreach (string key in source.Keys)
            {
                result[key] = ToPlainValue(source[key]);
            }

            return result;
        }

        public static object ToPlainValue(Variant value)
        {
            return value.VariantType switch
            {
                Variant.Type.Nil => null!,
                Variant.Type.Bool => value.AsBool(),
                Variant.Type.Int => value.AsInt64(),
                Variant.Type.Float => value.AsDouble(),
                Variant.Type.String => value.AsString(),
                Variant.Type.StringName => value.AsString(),
                Variant.Type.Dictionary => ToPlainDictionary((Godot.Collections.Dictionary<string, Variant>)value),
                Variant.Type.Array => ToPlainList((Godot.Collections.Array<Variant>)value),
                _ => value.ToString(),
            };
        }

        public static int GetInt(IDictionary<string, object> source, string key, int fallback = 0)
        {
            return source.TryGetValue(key, out object value) ? System.Convert.ToInt32(value) : fallback;
        }

        public static long GetLong(IDictionary<string, object> source, string key, long fallback = 0L)
        {
            return source.TryGetValue(key, out object value) ? System.Convert.ToInt64(value) : fallback;
        }

        public static double GetDouble(IDictionary<string, object> source, string key, double fallback = 0.0)
        {
            return source.TryGetValue(key, out object value) ? System.Convert.ToDouble(value) : fallback;
        }

        public static bool GetBool(IDictionary<string, object> source, string key, bool fallback = false)
        {
            return source.TryGetValue(key, out object value) ? System.Convert.ToBoolean(value) : fallback;
        }

        public static string GetString(IDictionary<string, object> source, string key, string fallback = "")
        {
            return source.TryGetValue(key, out object value) ? System.Convert.ToString(value) ?? fallback : fallback;
        }

        public static List<object> GetList(IDictionary<string, object> source, string key)
        {
            return source.TryGetValue(key, out object value) && value is IEnumerable<object> items
                ? new List<object>(items)
                : new List<object>();
        }

        private static Variant ToVariantValue(object value)
        {
            if (value is null)
            {
                return default;
            }

            return value switch
            {
                bool b => b,
                int i => i,
                long l => l,
                float f => f,
                double d => d,
                string s => s,
                IReadOnlyDictionary<string, object> dict => ToVariantDictionary(dict),
                IDictionary<string, object> dict => ToVariantDictionary(new Dictionary<string, object>(dict)),
                IEnumerable<object> list => ToVariantArray(list),
                _ => value.ToString() ?? string.Empty,
            };
        }

        private static Godot.Collections.Array<Variant> ToVariantArray(IEnumerable<object> source)
        {
            var result = new Godot.Collections.Array<Variant>();
            foreach (object item in source)
            {
                result.Add(ToVariantValue(item));
            }

            return result;
        }

        private static List<object> ToPlainList(Godot.Collections.Array<Variant> source)
        {
            var result = new List<object>(source.Count);
            foreach (Variant item in source)
            {
                result.Add(ToPlainValue(item));
            }

            return result;
        }
    }
}

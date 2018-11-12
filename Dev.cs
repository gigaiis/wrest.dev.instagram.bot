using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace main
{
    public static class Dev
    {
        public static Int32 GetUnixTimestamp(DateTime? time = null) => (Int32)((time != null) ? (time.Value) : DateTime.UtcNow).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        public static DateTime TimestampToDateTime(Int32 timestamp) => (new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp));
        public static async Task<string> ReadAsync(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Read))
            {
                byte[] bytes = new byte[fs.Length];
                int numBytesToRead = (int)fs.Length;
                int numBytesRead = 0;
                while (numBytesToRead > 0)
                {
                    int n = await fs.ReadAsync(bytes, numBytesRead, numBytesToRead);
                    if (n == 0) break;
                    numBytesRead += n;
                    numBytesToRead -= n;
                }
                numBytesToRead = bytes.Length;
                fs.Close();
                return Encoding.UTF8.GetString(bytes);
            }
        }
        public static async Task WriteAsync(string filename, string text)
        {
            File.WriteAllText(filename, string.Empty);
            using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                await fs.WriteAsync(bytes, 0, bytes.Length);
                fs.Close();
                return;
            }
        }

        public static string GetTranslitText(string text)
        {
            var rules = new Dictionary<char, string>()
            {
                {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"},
                {'е', "ye"}, {'ё', "yo"}, {'ж', "zh"}, {'з', "z"}, {'и', "i"},
                {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"},
                {'н', "n"}, {'о', "o"}, {'п', "p"}, {'р', "r"},
                {'с', "s"}, {'т', "t"}, {'у', "u"}, {'ф', "f"},
                {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"}, {'ш', "sh"},
                {'щ', "shch"}, {'ъ', "\""}, {'ы', "y"}, {'ь', "'"},
                {'э', "e"}, {'ю', "yu"}, {'я', "ya" }
            };
            var result = "";
            for (var i = 0; i < text.Length; i++)
                if (rules.ContainsKey(text[i])) result += rules[text[i]];
                else
                {
                    var c = char.ToLower(text[i]);
                    if (rules.ContainsKey(c)) result += char.ToUpper(rules[c][0]) + rules[c].Substring(1);
                    else result += text[i];
                }
            return result;
        }
    }
}

namespace System.Collections.Generic
{
    public static class DictionaryExtensionsClass
    {
        public static Dictionary<T, U> GetRange<T, U>(this Dictionary<T, U> d, int index) => GetRange(d, index, d.Count);
        public static Dictionary<T, U> GetRange<T, U>(this Dictionary<T, U> d, int index, int count)
        {
            var res = new Dictionary<T, U>();
            var last_index = (index + count - 1 >= d.Count) ? d.Count - 1 : index + count - 1;
            for (int i = index; i <= last_index; i++)
            {
                var e = d.ElementAt(i);
                res.Add(e.Key, e.Value);
            }
            return res;
        }
        public static Dictionary<T, U> AddRange<T, U>(this Dictionary<T, U> d, Dictionary<T, U> v)
        {
            foreach (var e in v)
                d.Insert(e);
            return d;
        }
        public static int IndexOf<T, U>(this Dictionary<T, U> d, T k)
        {
            for (int i = 0; i < d.Count; i++)
                if (d.ElementAt(i).Key.Equals(k)) return i;
            return -1;
        }
        public static bool Insert<T, U>(this Dictionary<T, U> d, KeyValuePair<T, U> v)
        {
            if (d.ContainsKey(v.Key)) return false;
            d.Add(v.Key, v.Value);
            return true;
        }
        public static bool Insert<T, U>(this Dictionary<T, U> d, int index, KeyValuePair<T, U> v)
        {
            if (d.ContainsKey(v.Key)) return false;
            var p = GetRange(d, 0, index);
            if (!p.Insert(v)) return false;
            p = p.AddRange(GetRange(d, index));
            d.Clear();
            d.AddRange(p);
            return true;
        }
        public static bool RemoveAt<T, U>(this Dictionary<T, U> d, int index) => d.Remove(d.ElementAt(index).Key);
    }
}
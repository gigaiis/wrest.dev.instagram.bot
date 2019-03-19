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
        public static double GetUnixTimestamp(DateTime? time = null) => ((time != null) ? (time.Value) : DateTime.UtcNow).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds / 1000;
        public static DateTime TimestampToDateTime(double timestamp) => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timestamp * 1000);
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
        public static async Task RestartApp()
        {
            await Log.Write(new List<string>() {
                string.Format("Restart app, success = {0}",
                System.Diagnostics.Process.Start(AppDomain.CurrentDomain.FriendlyName).ToString())
            }, Log.Type.warning);
            Environment.Exit(0);
        }
    }
}

namespace System.Collections.Generic
{
    public static class HashSetExtensionsClass
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null) => new HashSet<T>(source, comparer);
        public static bool RemoveAt<T>(this HashSet<T> source, int index) => index < source.Count() && (index >= 0) && source.Remove(source.ElementAt(index));
        public static HashSet<T> Range<T>(this IEnumerable<T> source, int index)
        {
            var res = new HashSet<T>();
            for (int i = index; i < source.Count(); i++)
                res.Add(source.ElementAt(i));
            return res;
        }
        public static HashSet<T> Range<T>(this IEnumerable<T> source, int index, int count)
        {
            var res = new HashSet<T>();
            var last_index = (index + count - 1 >= source.Count()) ? source.Count() - 1 : index + count - 1;
            for (int i = index; i <= last_index; i++)
                res.Add(source.ElementAt(i));
            return res;
        }
        public static int IndexOf<T>(this IEnumerable<T> source, T value)
        {
            for (int i = 0; i < source.Count(); i++)
                if (source.ElementAt(i).Equals(value)) return i;
            return -1;
        }
        public static int Insert<T>(this HashSet<T> source, int index, T value, bool isOverride = false)
        {
            if (source.Contains(value))
                return (!isOverride || !source.Remove(value)) ? 0 : 3;
            else if (source.Count == index)
            {
                source.Add(value);
                return 1;
            }
            var p = source.Range(0, index);
            if (!p.Add(value)) return 0;
            p = p.AddRange(source.Range(index));
            source.Clear();
            source.AddRange(p);
            return 1;
        }
        public static HashSet<T> AddRange<T>(this HashSet<T> source, HashSet<T> value)
        {
            foreach (var e in value)
                source.Add(e);
            return source;
        }
        public static bool Swap<T>(this HashSet<T> source, int p1, int p2)
        {
            if ((p1 == p2) || !(p1 < source.Count() || p2 < source.Count())) return false;
            var dp = source.ElementAt(p1);
            return (source.Replace(p1, source.ElementAt(p2)) & source.Replace(p2, dp)) == 1;
        }
        public static int Replace<T>(this HashSet<T> source, int p, T value)
        {
            var result = ((p < source.Count()) && (p >= 0)
                && source.Remove(source.ElementAt(p))) ? 1 : 0;
            var resultInsert = source.Insert(p, value, true);
            return result | (resultInsert & 2);
        }
    }
}
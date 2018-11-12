using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace main
{
    public static class Log
    {
        static int id_indexer = 0;
        public enum Type { unknown = 0, log = 1, info = 2, warning = 3, error = 4 };
        public class Log_object
        {
            public Type type;
            public List<string> text;
            public int timestamp;
            public int id;
            public Log_object(List<string> text, Type type = Type.log)
            {
                id = Dev.GetUnixTimestamp() - Program.init_time + (++id_indexer);
                this.type = type;
                timestamp = Dev.GetUnixTimestamp();
                this.text = text;
            }
        }
        public static bool isSave = false;
        public static readonly string Log_name = "log.txt";
        public static readonly int max_log_size = 1024;
        public static bool isWork = true, isInit = false;
        public static List<Log_object> list = new List<Log_object>();
        public static async void Init(bool _isWork = true)
        {
            isWork = _isWork;
            var _list = JsonConvert.DeserializeObject<List<Log_object>>(
                await Dev.ReadAsync(Log_name)
            );
            if (_list != null) list = _list;
            isInit = true;
        }
        public static List<Log_object> Delete(int id)
        {
            var _l = list.Where(e => e.id == id).ToList();
            if (_l.Count != 0) list.Remove(_l.First());
            Save();
            return list;
        }
        public static async void Save()
        {
            if (!isSave)
            {
                isSave = true;
                var t = Dev.WriteAsync(Log_name, JsonConvert.SerializeObject(list));
                while (!t.IsCompleted)
                    Thread.Sleep(33);
                isSave = false;
            }
        }
        public static Log_object Write(List<string> text, Type type = Type.log)
        {
            if (!isWork || !isInit) return null;
            if (list.Count > max_log_size) list.RemoveAt(0);
            var row = new Log_object(text, type);
            list.Add(row);
            Save();
            return row;
        }
    }
}

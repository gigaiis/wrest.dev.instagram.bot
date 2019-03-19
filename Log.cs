using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace main
{
    public static class Log
    {
        static int id_indexer = 0;
        public enum Type { unknown = 0, log = 1, info = 2, warning = 3, error = 4 };
        public class Row
        {
            public Type type;
            public List<string> text;
            public double timestamp;
            public long id;
            public Row(List<string> text, Type type = Type.log)
            {
                id = (long)(Dev.GetUnixTimestamp() - Program.init_time + (++id_indexer));
                this.type = type;
                timestamp = Dev.GetUnixTimestamp();
                this.text = text;
            }
        }
        public static bool isSave = false;
        public static readonly string Log_name = "log.txt";
        public static readonly int max_log_size = 1024;
        public static bool isWork = true, isInit = false;
        public static List<Row> list = new List<Row>();
        public static async void Init(bool _isWork = true)
        {
            isWork = _isWork;
            var _list = JsonConvert.DeserializeObject<List<Row>>(
                await Dev.ReadAsync(Log_name)
            );
            if (_list != null) list = _list;
            isInit = true;
        }
        public static async Task<List<Row>> Delete(int id)
        {
            var _l = list.Where(e => e.id == id).ToList();
            if (_l.Count != 0) list.Remove(_l.First());
            await Save();
            return list;
        }
        public static async Task Save()
        {
            if (!isSave)
            {
                isSave = true;
                await Dev.WriteAsync(Log_name, JsonConvert.SerializeObject(new List<Row>(list)));
                isSave = false;
            }
        }
        public static async Task<Row> Write(List<string> text, Type type = Type.log)
        {
            if (!isWork || !isInit) return null;
            if (list.Count > max_log_size) list.RemoveAt(0);
            var row = new Row(text, type);
            list.Add(row);
            await Save();
            return row;
        }
    }
}

using System;
using System.IO;

namespace main
{
    public static class Log
    {
        public static readonly string Log_name = "log.txt";
        public static bool isWork = true;
        public static void Write(string text, bool include_date = false)
        {
            if (isWork)
            {
                using (FileStream File = new FileStream(Log_name, FileMode.Append, FileAccess.Write))
                using (StreamWriter o = new StreamWriter(File))
                {
                    var st = !include_date ? text : string.Format("[{0}][{1}]: {2}", Dev.GetUnixTimestamp(), DateTime.UtcNow.ToString(), text);
                    o.WriteLine(st);
                }
            }
        }
    }
}

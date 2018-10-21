using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace main
{
    public static class Log
    {
        public static readonly string Log_name = "log.txt";
        public static bool isWork = true;
        public static void Write(string text)
        {
            if (isWork)
            {
                using (FileStream File = new FileStream(Log_name, FileMode.Append, FileAccess.Write))
                using (StreamWriter o = new StreamWriter(File))
                {
                    o.WriteLine(string.Format("[{0}][{1}]: {2}", Dev.GetUnixTimestamp(), DateTime.UtcNow.ToString(), text));
                }
            }
        }
    }
}

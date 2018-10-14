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
        public static async void WriteAsync(string filename, string text)
        {
            using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                await fs.WriteAsync(bytes, 0, bytes.Length);
                fs.Close();
            }
        }
    }
}

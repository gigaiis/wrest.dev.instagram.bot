using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HTTPServer
{
    public class Client
    {
        private void SendError(TcpClient Client, int Code)
        {
            string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
            string Html = "<html><body><h1>" + CodeStr + "</h1></body></html>";
            string Str = "HTTP/1.1 " + CodeStr + "\nContent-type: text/html\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
            byte[] Buffer = Encoding.ASCII.GetBytes(Str);
            Client.GetStream().Write(Buffer, 0, Buffer.Length);
            Client.Close();
        }
        public TcpClient Html(TcpClient Client, string text)
        {
            string _Headers = "HTTP/1.1 200 OK\nContent-Type: text/html\nContent-Length: " + text.Length + "\n\n";
            byte[] _HeadersBuffer = Encoding.UTF8.GetBytes(_Headers);
            Client.GetStream().Write(_HeadersBuffer, 0, _HeadersBuffer.Length);
            Client.GetStream().Write(Encoding.UTF8.GetBytes(text), 0, text.Length);
            return Client;
        }
        public TcpClient Json(TcpClient Client, object o)
        {
            string answ = JsonConvert.SerializeObject(o);
            string _Headers = "HTTP/1.1 200 OK\nContent-Type: application/json\nContent-Length: " + answ.Length + "\n\n";
            byte[] _HeadersBuffer = Encoding.UTF8.GetBytes(_Headers);
            Client.GetStream().Write(_HeadersBuffer, 0, _HeadersBuffer.Length);
            Client.GetStream().Write(Encoding.UTF8.GetBytes(answ), 0, answ.Length);
            return Client;
        }
        public Client(TcpClient Client)
        {
            string Request = "";
            byte[] Buffer = new byte[1024];
            int Count;
            while ((Count = Client.GetStream().Read(Buffer, 0, Buffer.Length)) > 0)
            {
                Request += Encoding.ASCII.GetString(Buffer, 0, Count);
                if (Request.IndexOf("\r\n\r\n") >= 0 || Request.Length > 4096) break;
            }
            Match ReqMatch = Regex.Match(Request, @"^\w+\s+([^\s\?]+)([^\s]*)\s+HTTP/.*|");
            if (ReqMatch == Match.Empty)
            {
                SendError(Client, 400);
                return;
            }
            string RequestUri = Uri.UnescapeDataString(ReqMatch.Groups[1].Value);

            if (RequestUri.IndexOf("..") >= 0)
            {
                SendError(Client, 400);
                return;
            }
            if (RequestUri.EndsWith("/")) RequestUri += "index.html";
            FileStream FS = null;
            string ContentType = "";
            string FilePath = "www/" + RequestUri;
            if (!File.Exists(FilePath))
            {
                var data = ReqMatch.Groups[2].Value.ToLower();
                var dic_data = new Dictionary<string, string>();
                var m = (new Regex("[?|&](?<k>[a-z]{1,})=(?<v>[^&#]*)")).Matches(data);
                foreach (Match _m in m)
                    if (_m.Success)
                        if (dic_data.ContainsKey(_m.Groups["k"].Value)) dic_data[_m.Groups["k"].Value] = _m.Groups["v"].Value;
                        else dic_data.Add(_m.Groups["k"].Value, _m.Groups["v"].Value);

                if (RequestUri == "/api/log.get") Json(Client, main.Log.list).Close();
                else if (RequestUri == "/api/log.delete")
                {
                    if (dic_data.ContainsKey("id"))
                    {
                        var id = -1;
                        if (!Int32.TryParse(dic_data["id"], out id)) Json(Client, new { error = 503, errormessage = "Error convert id to int" }).Close();
                        else
                        {
                            var _l = main.Log.Delete(id);
                            while (_l.Count > 0 && _l.ElementAt(0).text.Count == 0)
                                _l.RemoveAt(0);
                            Json(Client, _l).Close();
                        }
                    }
                    else Json(Client, new { error = 503, errormessage = "Arg \"id\" not found" }).Close();
                }
                else if (RequestUri == "/api/log.deleteall")
                {
                    main.Log.list.Clear();
                    Json(Client, main.Log.list).Close();
                }
                else if (RequestUri == "/log")
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            Html(Client, await main.Dev.ReadAsync(FilePath + ".html")).Close();
                        }
                        catch (Exception)
                        {
                            SendError(Client, 500);
                            return;
                        }
                    });
                }
                else if (RequestUri == "/stats")
                {
                    var obj = new List<object>();

                    var c1 = main.InstagramApi.account.access_tool.current_action_follow_list;
                    var c2 = main.Web.current_action_web_list;
                    var d = new Dictionary<object, Dictionary<int, List<object>>>()
                    {
                        {main.InstagramApi.account.access_tool.act.act_type.followers_follow, new Dictionary<int, List<object>>() },
                        {main.InstagramApi.account.access_tool.act.act_type.followers_unfollow, new Dictionary<int, List<object>>() },
                        {main.InstagramApi.account.access_tool.act.act_type.following_follow, new Dictionary<int, List<object>>() },
                        {main.InstagramApi.account.access_tool.act.act_type.following_unfollow, new Dictionary<int, List<object>>() },


                        {main.Web.act.act_type.get, new Dictionary<int, List<object>>() },
                        {main.Web.act.act_type.post, new Dictionary<int, List<object>>() }
                    };

                    int t = 0;
                    if (dic_data.ContainsKey("t")) Int32.TryParse(dic_data["t"], out t);

                    for (var q = 0; q < c1.Count; q++)
                    {
                        var i = c1[q];
                        if (d.ContainsKey(i.act_Type))
                        {
                            var z = d[i.act_Type];
                            var w = z.Keys.Where(_k => _k > i.timestamp - t && _k < i.timestamp).ToList();
                            int k = w.Count > 0 ? w.Last() : i.timestamp;
                            if (z.ContainsKey(k))
                            {
                                z[k][0] = (int)z[k][0] + 1;
                                ((List<string>)z[k][1]).Add(i.username);
                            }
                            else z.Add(k, new List<object>() { 1, new List<string>() { i.username } });
                        }

                        //foreach (var t in d)
                        //    if (!d[t.Key].ContainsKey(i.timestamp)) d[t.Key].Add(i.timestamp, new List<object>() { 0, new List<string>() });
                    }

                    for (var q = 0; q < c2.Count; q++)
                    {
                        var i = c2[q];
                        if (d.ContainsKey(i.act_Type))
                        {
                            var z = d[i.act_Type];
                            var w = z.Keys.Where(_k => _k > i.timestamp - t && _k < i.timestamp).ToList();
                            int k = w.Count > 0 ? w.Last() : i.timestamp;
                            if (z.ContainsKey(k))
                            {
                                z[k][0] = (int)z[k][0] + 1;
                                ((List<string>)z[k][1]).Add(i.url);
                            }
                            else z.Add(k, new List<object>() { 1, new List<string>() { i.url } });

                            //foreach (var _t in d)
                            //    if (!_t.Value.ContainsKey(i.timestamp)) _t.Value.Add(i.timestamp, new List<object>() { 0, new List<string>() });
                        }
                    }

                    foreach (var i in d)
                    {
                        var z = new List<object>();
                        foreach (var q in i.Value)
                            z.Add(new List<object>() { q.Key, q.Value[0], q.Value[1] });
                        obj.Add(z);
                    }


                    string answ = JsonConvert.SerializeObject(obj);
                    string _Headers = "HTTP/1.1 200 OK\nContent-Type: application/json\nContent-Length: " + answ.Length + "\n\n";
                    byte[] _HeadersBuffer = Encoding.UTF8.GetBytes(_Headers);
                    Client.GetStream().Write(_HeadersBuffer, 0, _HeadersBuffer.Length);
                    Client.GetStream().Write(Encoding.UTF8.GetBytes(answ), 0, answ.Length);
                    Client.Close();
                }
                else SendError(Client, 404);
                return;
            }

            string Extension = RequestUri.Substring(RequestUri.LastIndexOf('.'));
            switch (Extension)
            {
                case ".htm":
                case ".html":
                    ContentType = "text/html";
                    break;
                case ".css":
                    ContentType = "text/css";
                    break;
                case ".js":
                    ContentType = "text/javascript";
                    break;
                case ".jpg":
                    ContentType = "image/jpeg";
                    break;
                case ".jpeg":
                case ".png":
                case ".gif":
                    ContentType = "image/" + Extension.Substring(1);
                    break;
                default:
                    ContentType = "application/" + ((Extension.Length > 1) ? Extension.Substring(1) : "unknown");
                    break;
            }
            try
            {
                FS = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception)
            {
                SendError(Client, 500);
                return;
            }
            string Headers = "HTTP/1.1 200 OK\nContent-Type: " + ContentType + "\nContent-Length: " + FS.Length + "\n\n";
            byte[] HeadersBuffer = Encoding.ASCII.GetBytes(Headers);
            Client.GetStream().Write(HeadersBuffer, 0, HeadersBuffer.Length);
            while (FS.Position < FS.Length)
            {
                Count = FS.Read(Buffer, 0, Buffer.Length);
                Client.GetStream().Write(Buffer, 0, Count);
            }
            FS.Close();
            Client.Close();
        }
    }

    class Server
    {
        TcpListener Listener;
        public Server(int Port = 80)
        {
            Listener = new TcpListener(IPAddress.Any, Port);
            Listener.Start();
            while (true)
            {
                TcpClient Client = Listener.AcceptTcpClient();
                Thread Thread = new Thread(new ParameterizedThreadStart(ClientThread));
                Thread.Start(Client);
            }
        }
        static void ClientThread(Object StateInfo) => new Client((TcpClient)StateInfo);
        ~Server()
        {
            if (Listener != null) Listener.Stop();
        }
    }
}
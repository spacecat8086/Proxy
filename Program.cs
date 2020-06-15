using System;
using System.Threading;
using System.Net.Sockets;
using System.IO;

namespace Proxy
{
    class Program
    {
        static void Main(string[] args)
        {
            string blackList = getBlackList("blacklist.conf");
            ProxyServer proxyServer = new ProxyServer("127.0.0.1", 8086, blackList);
            proxyServer.Start();
            while (true)
            {
                Socket socket = proxyServer.Accept();
                Thread thread = new Thread(() => proxyServer.ReceiveData(socket));
                thread.Start();
            }
        }

        static string getBlackList(string path)
        {
            string blacklist = "";
            using (StreamReader reader = new StreamReader(path, System.Text.Encoding.Default))
            {
                blacklist = reader.ReadToEnd();
            }
            return blacklist;
        }
    }
}


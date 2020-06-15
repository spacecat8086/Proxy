using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace Proxy
{
    public class ProxyServer
    {
        private int port;
        private string host;
        private TcpListener listener;
        private string[] blackList;
        private byte[] buffer;

        public ProxyServer(string host, int port, string blacklist)
        {
            this.host = host;
            this.port = port;
            this.listener = new TcpListener(IPAddress.Parse(this.host), this.port);
            this.blackList = blacklist.Trim().Split(new char[] { '\r', '\n' });
        }

        public void Start()
        {
            listener.Start();
        }

        public Socket Accept()
        {
            return listener.AcceptSocket();
        }

        public void ReceiveData(Socket client)
        {
            NetworkStream browser = new NetworkStream(client);
            buffer = new byte[20 * 1024];
            while (true)                                                        // Читаем данные, которые отправляет браузер
            {
                if (!browser.CanRead)
                    return;
                try
                {
                    browser.Read(buffer, 0, buffer.Length);
                }
                catch (IOException e) { return; }
                HttpResponser(buffer, browser);
                client.Dispose();
            }
        }


        public void HttpResponser(byte[] buffer, NetworkStream browser)
        {
            TcpClient server;
            string responseRecord;
            string responseCode;
            try
            {
                buffer = CutGet(buffer);

                string[] temp = Encoding.ASCII.GetString(buffer).Trim().Split(new char[] { '\r', '\n' });

                string request = temp.FirstOrDefault(x => x.Contains("Host"));
                request = request.Substring(request.IndexOf(":") + 2);
                string[] hostAndPort = request.Trim().Split(new char[] { ':' });                   // Получаем имя домена и по возможности номер порта


                if (hostAndPort.Length == 2)                                                       // Соединяемся с сервером по имени хоста и по порту
                {                                                                                  // (или стандартному 80, или указанному)
                    server = new TcpClient(hostAndPort[0], int.Parse(hostAndPort[1]));
                }
                else
                {
                    server = new TcpClient(hostAndPort[0], 80);
                }

                NetworkStream serverStream = server.GetStream();                                   // Поток с сервером
                if (blackList != null && Array.IndexOf(blackList, request.ToLower()) != -1)
                {
                    byte[] blackResponse = Encoding.ASCII.GetBytes("HTTP/1.1 403 Forbidden\r\nContent-Type: text/html\r\nContent-Length: 19\r\n\r\nYou shall not pass!");
                    browser.Write(blackResponse, 0, blackResponse.Length);
                    responseCode = "403";
                    responseRecord = request + " " + responseCode;
                    Console.WriteLine(responseRecord);
                    return;
                }


                serverStream.Write(buffer, 0, buffer.Length);                                      // Отправляем данные на сервер, которые получили от браузера
                var bufResponse = new byte[32];                                                    // Для заголовка


                serverStream.Read(bufResponse, 0, bufResponse.Length);                             // Ответ от сервера
                browser.Write(bufResponse, 0, bufResponse.Length);                                 // Отправляем этот ответ браузеру
                
                string[] head = Encoding.UTF8.GetString(bufResponse).Split(new char[] { '\r', '\n' });     // Получаем код ответа

                responseCode = head[0].Substring(head[0].IndexOf(" ") + 1);
                responseRecord = request + " " + responseCode;
                Console.WriteLine(responseRecord);
                serverStream.CopyTo(browser);                                                      // Перенаправляем остальные данные от сервера к браузеру               
            }
            catch { return; }
        }

        private byte[] CutGet(byte[] buf)
        {
            string buffer = Encoding.ASCII.GetString(buf);
            Regex regex = new Regex(@"http:\/\/[a-z0-9а-яё\:\.]*");
            MatchCollection matches = regex.Matches(buffer);
            string host = matches[0].Value;
            buffer = buffer.Replace(host, "");
            buf = Encoding.ASCII.GetBytes(buffer);
            return buf;
        }

    }
}


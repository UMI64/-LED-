using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;
using System.Threading;

namespace SLED
{
    public class JoinWIFI
    {
        Socket c;
        static string IP = "192.168.4.1";
        static int Port = 8086;
        public bool DeviceJoinWIFI(string WIFIName, string WIFIPWD)
        {
            bool TimeOut = false;
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(IP), Port);//把ip和端口转化为IPEndpoint实例
                                                                       //创建socket并连接到服务器
            c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建Socket
            ManualResetEvent TimeoutObject = new ManualResetEvent(false);
            TimeoutObject.Reset();
            c.BeginConnect(ipe, asyncResult =>
            {
                if (TimeOut)
                    return;
                if (Send(c, WIFIName, WIFIPWD) == false)
                {
                    return;
                }
                else
                {
                    TimeoutObject.Set();
                }
            }, c);
            if (!TimeoutObject.WaitOne(700, false))
            {//链接超时
                TimeOut = true;
                c.Close();
                return false;
            }
            return true;
        }
        private bool Send(Socket c, string WIFIName, string WIFIPWD)
        {
            int DeadTime = 3;
            while (DeadTime > 0)
            {
                string sendStr = "WIFI=" + WIFIName + ".PWD=" + WIFIPWD + ".";
                byte[] bs = Encoding.ASCII.GetBytes(sendStr);//把字符串编码为字节
                try
                {
                    c.Send(bs, bs.Length, 0);//发送信息
                                             ///接受从服务器返回的信息
                    string recvStr = "";
                    byte[] recvBytes = new byte[1024];
                    int bytes;

                    c.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 600);
                    bytes = c.Receive(recvBytes, recvBytes.Length, 0);//从服务器端接受返回信息
                    recvStr += Encoding.ASCII.GetString(recvBytes, 0, bytes);
                    if (recvStr.Contains("OK"))
                    {
                        return true;
                    }
                    throw new Exception("");
                }
                catch
                {
                    DeadTime--;
                }
            }
            return false;
        }
        public bool WaitingConnectResout()
        {
            try
            {
                string recvStr = "";
                byte[] recvBytes = new byte[1024];
                int bytes;

                c.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 15000);
                bytes = c.Receive(recvBytes, recvBytes.Length, 0);//从服务器端接受返回信息
                recvStr += Encoding.ASCII.GetString(recvBytes, 0, bytes);
                if (recvStr.Contains("OK"))
                {
                    return true;
                }
                throw new Exception("");
            }
            catch
            {
                return false;
            }
        }

    }
}
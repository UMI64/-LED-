using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

using Android.App;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Text.Format;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Socket = System.Net.Sockets.Socket;
using SocketType = System.Net.Sockets.SocketType;
using ProtocolType = System.Net.Sockets.ProtocolType;
using System.Threading;

namespace SLED
{
    public class Device
    {
        public string DevIp;
        public Socket DevSocket;
        public int LUX=0;//LED的光通量
        public float LEDPWM = 0;//LED当前的PWM占空比
        public bool HumanNearby = false;
        public bool LEDOn = false;//LED开关状态
        public bool DeviceOnline = false;//LED链接状态
        public int DeadTime=0;//LED链接死亡计数
        public bool CloseThread = false;//是否关闭线程
        public bool HaveThread = false;//是否有线程占用次LED
    }
    public class DeviceManager
    {
        public Dictionary<string, string> DeviceIP = new Dictionary<string, string>();//扫描到的IP暂存位置
        public List<Device> Devices = new List<Device>();//成功连接上的设备

        private Context context;
        private TextView textView;
        private bool FLAG_Scaning=false;
        private readonly int ScanDeviceTheardNum = 32;//扫描线程总数
        private int ConnectDeviceTheardNum = 0;//连接线程总数
        public int TheardComplete = 0;//已完成的线程数
        public int TheardNum = 0;//当前总线程数
        public DeviceManager(Context context, TextView textView)
        {
            this.context = context;
            this.textView = textView;
        }
        /// <summary>
        /// 扫描局域网中的设备
        /// </summary>
        public void ScanDevice()
        {
            if(FLAG_Scaning) return;//如果当前正在扫描 则立刻退出
            FLAG_Scaning = true;//表示当前正在扫描
            DeviceIP.Clear();//清空IP缓存
            lock (this)
            {
                for(int count=0;count<Devices.Count;count++)
                {
                    Devices[count].CloseThread = true;//让所有连接上的设备的线程退出
                }
            }
            int AliveThread;//还活着的线程
            do
            {
                AliveThread = 0;
                for (int count = 0; count < Devices.Count; count++)
                {
                    if (Devices[count].HaveThread)//如果线程还活着 则活着的线程加1
                        AliveThread++;
                }
            } while (AliveThread>0);//等待所有线程退出
            Devices.Clear();//清空设备数据
            for (int i = 0; i < ScanDeviceTheardNum; i++)//启动扫描线程
            {
                new Java.Lang.Thread(ScanDeviceThread).Start();
            }
        }
        /// <summary>
        /// 尝试与扫描到的IP连接
        /// </summary>
        public void ConnectDevice()
        {
            ConnectDeviceTheardNum = DeviceIP.Count;
            for (int i = 0; i < ConnectDeviceTheardNum; i++)
            {
                new Java.Lang.Thread(ConnectDeviceThread).Start();
            }
        }
        public void ConnectDeviceThread()
        {
            string ip = "";
            lock (this)//拿出一个IP
            {
                foreach (var IP in DeviceIP)
                {
                    if (IP.Value.Length == 0)
                    {
                        ip = IP.Key;
                        break;
                    }
                }
                DeviceIP[ip] = "Connecting";
            }
            bool TimeOut=false;//超时标志
            bool Connected = false;//已经连接上标志
            string Resourt = "";
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), 8086);//把ip和端口转化为IPEndpoint实例
            //创建socket并连接到服务器
            Socket c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建Socket
            ManualResetEvent TimeoutObject = new ManualResetEvent(false);
            TimeoutObject.Reset();
            c.BeginConnect(ipe, asyncResult =>//开始链接
            {//链接完毕后
                try
                {
                    if (TimeOut)//如果超时了才进入 则立刻退出
                        throw new System.Exception("Connect time out");
                    c.EndConnect(asyncResult);
                    if (c.Connected)
                    {
                        //向服务器发送信息
                        Log.Info("tcp:"+ip, "Send message start");
                        string sendStr = "AreYouThere";
                        byte[] bs = Encoding.ASCII.GetBytes(sendStr);//把字符串编码为字节
                        c.Send(bs, bs.Length, 0);//发送信息
                        Log.Info("tcp:" + ip, "Send message complete");
                        ///接受从服务器返回的信息
                        string recvStr = "";
                        byte[] recvBytes = new byte[1024];
                        int bytes;
                        try
                        {
                            c.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 600);
                            bytes = c.Receive(recvBytes, recvBytes.Length, 0);//从服务器端接受返回信息
                            recvStr += Encoding.ASCII.GetString(recvBytes, 0, bytes);
                        }
                        catch //接收确认信息超时 立刻退出
                        {
                            throw new System.Exception("ReceiveTimeOut");
                        }
                        Resourt = recvStr;
                        Log.Info("tcp:"+ip, "Get essage:" + recvStr);//显示服务器返回信息
                        if (recvStr.Contains("AAA"))//获得了正确的确认信息
                        {
                            Connected = true;
                            lock (this)
                            {
                                Device device=new Device();//加入这个设备
                                device.DevIp = ip;
                                device.DevSocket = c;
                                Devices.Add(device);
                            }
                            new Java.Lang.Thread(ConmmunityDeviceThread).Start();//启动一个与当前设备通信的进程
                        }
                    }
                    else//没链接上时 立刻退出
                        throw new System.Exception("No connect");
                }
                catch (System.Exception e)
                {
                    Log.Error("tcp:"+ip, e.Message);//显示服务器返回信息
                    Resourt = e.Message;
                }
                finally
                {
                    lock (this)
                    {
                        DeviceIP[ip] = Resourt;
                    }
                    //使阻塞的线程继续        
                    TimeoutObject.Set();
                }
            }, c);
            //阻塞当前线程
            if (!TimeoutObject.WaitOne(700, false))
            {//链接超时
                if (Connected == false)//未连接上
                {
                    Log.Error("tcp:" + ip, "Time out");//显示服务器返回信息
                    TimeOut = true;
                    c.Close();
                }   
            }
            if (TheardComplete++ >= ConnectDeviceTheardNum)
            {
                TheardComplete = 0;//清空完成线程数
                FLAG_Scaning = false;//关闭扫描中标志 可以开启下一次扫描
            }
        }
        public void ConmmunityDeviceThread()
        {
            int count = 0;
            lock (this)
            {
                foreach (var dev in Devices)
                {
                    if (dev.HaveThread)
                        count++;
                    else
                        break;
                }
                Devices[count].HaveThread = true;
            }
            while(!Devices[count].CloseThread)//没收到关闭则继续获取数据
            {
                try
                {
                    string recvStr = "";
                    byte[] recvBytes = new byte[1024];
                    int bytes;
                    Devices[count].DevSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);
                    bytes = Devices[count].DevSocket.Receive(recvBytes, recvBytes.Length, 0);//从服务器端接受返回信息
                    recvStr += Encoding.ASCII.GetString(recvBytes, 0, bytes);
                    Devices[count].DeadTime = 0;
                    /*反序列化出数据*/
                    JObject jO = (JObject)JsonConvert.DeserializeObject(recvStr);
                    Devices[count].HumanNearby = bool.Parse(jO["HumanNearby"].ToString());
                    Devices[count].LUX = int.Parse(jO["LUX"].ToString());
                    Devices[count].LEDOn = bool.Parse(jO["LEDOn"].ToString());
                    Devices[count].LEDPWM = int.Parse(jO["LEDPWM"].ToString());

                    Devices[count].DeviceOnline = true;
                }
                catch
                {
                    if (Devices[count].DeadTime++ > 10)
                        Devices[count].DeviceOnline = false;
                }
                ((Activity)context).RunOnUiThread(() =>
                {
                    lock (this)
                    {
                        if (Devices.Count > 0)
                        {
                            if (((MainActivity)context).SelectLED.Length == 0)//默认选择第一个设备
                            {
                                ((MainActivity)context).SelectLED = Devices[0].DevIp;
                            }
                            foreach (var dev in Devices)
                            {
                                if (dev.DevIp == ((MainActivity)context).SelectLED)
                                {
                                    if (dev.DeviceOnline)
                                    {
                                        ((MainActivity)context).HumantextView.Text = dev.HumanNearby ? "检测到人类" : "未检测到人类";
                                        ((MainActivity)context).LXTextView.Text = dev.LUX.ToString() + "Lux";
                                        ((MainActivity)context).PWMText.Text = "PWM:"+ dev.LEDPWM+ "%";
                                        int LuxPercentage = (int)(dev.LUX / 500.0f * 100);
                                        ((MainActivity)context).CircleBarView.SetProgressNum(1000, LuxPercentage);//设置动画时间为3000毫秒，即3秒
                                        ((MainActivity)context).SetLEDButton(dev.LEDOn);
                                    }
                                    else
                                    {
                                        ((MainActivity)context).HumantextView.Text = "设备离线";
                                        ((MainActivity)context).LXTextView.Text = "0Lux";
                                        ((MainActivity)context).PWMText.Text = "PWM:0%";
                                        ((MainActivity)context).CircleBarView.SetProgressNum(1000, 0);//设置动画时间为3000毫秒，即3秒
                                        ((MainActivity)context).SetLEDButton(false);
                                    }
                                    break;
                                }
                            }
                        }
                        else //没有设备时清空数据
                        {
                            ((MainActivity)context).SelectLED = "";
                            ((MainActivity)context).HumantextView.Text = "设备未连接";
                            ((MainActivity)context).LXTextView.Text = "0Lux";
                            ((MainActivity)context).PWMText.Text = "PWM:0%";
                            ((MainActivity)context).CircleBarView.SetProgressNum(1000, 0);//设置动画时间为3000毫秒，即3秒
                            ((MainActivity)context).SetLEDButton(false);
                        }
                    }
                });
            }
            Devices[count].DevSocket.Close();
            Devices[count].HaveThread = false;
        }
        public void ScanDeviceThread()
        {
            int BEGANNum = 0;//开始扫描的起始
            int ENDNum = 0;//开始扫描的结束
            int mTheardNum;
            lock (this)
            {
                mTheardNum = TheardNum++;//当前是第几个线程
            }
            BEGANNum = mTheardNum * 256 / ScanDeviceTheardNum;
            ENDNum = BEGANNum + 256 / ScanDeviceTheardNum;
            ConnectivityManager cm = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
            NetworkInfo activeNetwork = cm.ActiveNetworkInfo;
            WifiManager wm = (WifiManager)context.GetSystemService(Context.WifiService);

            WifiInfo connectionInfo = wm.ConnectionInfo;
            int ipAddress = connectionInfo.IpAddress;
            string ipString = Formatter.FormatIpAddress(ipAddress);

            string prefix = ipString.Substring(0, ipString.LastIndexOf(".") + 1);

            for (int i = BEGANNum; i < ENDNum; i++)//执行扫描
            {
                string testIp = prefix + i.ToString();
                InetAddress address = InetAddress.GetByName(testIp);
                bool reachable = address.IsReachable(800);
                string hostName = address.CanonicalHostName;
                if (reachable)//如果Ping通 吧IP加入
                {
                    lock (this)
                    {
                        DeviceIP.Add(testIp, "");
                    }
                }
            }
            TheardComplete++;//线程完成数加1
            if (TheardComplete >= ScanDeviceTheardNum)//如果这个是最后一个完成的线程
            {
                Log.Info("TCP", "StartConnectDevice : " + DeviceIP.Count.ToString());
                TheardComplete = 0;
                TheardNum = 0;
                ConnectDevice();//开始尝试连接
            }
        }
    }
}
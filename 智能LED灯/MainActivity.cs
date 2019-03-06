using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

namespace SLED
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        DeviceManager deviceManager;
        public TextView LXTextView;
        public TextView HumantextView;
        public TextView PWMText;
        public CircleBarView CircleBarView;
        public ImageButton LEDButton;
        public string SelectLED = "";
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            CircleBarView = FindViewById<CircleBarView>(Resource.Id.circle_view);
            HumantextView = FindViewById<TextView>(Resource.Id.HumantextView);
            PWMText = FindViewById<TextView>(Resource.Id.PWMText);
            LEDButton = FindViewById<ImageButton>(Resource.Id.LEDButton);
            SetLEDButton(false);
            TextView ScanedDevice = FindViewById<TextView>(Resource.Id.ScanedDevice);
            deviceManager = new DeviceManager(this, ScanedDevice);
            LEDButton.Click += (object sender, EventArgs e) =>
            {
                CircleBarView.SetProgressNum(1000, 47);//设置动画时间为3000毫秒，即3秒
                ScanedDevice.Text += "正在搜索设备";
                deviceManager.ScanDevice();
            };
            LXTextView = FindViewById<TextView>(Resource.Id.LXTextView);
            Button JoinWifiButton = FindViewById<Button>(Resource.Id.JoinWifiButton);
            JoinWifiButton.Click += (object sender, EventArgs e) =>
            {
                Android.Support.V7.App.AlertDialog.Builder customizeDialog = new Android.Support.V7.App.AlertDialog.Builder(this);
                View dialogView = LayoutInflater.From(this).Inflate(Resource.Layout.JoinWIFI, null);
                EditText WIFIPWDEditText = dialogView.FindViewById<EditText>(Resource.Id.WIFIPWDEditText);
                EditText WIFINameEditText = dialogView.FindViewById<EditText>(Resource.Id.WIFINameEditText);
                TextView StateTextView = dialogView.FindViewById<TextView>(Resource.Id.StateTextView);
                dialogView.FindViewById<Button>(Resource.Id.OkButton).Click += (object bsender, EventArgs be) =>
                {
                    JoinWIFI joinWIFI = new JoinWIFI();
                    if (joinWIFI.DeviceJoinWIFI(WIFINameEditText.Text, WIFIPWDEditText.Text))
                    {//成功继续监听
                        if(joinWIFI.WaitingConnectResout()==false)
                        {
                            StateTextView.Text = "WIFI信息发送失败";
                        }
                    }
                    else
                    {//失败则要求用户处理
                        StateTextView.Text = "WIFI信息发送失败";
                    }
                };
                customizeDialog.SetView(dialogView);
                customizeDialog.Show();
            };
        }
        public void SetLEDButton(bool OnOFF)
        {
            LEDButton.Background = OnOFF ? GetDrawable(Resource.Drawable.LEDButtonON) : GetDrawable(Resource.Drawable.LEDButtonOFF);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;
using System.Timers;
using System.Collections;
using System.Threading;
using System.Windows.Forms;

namespace Speed
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private double nLeft = 0, nTop = 0;
        private System.Timers.Timer timer;
        private NetworkMonitor monitor;
        private String BgColor = "#FF11113C";//背景色
        private String ArrColor = "#FF60F77D";//箭头颜色
        private String FonColor = "White";//箭头颜色
        private bool CanDrag = true;//是否可以拖拽
        private bool IsTop = true;//是否始终置顶，为false时为一般置顶

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left; //最左坐标
            public int Top; //最上坐标
            public int Right; //最右坐标
            public int Bottom; //最下坐标
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        WindowState wsl;
        System.Windows.Forms.NotifyIcon notifyIcon = null;

        /// <summary>
        /// 初始化Config配置
        /// </summary>
        public void InitResource()
        {
            CanDrag = Config.Default.CanDrag;
            IsTop = Config.Default.IsTop;
            nLeft = Config.Default.Left; nTop = Config.Default.Top;
            if (Left == 0 && Top == 0)
            {
                var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
                nLeft = desktopWorkingArea.Right - this.Width + 1;
                nTop = desktopWorkingArea.Bottom - this.Height + 1;
                Config.Default.Left = nLeft;
                Config.Default.Top = nTop;
            }
            BgColor = Config.Default.BgColor;
            ArrColor = Config.Default.ArrowColor;
            FonColor = Config.Default.FontColor;
        }

        /// <summary>
        /// 设置窗口置顶，单次置顶会因为焦点的原因
        /// </summary>
        private void SetWindowTop()
        {
            Dispatcher.Invoke(new Action(
                delegate
                {
                    IntPtr hwnd = new WindowInteropHelper(this).Handle;
                    TopMostTool.SetTopmost(hwnd);
                }
            ));
        }

        /// <summary>
        /// 设置界面上速度的显示
        /// </summary>
        /// <param name="USpeed">上传速度</param>
        /// <param name="DSpeed">下载速度</param>
        private void SetSpeed(float USpeed = 0, float DSpeed = 0)
        {
            Dispatcher.Invoke(new Action(
                delegate
                {
                    this.UpSpeed.Content = TransSpeed(USpeed);
                    this.DownSpeed.Content = TransSpeed(DSpeed);
                }
            ));
        }

        /// <summary>
        /// 设置窗口位置
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Top"></param>
        private void SetPosition(double Left = 0, double Top = 0)
        {
            this.Left = Left;
            this.Top = Top;
        }

        private SolidColorBrush StringToBrush(String color)
        {
            System.Drawing.Color DColor = System.Drawing.ColorTranslator.FromHtml(color);
            System.Windows.Media.Color MColor = new System.Windows.Media.Color();
            MColor = System.Windows.Media.Color.FromArgb(DColor.A, DColor.R, DColor.G, DColor.B);
            return new SolidColorBrush(MColor);
        }

        public List<T> GetChildObjects<T>(DependencyObject obj) where T : FrameworkElement
        {
            DependencyObject child = null;
            List<T> childList = new List<T>();

            for (int i = 0; i <= VisualTreeHelper.GetChildrenCount(obj) - 1; i++)
            {
                child = VisualTreeHelper.GetChild(obj, i);

                if (child is T)
                {
                    childList.Add((T)child);
                }
                childList.AddRange(GetChildObjects<T>(child));
            }
            return childList;
        }

        /// <summary>
        /// 转换网速单位
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        public string TransSpeed(double speed)
        {
            //网速单位b/s,kb/s,mb/s现在网速没有超过mb/s的
            double num = speed / 1024.0; //网速的数字部分 
            int index = 0; //对应的单位部分
            string[] unit = new string[] { "k/s", "m/s" };
            while (num > 1024)
            {
                num = num / 1024.0;
                index++;
            }
            return num.ToString("f2") + unit[index];
        }

        private  void InitIcon()
        {
            this.notifyIcon = new System.Windows.Forms.NotifyIcon();
            //this.notifyIcon.BalloonTipText = "正在启动网速监控程序"; //设置程序启动时显示的文本
            this.notifyIcon.Text = "Speed网速监控";//最小化到托盘时，鼠标点击时显示的文本
            this.notifyIcon.Icon = (System.Drawing.Icon)Properties.Resources.ResourceManager.GetObject("favorite32");// new System.Drawing.Icon(Resources.);//程序图标
            this.notifyIcon.Visible = true;
            
            //右键菜单--退出菜单项
            //System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem("关闭");
            //exit.Click += new EventHandler(CloseWindow);

            //右键菜单--测速
            //System.Windows.Forms.MenuItem speed = new System.Windows.Forms.MenuItem("测速");
            //exit.Click += new EventHandler(SpeedTest);

            //关联托盘控件
            //System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { speed,exit };
            //notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);

            notifyIcon.MouseDoubleClick += OnNotifyIconDoubleClick;
            //this.notifyIcon.ShowBalloonTip(1000);

        }

        public MainWindow()
        {
            //AllocConsole();
            InitializeComponent();
            this.Topmost = true;
            InitIcon();
            wsl = WindowState.Minimized;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //初始化参数
            InitResource();
            //调整窗口位置
            SetPosition(nLeft, nTop);
            //设置背景色
            SpeedContent.Background = StringToBrush(BgColor);
            //设置箭头颜色
            SolidColorBrush Arrow = StringToBrush(ArrColor);
            ArrowUp.Foreground = Arrow;
            ArrowDown.Foreground = Arrow;
            //设置字体颜色
            SolidColorBrush Font = StringToBrush(FonColor);
            UpSpeed.Foreground = Font;
            DownSpeed.Foreground = Font;

            //设置程序置顶，但是现在有个问题，程序获取焦点时才完全置顶
            //先初始化timer对象
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            if (IsTop)
            {
                topWindow.IsChecked = true;
                timer.Start();
            }
            else
            {
                SetWindowTop();
            }
            if (!CanDrag)
            {
                position.IsChecked = true;
            }
            //网络展示
            monitor = new NetworkMonitor();
            monitor.StartMonitoring(SpeedChange);
        }

        /// <summary>
        /// 速度改变事件，每秒一次
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpeedChange(object sender, EventArgs e)
        {
            ChangeEvent dat = (ChangeEvent)e;
            ArrayList adapters = dat.adapters;
            float DSpeed = 0, USpeed = 0;
            foreach (NetworkAdapter adapter in adapters)
            {
                DSpeed += adapter.DownloadSpeed;
                USpeed += adapter.UploadSpeed;
            }
            SetSpeed(USpeed, DSpeed);
        }
        /// <summary>
        /// 鼠标拖拽事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (CanDrag && e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
                IntPtr awin = GetForegroundWindow();    //获取当前窗口句柄
                RECT fx = new RECT();
                GetWindowRect(awin, ref fx);//h为窗口句柄
                int x = fx.Left; int y = fx.Top;
                Config.Default.Top = y;
                Config.Default.Left = x;
                Config.Default.Save();
            }
        }
        /// <summary>
        /// 通知栏图标双击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNotifyIconDoubleClick(object sender, EventArgs e)
        {
            /*
             * 这一段代码需要解释一下:
             * 窗口正常时双击图标执行这段代码是这样一个过程：
             * this.Show()-->WindowState由Normail变为Minimized-->Window_StateChanged事件执行(this.Hide())-->WindowState由Minimized变为Normal-->窗口隐藏
             * 窗口隐藏时双击图标执行这段代码是这样一个过程：
             * this.Show()-->WindowState由Normail变为Minimized-->WindowState由Minimized变为Normal-->窗口显示
             */
            this.Show();
            this.WindowState = WindowState.Minimized;
            this.WindowState = WindowState.Normal;
        }
        /// <summary>
        /// 关闭程序的事件 分为右键菜单关闭 通知栏图标右键菜单关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseWindow(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 网速测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpeedTest(object sender, EventArgs e)
        {
            //this.Close();
        }

        /// <summary>
        /// 修改位置是否固定的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DragControl(object sender, RoutedEventArgs e)
        {
            if (CanDrag)
            {
                CanDrag = false;
            }
            else
            {
                CanDrag = true;
            }
            Config.Default.CanDrag = CanDrag;
            Config.Default.Save();
        }
        /// <summary>
        /// 修改背景色的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorControl(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem menuItem = (System.Windows.Controls.MenuItem)sender;
            List<System.Windows.Controls.Label> label = GetChildObjects<System.Windows.Controls.Label>(menuItem);
            SpeedContent.Background = label[0].Background;
            //保存颜色到Config
            Config.Default.BgColor = label[0].Background.ToString();
            Config.Default.Save();
        }
        /// <summary>
        /// 修改是否一直置顶的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TopControl(object sender, RoutedEventArgs e)
        {
            if (IsTop)
            {
                IsTop = false;
                timer.Stop();
            }
            else
            {
                IsTop = true;
                timer.Start();
            }
            Config.Default.IsTop = IsTop;
            Config.Default.Save();
        }
        /// <summary>
        /// 修改箭头颜色的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArrowControl(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem menuItem = (System.Windows.Controls.MenuItem)sender;
            List<System.Windows.Controls.Label> label = GetChildObjects<System.Windows.Controls.Label>(menuItem);
            ArrowUp.Foreground = label[0].Background;
            ArrowDown.Foreground = label[0].Background;
            Config.Default.ArrowColor = label[0].Background.ToString();
            Config.Default.Save();
        }
        /// <summary>
        /// 修改字体颜色的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontControl(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem menuItem = (System.Windows.Controls.MenuItem)sender;
            List<System.Windows.Controls.Label> label = GetChildObjects<System.Windows.Controls.Label>(menuItem);
            UpSpeed.Foreground = label[0].Background;
            DownSpeed.Foreground = label[0].Background;
            Config.Default.FontColor = label[0].Background.ToString();
            Config.Default.Save();
        }

        /// <summary>
        /// 一直置顶，定时执行的任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SetWindowTop();
        }
    }

    public class TopMostTool
    {
        public static int SW_SHOW = 5;
        public static int SW_NORMAL = 1;
        public static int SW_MAX = 3;
        public static int SW_HIDE = 0;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);    //窗体置顶
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);    //取消窗体置顶
        public const uint SWP_NOMOVE = 0x0002;    //不调整窗体位置
        public const uint SWP_NOSIZE = 0x0001;    //不调整窗体大小
        public bool isFirst = true;

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", EntryPoint = "ShowWindow")]
        public static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        /// <summary>
        /// 查找子窗口
        /// </summary>
        /// <param name="hwndParent"></param>
        /// <param name="hwndChildAfter"></param>
        /// <param name="lpClassName"></param>
        /// <param name="lpWindowName"></param>
        /// <returns></returns>
        [DllImport("User32.dll", EntryPoint = "FindWindowEx")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);

        /// <summary>
        /// 窗体置顶，可以根据需要传入不同的值(需要置顶的窗体的名字Title)
        /// </summary>
        public static void SetTopWindow()
        {
            IntPtr frm = FindWindow(null, "窗体的名字Title");    // 程序中需要置顶的窗体的名字
            if (frm != IntPtr.Zero)
            {
                SetWindowPos(frm, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

                var child = FindWindowEx(frm, IntPtr.Zero, null, "子窗体的名字Title");
            }
        }

        public static void SetTopmost(IntPtr handle)
        {
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }
    }

    /// <summary>
    /// Represents a network adapter installed on the machine.
    /// Properties of this class can be used to obtain current network speed.
    /// </summary>
    public class NetworkAdapter
    {
        /// <summary>
        /// Instances of this class are supposed to be created only in an NetworkMonitor.
        /// </summary>
        internal NetworkAdapter(string name)
        {
            this.name = name;
        }

        private long dlSpeed, ulSpeed;         // Download/Upload speed in bytes per second.
        private long dlValue, ulValue;         // Download/Upload counter value in bytes.
        private long dlValueOld, ulValueOld; // Download/Upload counter value one second earlier, in bytes.

        internal string name;                                // The name of the adapter.
        internal PerformanceCounter dlCounter, ulCounter;    // Performance counters to monitor download and upload speed.
        /// <summary>
        /// Preparations for monitoring.
        /// </summary>
        internal void init()
        {
            // Since dlValueOld and ulValueOld are used in method refresh() to calculate network speed, they must have be initialized.
            this.dlValueOld = this.dlCounter.NextSample().RawValue;
            this.ulValueOld = this.ulCounter.NextSample().RawValue;
        }
        /// <summary>
        /// Obtain new sample from performance counters, and refresh the values saved in dlSpeed, ulSpeed, etc.
        /// This method is supposed to be called only in NetworkMonitor, one time every second.
        /// </summary>
        internal void refresh()
        {
            this.dlValue = this.dlCounter.NextSample().RawValue;
            this.ulValue = this.ulCounter.NextSample().RawValue;

            // Calculates download and upload speed.
            this.dlSpeed = this.dlValue - this.dlValueOld;
            this.ulSpeed = this.ulValue - this.ulValueOld;

            this.dlValueOld = this.dlValue;
            this.ulValueOld = this.ulValue;
        }
        /// <summary>
        /// Overrides method to return the name of the adapter.
        /// </summary>
        /// <returns>The name of the adapter.</returns>
        public override string ToString()
        {
            return this.name;
        }
        /// <summary>
        /// The name of the network adapter.
        /// </summary>
        public string Name
        {
            get { return this.name; }
        }
        /// <summary>
        /// Current download speed in bytes per second.
        /// </summary>
        public long DownloadSpeed
        {
            get { return this.dlSpeed; }
        }
        /// <summary>
        /// Current upload speed in bytes per second.
        /// </summary>
        public long UploadSpeed
        {
            get { return this.ulSpeed; }
        }
        /// <summary>
        /// Current download speed in kbytes per second.
        /// </summary>
        public double DownloadSpeedKbps
        {
            get { return this.dlSpeed / 1024.0; }
        }
        /// <summary>
        /// Current upload speed in kbytes per second.
        /// </summary>
        public double UploadSpeedKbps
        {
            get { return this.ulSpeed / 1024.0; }
        }
    }

    /// <summary>
    /// The NetworkMonitor class monitors network speed for each network adapter on the computer,
    /// using classes for Performance counter in .NET library.
    /// </summary>
    public class NetworkMonitor
    {
        private System.Timers.Timer timer;                // The timer event executes every second to refresh the values in adapters.
        private ArrayList adapters;            // The list of adapters on the computer.
        private ArrayList monitoredAdapters;// The list of currently monitored adapters.

        // 事件对象
        private event EventHandler<ChangeEvent> OnChangeEvent;

        public NetworkMonitor()
        {
            this.adapters = new ArrayList();
            this.monitoredAdapters = new ArrayList();
            EnumerateNetworkAdapters();

            timer = new System.Timers.Timer(1000);
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
        }
        /// <summary>
        /// Enumerates network adapters installed on the computer.
        /// </summary>
        private void EnumerateNetworkAdapters()
        {
            PerformanceCounterCategory category = new PerformanceCounterCategory("Network Interface");

            foreach (string name in category.GetInstanceNames())
            {
                // This one exists on every computer.
                if (name == "MS TCP Loopback interface")
                    continue;
                // Create an instance of NetworkAdapter class, and create performance counters for it.
                NetworkAdapter adapter = new NetworkAdapter(name);
                adapter.dlCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", name);
                adapter.ulCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", name);
                this.adapters.Add(adapter);    // Add it to ArrayList adapter
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (NetworkAdapter adapter in this.monitoredAdapters)
            {
                adapter.refresh();
            }
            if (OnChangeEvent != null)
            {
                ChangeEvent ad = new ChangeEvent();
                ad.adapters = this.monitoredAdapters;
                OnChangeEvent(this, ad);
            }
        }
        /// <summary>
        /// Get instances of NetworkAdapter for installed adapters on this computer.
        /// </summary>
        public NetworkAdapter[] Adapters
        {
            get { return (NetworkAdapter[])this.adapters.ToArray(typeof(NetworkAdapter)); }
        }
        /// <summary>
        /// Enable the timer and add all adapters to the monitoredAdapters list, 
        /// unless the adapters list is empty.
        /// </summary>
        public void StartMonitoring()
        {
            if (this.adapters.Count > 0)
            {
                foreach (NetworkAdapter adapter in this.adapters)
                    if (!this.monitoredAdapters.Contains(adapter))
                    {
                        this.monitoredAdapters.Add(adapter);
                        adapter.init();
                    }

                timer.Enabled = true;
            }
        }

        public void StartMonitoring(EventHandler<ChangeEvent> changeEvent)
        {
            this.OnChangeEvent += new EventHandler<ChangeEvent>(changeEvent);
            if (this.adapters.Count > 0)
            {
                foreach (NetworkAdapter adapter in this.adapters)
                    if (!this.monitoredAdapters.Contains(adapter))
                    {
                        this.monitoredAdapters.Add(adapter);
                        adapter.init();
                    }

                timer.Enabled = true;
            }
        }
        /// <summary>
        /// Enable the timer, and add the specified adapter to the monitoredAdapters list
        /// </summary>
        public void StartMonitoring(NetworkAdapter adapter)
        {
            if (!this.monitoredAdapters.Contains(adapter))
            {
                this.monitoredAdapters.Add(adapter);
                adapter.init();
            }
            timer.Enabled = true;
        }
        /// <summary>
        /// Disable the timer, and clear the monitoredAdapters list.
        /// </summary>
        public void StopMonitoring()
        {
            this.monitoredAdapters.Clear();
            timer.Enabled = false;
        }
        /// <summary>
        /// Remove the specified adapter from the monitoredAdapters list, and 
        /// disable the timer if the monitoredAdapters list is empty.
        /// </summary>
        public void StopMonitoring(NetworkAdapter adapter)
        {
            if (this.monitoredAdapters.Contains(adapter))
                this.monitoredAdapters.Remove(adapter);
            if (this.monitoredAdapters.Count == 0)
                timer.Enabled = false;
        }
    }

    public class ChangeEvent : EventArgs
    {
        public ArrayList adapters;
    }
}

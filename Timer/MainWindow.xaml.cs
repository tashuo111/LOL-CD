
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Timer.Services;
using System.Windows.Interop;
using Timer;
using System.Speech.Synthesis;

namespace Timer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 对位记录时间
        /// </summary>
        long TopStartTime, JugStartTime, MidStartTime, BotStartTime, SupStartTime, GameStartTime;
        /// <summary>
        /// 对位触发器
        /// </summary>
        DispatcherTimer TopTimer, JugTimer, MidTimer, BotTimer, SupTimer, GameTimer;
        /// <summary>
        /// 加减步长 固定五秒
        /// </summary>
        private const int stepLength = 5000;
        /// <summary>
        /// 复位步长 固定一千秒
        /// </summary>
        private const int clearLength = 1000000;
        /// <summary>
        /// 弹窗对象
        /// </summary>
        FlowWindow flowWindow;
        /// <summary>
        /// 钩子对象
        /// </summary>
        private HotkeyService hotkeys;
        /// <summary>
        /// ini解析对象
        /// </summary>
        
        /// <summary>
        /// ini数据对象
        /// </summary>
        
        /// <summary>
        /// 从ini文件读出来的对应Key值
        /// </summary>
        
        /// <summary>
        /// 委托对象，开关弹窗用途
        /// </summary>
        System.EventHandler delegateInstance;

        VoiceService voice = new VoiceService();
        /// <summary>
        /// 编辑模式的左偏移
        /// </summary>
        const double leftOffset = 3;
        /// <summary>
        /// 编辑模式的上偏移
        /// </summary>
        const double topOffset = 26;
        public MainWindow() : this(new SettingsService()) { }
        public MainWindow(SettingsService settingsService)
        {
            this.settingsService = settingsService;
            InitializeComponent();
            #region 多开判断
            //获取当前活动进程的模块名称
            string moduleName = Process.GetCurrentProcess().MainModule.ModuleName;
            //返回指定路径字符串的文件名
            string processName = System.IO.Path.GetFileNameWithoutExtension(moduleName);
            //根据文件名创建进程资源数组
            Process[] processes = Process.GetProcessesByName(processName);
            //如果该数组长度大于1，说明多次运行
            if (processes.Length > 1)
            {
                MessageBox.Show("不允许多开", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();//关闭当前窗体
                return;
            }
            #endregion
            settings = settingsService.Load();
            chkVoice.IsChecked = settings.VoiceEnabled;
            gameSend = new GameSendService(gameWindows, new WindowsGameInput(), () => settings);
            delegateInstance = new System.EventHandler(WindowEditFun);
            Loaded += (_, _) => {
                try { hotkeys ??= new HotkeyService(new WindowInteropHelper(this).Handle); } catch (Exception error) { ShowStatus(error.Message, 30); return; }
                RegisterHotkeys();
                if (settingsService.LoadWarning != null) ShowStatus(settingsService.LoadWarning);
            };
            flowWindow = new FlowWindow();
            Switch(null, null);//按一下弹窗复选框按钮
            GameButton_Click(new object(), new RoutedEventArgs());//按一下游戏开始按钮
            InitializeMode();
            voice.Failed += message => Dispatcher.BeginInvoke(new Action(() => ShowStatus(message, 15)));
        }
        /// <summary>
        /// 钩子回调函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Switch(object sender, EventArgs e)
        {
            if (flowWindow.IsVisible)
            {
                WindowEdit.Visibility = Visibility.Hidden;
                flowWindow.Hide();
            }
            else
            {
                if (!flowWindow.IsLoaded)
                {
                    flowWindow.Close();
                    flowWindow = new FlowWindow() { Top = settings.OverlayTop, Left = settings.OverlayLeft };
                }
                WindowEdit.Visibility = Visibility.Visible;
                UpdateFlowLabels(); flowWindow.Show();
            }
            if (e == null) { chkSwitch.IsChecked = (bool)chkSwitch.IsChecked ? false : true; }
        }



        /*上单模板 开始*/
        private void TopButton_Click(object sender, RoutedEventArgs e)
        {
            if (TopTimer != null)
            {
                TopTimer.Stop();
            }
            TopTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            TopStartTime = Environment.TickCount64;
            recorded.Add("Top");
            RefreshTeamText();
            TopTimer.Tick += Toptimer_Tick;
            TopTimer.IsEnabled = true;
        }

        private void Toptimer_Tick(object sender, EventArgs e)
        {
            bool isReady = true;
            TopLabel.Content = TimerUtil.ChangeTimeContent(TopStartTime, GameStartTime, CooldownFor(0, (bool)TopBoot.IsChecked, (bool)TopStar.IsChecked), out isReady);
            if (isReady)
            {
                if ((bool)chkVoice.IsChecked)
                {
                    voice.Speak(PositionName(0) + " 闪现已就绪");
                }
                TopTimer.Stop();
            }
            flowWindow.TopTime.Content = TopLabel.Content;
        }


        private void TopAdd_Click(object sender, RoutedEventArgs e)
        {
            TopStartTime += stepLength;
        }

        private void TopSubtract_Click(object sender, RoutedEventArgs e)
        {
            TopStartTime -= stepLength;
        }

        private void TopClear_Click(object sender, RoutedEventArgs e)
        {
            TopStartTime -= clearLength;
            recorded.Remove("Top");
            TopTimer?.Stop();
            TopLabel.Content = flowWindow.TopTime.Content = "就绪";
            RefreshTeamText();
        }

        /*上单模板 结束*/
        private void JugButton_Click(object sender, RoutedEventArgs e)
        {
            if (JugTimer != null)
            {
                JugTimer.Stop();
            }
            JugTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            JugTimer.Tick += Jugtimer_Tick;
            JugTimer.IsEnabled = true;
            JugStartTime = Environment.TickCount64;
            recorded.Add("Jug");
            RefreshTeamText();
        }

        private void Jugtimer_Tick(object sender, EventArgs e)
        {
            bool isReady = true;
            Jug.Content = TimerUtil.ChangeTimeContent(JugStartTime, GameStartTime, CooldownFor(1, (bool)JugBoot.IsChecked, (bool)JugStar.IsChecked), out isReady);
            if (isReady)
            {
                if ((bool)chkVoice.IsChecked)
                {
                    voice.Speak(PositionName(1) + " 闪现已就绪");
                }
                JugTimer.Stop();
            }
            flowWindow.JugTime.Content = Jug.Content;
        }

        private void JugAdd_Click(object sender, RoutedEventArgs e)
        {
            JugStartTime += stepLength;
        }

        private void JugSubtract_Click(object sender, RoutedEventArgs e)
        {
            JugStartTime -= stepLength;
        }

        private void JugClear_Click(object sender, RoutedEventArgs e)
        {
            JugStartTime -= clearLength;
            recorded.Remove("Jug");
            JugTimer?.Stop();
            Jug.Content = flowWindow.JugTime.Content = "就绪";
            RefreshTeamText();
        }


        private void MidButton_Click(object sender, RoutedEventArgs e)
        {
            if (MidTimer != null)
            {
                MidTimer.Stop();
            }
            MidTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            MidStartTime = Environment.TickCount64;
            recorded.Add("Mid");
            RefreshTeamText();
            MidTimer.Tick += Midtimer_Tick;
            MidTimer.IsEnabled = true;
        }

        private void Midtimer_Tick(object sender, EventArgs e)
        {
            bool isReady = true;
            Mid.Content = TimerUtil.ChangeTimeContent(MidStartTime, GameStartTime, CooldownFor(2, (bool)MidBoot.IsChecked, (bool)MidStar.IsChecked), out isReady);
            if (isReady)
            {
                if ((bool)chkVoice.IsChecked)
                {
                    voice.Speak(PositionName(2) + " 闪现已就绪");
                }
                MidTimer.Stop();
            }
            flowWindow.MidTime.Content = Mid.Content;
        }

        private void MidAdd_Click(object sender, RoutedEventArgs e)
        {
            MidStartTime += stepLength;
        }

        private void MidSubtract_Click(object sender, RoutedEventArgs e)
        {
            MidStartTime -= stepLength;
        }

        private void MidClear_Click(object sender, RoutedEventArgs e)
        {
            MidStartTime -= clearLength;
            recorded.Remove("Mid");
            MidTimer?.Stop();
            Mid.Content = flowWindow.MidTime.Content = "就绪";
            RefreshTeamText();
        }


        private void BotButton_Click(object sender, RoutedEventArgs e)
        {
            if (BotTimer != null)
            {
                BotTimer.Stop();
            }
            BotTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            BotStartTime = Environment.TickCount64;
            recorded.Add("Bot");
            RefreshTeamText();
            BotTimer.Tick += Bottimer_Tick;
            BotTimer.IsEnabled = true;
        }

        private void Bottimer_Tick(object sender, EventArgs e)
        {
            bool isReady = true;
            Bot.Content = TimerUtil.ChangeTimeContent(BotStartTime, GameStartTime, CooldownFor(3, (bool)BotBoot.IsChecked, (bool)BotStar.IsChecked), out isReady);
            if (isReady)
            {
                if ((bool)chkVoice.IsChecked)
                {
                    voice.Speak(PositionName(3) + " 闪现已就绪");
                }
                BotTimer.Stop();
            }
            flowWindow.BotTime.Content = Bot.Content;
        }

        private void BotAdd_Click(object sender, RoutedEventArgs e)
        {
            BotStartTime += stepLength;
        }

        private void BotSubtract_Click(object sender, RoutedEventArgs e)
        {
            BotStartTime -= stepLength;
        }

        private void BotClear_Click(object sender, RoutedEventArgs e)
        {
            BotStartTime -= clearLength;
            recorded.Remove("Bot");
            BotTimer?.Stop();
            Bot.Content = flowWindow.BotTime.Content = "就绪";
            RefreshTeamText();
        }

        private void SupButton_Click(object sender, RoutedEventArgs e)
        {
            if (SupTimer != null)
            {
                SupTimer.Stop();
            }
            SupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            SupStartTime = Environment.TickCount64;
            recorded.Add("Sup");
            RefreshTeamText();
            SupTimer.Tick += Suptimer_Tick;
            SupTimer.IsEnabled = true;
        }

        private void Suptimer_Tick(object sender, EventArgs e)
        {
            bool isReady = true;
            Sup.Content = TimerUtil.ChangeTimeContent(SupStartTime, GameStartTime, CooldownFor(4, (bool)SupBoot.IsChecked, (bool)SupStar.IsChecked), out isReady);
            if (isReady)
            {
                if ((bool)chkVoice.IsChecked)
                {
                    voice.Speak(PositionName(4) + " 闪现已就绪");
                }
                SupTimer.Stop();
            }
            flowWindow.SupTime.Content = Sup.Content;
        }

        private void SupAdd_Click(object sender, RoutedEventArgs e)
        {
            SupStartTime += stepLength;
        }

        private void SupSubtract_Click(object sender, RoutedEventArgs e)
        {
            SupStartTime -= stepLength;
        }

        private void SupClear_Click(object sender, RoutedEventArgs e)
        {
            SupStartTime -= clearLength;
            recorded.Remove("Sup");
            SupTimer?.Stop();
            Sup.Content = flowWindow.SupTime.Content = "就绪";
            RefreshTeamText();
        }

        private void GameButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameTimer != null)
            {
                GameTimer.Stop();
            }
            TopStartTime = JugStartTime = MidStartTime = BotStartTime = SupStartTime = 0;
            recorded.Clear();
            TopTimer?.Stop(); JugTimer?.Stop(); MidTimer?.Stop(); BotTimer?.Stop(); SupTimer?.Stop();
            TopLabel.Content = Jug.Content = Mid.Content = Bot.Content = Sup.Content = "就绪";
            flowWindow.TopTime.Content = flowWindow.JugTime.Content = flowWindow.MidTime.Content = flowWindow.BotTime.Content = flowWindow.SupTime.Content = "就绪";
            RefreshTeamText();
            GameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            GameStartTime = Environment.TickCount64;
            GameTimer.Tick += Gametimer_Tick;
            GameTimer.IsEnabled = true;
        }

        private void Gametimer_Tick(object sender, EventArgs e)
        {
            RefreshTeamText();
            long time = Math.Max(0, (Environment.TickCount64 - GameStartTime) / 1000);
            flowWindow.Game.Content = Game.Content = (time / 60).ToString() + ":" + (time % 60).ToString().PadLeft(2, '0');
        }

        private void GameAdd_Click(object sender, RoutedEventArgs e)
        {
            GameStartTime -= stepLength;
        }

        private void GameSubtract_Click(object sender, RoutedEventArgs e)
        {
            GameStartTime = Math.Min(Environment.TickCount64, GameStartTime + stepLength);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                flowWindow?.Close();
                if (TopTimer != null) { TopTimer.Stop(); }
                if (JugTimer != null) { JugTimer.Stop(); }
                if (MidTimer != null) { MidTimer.Stop(); }
                if (BotTimer != null) { BotTimer.Stop(); }
                if (SupTimer != null) { SupTimer.Stop(); }
                if (GameTimer != null) { GameTimer.Stop(); }
                hotkeys?.Dispose(); gameSend?.Dispose(); voice?.Dispose(); statusTimer.Stop();
            }
            catch (Exception e2)
            {
                MessageBox.Show(e2.Message, "error code:2");
            }
        }

        private void WindowEditFun(object sender, EventArgs e)
        {
            if (flowWindow.AllowsTransparency && e != EventArgs.Empty && flowWindow.IsLoaded)//编辑模式
            {
                flowWindow.Close();
                flowWindow = new FlowWindow() { AllowsTransparency = false, WindowStyle = WindowStyle.SingleBorderWindow, Top = settings.OverlayTop - topOffset, Left = settings.OverlayLeft - leftOffset };
                flowWindow.Closed += delegateInstance;
                UpdateFlowLabels(); flowWindow.Show();
            }
            else if (!flowWindow.AllowsTransparency)
            {
                settings.OverlayTop = flowWindow.Top + topOffset;
                settings.OverlayLeft = flowWindow.Left + leftOffset;
                SaveSettings();
                flowWindow.Closed -= delegateInstance;
                flowWindow.Close();
                flowWindow = new FlowWindow() { AllowsTransparency = true, WindowStyle = WindowStyle.None, Top = settings.OverlayTop, Left = settings.OverlayLeft };
                UpdateFlowLabels(); flowWindow.Show();
            }
        }
    }
}






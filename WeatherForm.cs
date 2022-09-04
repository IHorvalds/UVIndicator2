using System.Diagnostics;
using Microsoft.Win32;

namespace UVIndicator2
{
    public partial class WeatherForm : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        private readonly UVIndicatorSettings uvis = new();
        private readonly System.Windows.Forms.Timer timer = new();

        private static readonly string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private static readonly string StartupValue = "UVIndexApp";
        private static bool runsAtStartup = false;
        private readonly float scalingFactor = 1f;

        private readonly Dictionary<string, int> intervalOptions = new Dictionary<string, int>();

        private string lastRefreshedTime = "";

        public WeatherForm()
        {
            InitializeComponent();
            
            this.AutoScaleDimensions = new SizeF(96f, 96f); // default 100% scaling
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ResumeLayout(false);
            scalingFactor = CurrentAutoScaleDimensions.Width / 96f;



            intervalOptions.Add("one_hour", 60 * 60 * 1000);
            intervalOptions.Add("half_hour", 30 * 60 * 1000);

            string refresh_interval = uvis.RefreshInterval;
            if (refresh_interval != "manual")
            {
                timer.Interval = intervalOptions[refresh_interval];
            } else
            {
                timer.Interval = 24 * 60 * 60 * 1000; // 60m
                timer.Enabled = false;
            }
            timer.Tick += new EventHandler(Timer_Tick);

            foreach(ToolStripMenuItem item in refreshIntervalToolStripMenuItem.DropDownItems)
            {
                string tag = (string)item.Tag;
                if (refresh_interval == tag)
                {
                    item.Checked = true;
                }

                item.Click += SelectRefreshInterval;
            }

            
            
            runsAtStartup = CheckIfStartupEnabled();
            runAtStartupToolStripMenuItem.CheckState = runsAtStartup ? CheckState.Checked : CheckState.Unchecked;

            
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;


            
            // Position window in bottom-right of screen
            int screenPadding = 0;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width - screenPadding,
                                        Screen.PrimaryScreen.WorkingArea.Height - this.Height - screenPadding);
        }

        private async void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch(e.Reason)
            {
                case SessionSwitchReason.SessionLogoff:
                case SessionSwitchReason.SessionLock:
                    timer.Stop();
                break;

                case SessionSwitchReason.SessionLogon:
                case SessionSwitchReason.SessionUnlock:
                    ResetTimer();
                    await RefreshWeather();
                break;

            }
        }

        private async void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                ResetTimer();
                await RefreshWeather();
            }
            if (e.Mode == PowerModes.Suspend)
            {
                timer.Enabled = false;
            }
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            timer.Stop();
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            SystemEvents.SessionEnding -= SystemEvents_SessionEnding;
        }

        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.DisplayMainForm();
            }
        }

        private void WeatherForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                this.Hide();
            }
        }

        private void WeatherForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }
        }

        private async void LocationSearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                uvis.SavedLocation = locationSearchBox.Text.Trim();
                uvis.Save();
                ResetTimer();
                await RefreshWeather();
            }
        }

        private async void WeatherForm_Load(object sender, EventArgs e)
        {
            locationSearchBox.DataBindings.Add(new Binding("Text", uvis, "SavedLocation"));
            locationSearchBox.DeselectAll();
            locationSearchBox.ScrollToCaret();
            ResetTimer();
            await RefreshWeather();
        }

        private void WeatherForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                int lresult = SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                if (lresult != 0)
                {
                    // Error.
                    return;
                }
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DisplayMainForm();
        }
        
        private void RunAtStartupToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (runAtStartupToolStripMenuItem.Checked && !runsAtStartup)
            {
                // Add to startup
                SetStartup();
            } else if (!runAtStartupToolStripMenuItem.Checked && runsAtStartup)
            {
                // Remove from startup
                RemoveFromStartup();
            }
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            await RefreshWeather();
        }

        private void DisplayMainForm()
        {
            this.ShowInTaskbar = true;
            this.TopMost = true;
            this.WindowState = FormWindowState.Minimized;
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void RefreshButton_Click(object? sender, EventArgs e)
        {
            uvis.SavedLocation = locationSearchBox.Text;
            uvis.Save();

            ResetTimer();

            await RefreshWeather();
        }

        private async void SelectRefreshInterval(object sender, EventArgs e)
        {
            timer.Stop();
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            uvis.RefreshInterval = (string)menuItem.Tag;
            uvis.Save();
            
            for(int i = 0; i < refreshIntervalToolStripMenuItem.DropDownItems.Count; ++i)
            {
                ToolStripMenuItem item = (ToolStripMenuItem)refreshIntervalToolStripMenuItem.DropDownItems[i];
                if (item == menuItem)
                {
                    item.Checked = true;
                    string tag = (string)item.Tag;
                    if (tag != "manual")
                    {
                        timer.Interval = intervalOptions[tag];
                        timer.Start();
                        await RefreshWeather();
                    } else
                    {
                        timer.Interval = 24 * 60 * 60 * 1000;
                    }
                }
                else
                {
                    item.Checked = false;
                }
            }
        }

        private void ResetTimer()
        {
            if (uvis.RefreshInterval != "manual")
            {
                timer.Stop();
                timer.Start();
            }
        }

        private async Task RefreshWeather()
        {
            Debug.WriteLine("refreshed");
            string location = uvis.SavedLocation;
            lastRefreshedTime = DateTime.Now.ToString("HH:mm");
            if (location != "")
            {
                (string, string)? coord = await APIRequestsController.GetLocationCoordinates(location);
                
                if (coord != null)
                {

                    (double, double, double)? conditions = await APIRequestsController.GetWeatherFor(((string, string))coord);
                    
                    if (conditions != null)
                    {
                        double temp = (((double, double, double))conditions).Item1;
                        double uvi = (((double, double, double))conditions).Item2;
                        double hum = (((double, double, double))conditions).Item3;
                        UpdateUI(temp, uvi, hum);
                    }
                }
            }
        }

        private void UpdateUI(double temp, double uvi, double humidity)
        {
            temperatureLabel.Text = $"{temp}°C";
            humidityLabel.Text = $"Humidity: {humidity}%";
            uvIndexLabel.Text = $"UV: {uvi}";

            lastRefreshToolTip.SetToolTip(refreshButton, $"Last refreshed at {lastRefreshedTime}");

            int approxUVI = (int)Math.Round(uvi);

            // Cleanup old icon handle

            Bitmap bmp = UVBitmapGenerator.GenerateNumberIcon(approxUVI, scalingFactor);
            Bitmap squareCanvas = (Bitmap)bmp.GetThumbnailImage((int)128, (int)128, null, new IntPtr());


            IntPtr iconHandle = squareCanvas.GetHicon();
            Icon iconResult = Icon.FromHandle(iconHandle);

            notifyIcon.Icon = (Icon)iconResult.Clone();
            notifyIcon.Text = $"Last refreshed at {lastRefreshedTime}";
            DestroyIcon(iconHandle);
        }

        private static bool CheckIfStartupEnabled()
        {
            RegistryKey? key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
            if (key != null)
            {
                return key.GetValueNames().Contains(StartupValue);
            }
            return false;
        }

        private static void SetStartup()
        {
            //Set the application to run at startup
            RegistryKey? key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
            if (key != null)
            {
                key.SetValue(StartupValue, Application.ExecutablePath, RegistryValueKind.String);
                runsAtStartup = true;
            }
        }

        private static void RemoveFromStartup()
        {
            RegistryKey? key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
            if (key != null)
            {
                key.DeleteValue(StartupValue);
            }
            runsAtStartup = false;
        }
    }
}
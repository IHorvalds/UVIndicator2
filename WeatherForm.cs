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

        public WeatherForm()
        {
            InitializeComponent();
            locationSearchBox.AutoSize = false;
            locationSearchBox.Size = new System.Drawing.Size(locationSearchBox.Width, 28);
            locationSearchBox.TextAlign = HorizontalAlignment.Left;

            // Position window in bottom-right of screen
            int screenPadding = 10;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width - screenPadding,
                                        Screen.PrimaryScreen.WorkingArea.Height - this.Height - screenPadding);

            timer.Interval = 60 * 60 * 1000; // 60m
            timer.Tick += new EventHandler(Timer_Tick);

            runsAtStartup = CheckIfStartupEnabled();

            runAtStartupToolStripMenuItem.CheckState = runsAtStartup ? CheckState.Checked : CheckState.Unchecked;
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

        private void WeatherForm_VisibleChanged(object sender, EventArgs e)
        {
            this.notifyIcon.Visible = !this.Visible;
        }

        private void WeatherForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private async void LocationSearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                uvis.SavedLocation = locationSearchBox.Text;
                uvis.Save();
                ResetTimer();
                await RefreshWeather();
            }
        }

        private async void WeatherForm_Load(object sender, EventArgs e)
        {
            locationSearchBox.DataBindings.Add(new Binding("Text", uvis, "SavedLocation"));
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

        private void ResetTimer()
        {
            // Does this make sense???
            timer.Enabled = false;
            timer.Enabled = true;
        }

        private async Task RefreshWeather()
        {
            Debug.WriteLine("refreshed");
            string location = locationSearchBox.Text.Trim();
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

            int approxUVI = (int)Math.Round(uvi);

            // Cleanup old icon handle

            Bitmap bmp = UVBitmapGenerator.GenerateNumberIcon(approxUVI);
            Bitmap squareCanvas = (Bitmap)bmp.GetThumbnailImage((int)128, (int)128, null, new IntPtr());


            IntPtr iconHandle = squareCanvas.GetHicon();
            Icon iconResult = Icon.FromHandle(iconHandle);

            notifyIcon.Icon = (Icon)iconResult.Clone();
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
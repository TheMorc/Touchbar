using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System;
using System.Linq;
using System.Management;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Touchbar
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Aeroify

        private IntPtr hwnd;
        private HwndSource hsource;

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            try
            {
                if ((hwnd = new WindowInteropHelper(this).Handle) == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Could not get window handle for the main window.");
                }

                hsource = HwndSource.FromHwnd(hwnd);

                AdjustWindowFrame();
            }
            catch (InvalidOperationException)
            {
                FallbackPaint();
            }
        }

        private void AdjustWindowFrame()
        {
            if (DwmApiInterop.IsCompositionEnabled())
            {
                ExtendFrameIntoClientArea(4, 4, (int)this.Height - 40, 15);
            }
            else
            {
                FallbackPaint();
            }
        }

        private void ExtendFrameIntoClientArea(int left, int right, int top, int bottom)
        {
            var settings = new DwmBlurbehind { dwFlags = DwmApiInterop.DWM_BB_ENABLE, fEnable = true, fTransitionOnMaximized = false, hRgnBlur = null };
            int hresult = DwmApiInterop.ExtendFrameIntoClientArea(hwnd, ref settings);

            if (hresult == 0)
            {
                hsource.CompositionTarget.BackgroundColor = Colors.Transparent;
                Background = Brushes.Transparent;
            }
            else
            {
                throw new InvalidOperationException("Could not extend window frames in the main window.");
            }
        }

        private void FallbackPaint()
        {
            Background = Brushes.White;
        }

        #endregion

        #region definitions and functions
        //definitions
        //brightness
        public ManagementObjectSearcher brightness = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorBrightness");
        private int tempBrightness = 0;
        private int brightnessMaximum = 0;
        BitmapImage brightnessIcon = new BitmapImage(new Uri("pack://application:,,,/Touchbar;component/brightness.ico"));
        //volume
        public CoreAudioController Controller { get; } = new CoreAudioController();
        public CoreAudioDevice audioDevice
        {
            get
            {
                return Controller
                    .GetPlaybackDevices(DeviceState.Active)
                    .FirstOrDefault(o => o.IsDefaultDevice);
            }
        }
        private int tempVolume = 0;
        BitmapImage mutedAudio = new BitmapImage(new Uri("pack://application:,,,/Touchbar;component/0.ico"));
        BitmapImage noAudio = new BitmapImage(new Uri("pack://application:,,,/Touchbar;component/1.ico"));
        BitmapImage minAudio = new BitmapImage(new Uri("pack://application:,,,/Touchbar;component/2.ico"));
        BitmapImage medAudio = new BitmapImage(new Uri("pack://application:,,,/Touchbar;component/3.ico"));
        BitmapImage maxAudio = new BitmapImage(new Uri("pack://application:,,,/Touchbar;component/4.ico"));
        //other
        Timer detectionTimer = new Timer(100);
        Timer minimizeTimer = new Timer(2000);
        private static bool isRun = false;
        private static readonly object syncLock = new object();
        private SolidColorBrush green = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF01D328");

        //functions
        private void detect(object source, ElapsedEventArgs e)
        {
            //muted audio detection, icon and progressbar change
            if (audioDevice.IsMuted)
            {
                lock (syncLock)
                {
                    if (!isRun)
                    {
                        this.Dispatcher.Invoke(new Action(() => barLevel.Maximum = 100));
                        this.Dispatcher.Invoke(new Action(() => barLevel.Value = (int)audioDevice.Volume));
                        this.Dispatcher.Invoke(new Action(() => icon.Source = mutedAudio));
                        this.Dispatcher.Invoke(new Action(() => barLevel.Foreground = Brushes.Red));
                        show();
                        isRun = true;
                    }
                }
            }
            else
            {
                if (isRun)
                {
                    isRun = false;
                    this.Dispatcher.Invoke(new Action(() => barLevel.Maximum = 100));
                    this.Dispatcher.Invoke(new Action(() => barLevel.Value = (int)audioDevice.Volume));
                    this.Dispatcher.Invoke(new Action(() => barLevel.Foreground = green));
                    show();
                    setVolIcon();
                }
            }

            //volume detection
            if (!(tempVolume == (int)audioDevice.Volume))
            {
                this.Dispatcher.Invoke(new Action(() => barLevel.Maximum = 100));
                this.Dispatcher.Invoke(new Action(() => barLevel.Value = (int)audioDevice.Volume));
                this.Dispatcher.Invoke(new Action(() => barLevel.Foreground = green));
                show();
                setVolIcon();
            }

            //brightness detection
            try
            {
                foreach (ManagementObject queryObj in brightness.Get())
                {
                    if (!(tempBrightness == Int32.Parse(queryObj["CurrentBrightness"].ToString())))
                    {
                        this.Dispatcher.Invoke(new Action(() => barLevel.Maximum = brightnessMaximum));
                        this.Dispatcher.Invoke(new Action(() => barLevel.Foreground = green));
                        this.Dispatcher.Invoke(new Action(() => barLevel.Value = Int32.Parse(queryObj["CurrentBrightness"].ToString())));
                        this.Dispatcher.Invoke(new Action(() => icon.Source = brightnessIcon));
                        show();
                    }
                    tempBrightness = Int32.Parse(queryObj["CurrentBrightness"].ToString());
                }
            }
            catch (ManagementException ex)
            {
                MessageBox.Show("An error occurred while querying for WMI data: " + ex.Message);
            }
            tempVolume = (int)audioDevice.Volume;
        }

        private void show()
        {
            this.Dispatcher.Invoke(new Action(() => this.Show()));
            minimizeTimer.Enabled = true;
        }

        private void minimize(object source, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() => this.Hide()));
            minimizeTimer.Enabled = false;
        }

        //volume icon changer
        private void setVolIcon()
        {
            this.Dispatcher.Invoke(new Action(() => barLevel.Foreground = green));
            if ((int)audioDevice.Volume >= 66)
            {
                this.Dispatcher.Invoke(new Action(() => icon.Source = maxAudio));
            }
            else if ((int)audioDevice.Volume >= 33)
            {
                this.Dispatcher.Invoke(new Action(() => icon.Source = medAudio));
            }
            else if ((int)audioDevice.Volume >= 1)
            {
                this.Dispatcher.Invoke(new Action(() => icon.Source = minAudio));
            }
            else if ((int)audioDevice.Volume == 0)
            {
                this.Dispatcher.Invoke(new Action(() => icon.Source = noAudio));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //brightness max level detection
            try
            {
                foreach (ManagementObject queryObj in brightness.Get())
                {
                    brightnessMaximum = Int32.Parse(queryObj["Levels"].ToString());
                }
            }
            catch (ManagementException ex)
            {
                MessageBox.Show("An error occurred while querying for WMI data: " + ex.Message);
            }
            //enabling detection timer
            detectionTimer.Elapsed += new ElapsedEventHandler(detect);
            detectionTimer.Enabled = true;
            minimizeTimer.Elapsed += new ElapsedEventHandler(minimize);
            minimizeTimer.Enabled = true;
        }
        #endregion
    }
}
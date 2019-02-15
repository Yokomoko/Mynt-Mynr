using Mynt_Mynr.Business_Logic;
using Mynt_Mynr.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Mynt_Mynr
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        #region Public Fields

        public DialogResult Result;

        #endregion Public Fields

        #region Private Fields

        private readonly BackgroundWorker _amdBg = new BackgroundWorker();
        private readonly BackgroundWorker _cpuBg = new BackgroundWorker();
        private readonly BackgroundWorker _nVidiaBg = new BackgroundWorker();
        private bool _minerStarted;

        #endregion Private Fields

        #region Public Constructors

        public MainWindow()
        {
            InitializeComponent();
            UxIntensityPopupText.Text = "Select lower values if you still want to use your PC.\nRaise the intensity if you are idle. (GPU Only). Values above 20 may be unstable.\nSelect 'Auto' for the miner to auto-select the best intensity.";

            _cpuBg.WorkerSupportsCancellation = true;
            _nVidiaBg.WorkerSupportsCancellation = true;
            _amdBg.WorkerSupportsCancellation = true;
            _cpuBg.DoWork += OnCpuBgOnDoWork;
            _amdBg.DoWork += OnAmdBgOnDoWork;
            _nVidiaBg.DoWork += OnNVidiaBgOnDoWork;

            Height = 450;
            Width = 580;
            WindowState = WindowState.Normal;

            PopulatePage();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler CpuMinerClosed;

        #endregion Public Events

        #region Protected Methods

        protected virtual void OnCpuMinerClosed(EventArgs e)
        {
            if (CpuMinerClosed != null)
            {
                MiningOperations.CpuStarted = false;
            }
            if (MiningOperations.CpuStarted || MiningOperations.GpuStarted) return;
            if (_minerStarted)
            {
                BtnStart_OnClick(null, null);
            }
        }

        protected virtual void OnGpuMinerClosed(EventArgs e)
        {
            MiningOperations.GpuStarted = false;
            if (MiningOperations.CpuStarted || MiningOperations.GpuStarted) return;
            if (_minerStarted)
            {
                BtnStart_OnClick(null, null);
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void BtnStart_OnClick(object sender, RoutedEventArgs e)
        {
            _minerStarted = !_minerStarted;
            if (_minerStarted)
            {
                List<string> errors;
                if (!ValidateSettings(out errors))
                {
                    MessageBox.Show(this,
                        $"Unable to start miner, please rectify the following issues and try again:{Environment.NewLine + string.Join(Environment.NewLine, errors)} ");
                    return;
                }
                SaveSettings();

                BtnStart.Content = "Stop Mining";

                UxLogsExpander.Visibility = Visibility.Visible;

                UxCpuTgl.IsEnabled = false;
                uxIntervalSlider.IsEnabled = false;
                uxnVidiaRb.IsEnabled = false;
                uxnAMDRb.IsEnabled = false;
                TxtPool.IsEnabled = false;
                TxtUsername.IsEnabled = false;
                TxtPassword.IsEnabled = false;
                UxIntensityTxt.IsEnabled = false;
                uxPoolSelectorDdl.IsEnabled = false;
                GpuIntensityLbl.IsEnabled = false;
                UxIntensityHelp.Opacity = 0.56;
                ProgressBar.Visibility = Visibility.Visible;

                UxStandardSettings.IsExpanded = false;
                UxAdvancedSettings.IsExpanded = false;
                UxLogsExpander.IsExpanded = true;

                if (UxCpuTgl.IsChecked == true)
                {
                    _cpuBg.RunWorkerAsync();
                    uxCpuMiningLogGroup.Visibility = Visibility.Visible;
                }
                if (uxnAMDRb.IsChecked == true)
                {
                    _amdBg.RunWorkerAsync();
                    uxGpuMiningLog.Visibility = Visibility.Visible;
                }
                if (uxnVidiaRb.IsChecked == true)
                {
                    _nVidiaBg.RunWorkerAsync();
                    uxGpuMiningLog.Visibility = Visibility.Visible;
                }
                ProgressBar.IsIndeterminate = true;
            }
            else
            {
                BtnStart.Content = "Start Mining";

                KillProcesses();

                uxIntervalSlider.IsEnabled = true;
                UxCpuTgl.IsEnabled = true;
                uxnVidiaRb.IsEnabled = true;
                uxnAMDRb.IsEnabled = true;
                TxtPool.IsEnabled = true;
                TxtUsername.IsEnabled = true;
                TxtPassword.IsEnabled = true;
                UxIntensityTxt.IsEnabled = true;
                uxPoolSelectorDdl.IsEnabled = true;
                GpuIntensityLbl.IsEnabled = true;
                UxIntensityHelp.Opacity = 1;

                UxLogsExpander.IsExpanded = false;
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void KillProcesses()
        {
            var processes = Process.GetProcessesByName("minerd");
            foreach (var process in processes)
            {
                process.Kill();
            }
            processes = Process.GetProcessesByName("ccminer");
            foreach (var process in processes)
            {
                process.Kill();
            }
            processes = Process.GetProcessesByName("sgminer");
            foreach (var process in processes)
            {
                process.Kill();
            }
        }

        private void OnAmdBgOnDoWork(object sender, DoWorkEventArgs args)
        {
            if (_amdBg.CancellationPending)
            {
                args.Cancel = true;
                return;
            }
            #region Surrounding the Directory Path in Quotes
            var path = MiningOperations.AMDDirectory;
            var folderNames = path.Split('\\');

            folderNames = folderNames.Select(fn => (fn.Contains(' ')) ? $"\"{fn}\"" : fn)
                .ToArray();
            #endregion

            var fullPathWithQuotes = string.Join("\\", folderNames);

            using (var process = new Process())
            {
                var commands = MiningOperations.GetAMDCommandLine(MiningOperations.CommonMiningPoolVariables, MiningOperations.UseAutoIntensity, MiningOperations.MiningIntensity.ToString(), "x16");

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {fullPathWithQuotes} -g 4 -w 64 -k x16s {commands}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                process.StartInfo = info;
                process.EnableRaisingEvents = true;
                process.ErrorDataReceived += (o, eventArgs) =>
                {
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var lines = uxGpuLog.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                                .ToList();

                            if (lines.Count() == 30)
                            {
                                lines.RemoveAt(0);
                            }
                            lines.Add(eventArgs.Data);
                            uxGpuLog.Text = string.Join(Environment.NewLine, lines);

                            uxGpuScroller.ScrollToVerticalOffset(uxGpuScroller.ExtentHeight);
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error " + ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace);
                    }
                };


                process.Start();
                MiningOperations.GpuStarted = true;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                Dispatcher.Invoke(() => OnGpuMinerClosed(new EventArgs()));
            }
        }

        private void OnCpuBgOnDoWork(object sender, DoWorkEventArgs args)
        {
            if (_cpuBg.CancellationPending)
            {
                args.Cancel = true;
                return;
            }
            if (File.Exists(MiningOperations.CpuDirectory))
            {
                var commands = MiningOperations.GetCPUCommandLine(MiningOperations.CommonMiningPoolVariables);

                using (var process = new Process())
                {
                    ProcessStartInfo info = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/C " + "\"" + MiningOperations.CpuDirectory + "\"" + $" -a x16s {commands}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    process.StartInfo = info;
                    process.EnableRaisingEvents = true;
                    process.OutputDataReceived += (o, eventArgs) =>
                    {
                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var lines = uxCpuLog.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                                    .ToList();

                                if (lines.Count() == 30)
                                {
                                    lines.RemoveAt(0);
                                }
                                lines.Add(eventArgs.Data);
                                uxCpuLog.Text = string.Join(Environment.NewLine, lines);

                                uxCpuScroller.ScrollToVerticalOffset(uxGpuScroller.ExtentHeight);
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error " + ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace);
                        }
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    MiningOperations.CpuStarted = true;
                    process.WaitForExit();
                    Dispatcher.Invoke(() => OnCpuMinerClosed(new EventArgs()));
                }
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("minerd.exe file not found. Please check your antivirus settings, re-run the installer and select repair");
                    OnCpuMinerClosed(new EventArgs());
                });
            }
        }

        private void OnNVidiaBgOnDoWork(object sender, DoWorkEventArgs args)
        {
            if (_nVidiaBg.CancellationPending)
            {
                args.Cancel = true;
                return;
            }

            var commands = MiningOperations.GetNVidiaCommandLine(MiningOperations.CommonMiningPoolVariables, MiningOperations.UseAutoIntensity, MiningOperations.MiningIntensity.ToString());

            using (var process = new Process())
            {
                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/C " + "\"" + MiningOperations.NVididiaDirectory + "\"" + $" -a x16s {commands}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                process.StartInfo = info;

                if (Debugger.IsAttached)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(info.FileName + " " + info.Arguments);
                    });
                }

                process.EnableRaisingEvents = true;
                process.OutputDataReceived += (o, eventArgs) =>
                {
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var lines = uxGpuLog.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

                            if (lines.Count() == 30)
                            {
                                lines.RemoveAt(0);
                            }
                            lines.Add(eventArgs.Data);
                            uxGpuLog.Text = string.Join(Environment.NewLine, lines);

                            uxGpuScroller.ScrollToVerticalOffset(uxGpuScroller.ExtentHeight);
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error " + ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace);
                    }
                };
                process.Start();
                MiningOperations.GpuStarted = true;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                try
                {
                    Dispatcher.Invoke(() => OnGpuMinerClosed(new EventArgs()));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error " + ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace);
                }
            }
        }

        private void PopulatePage()
        {
            if (Debugger.IsAttached)
            {
                Settings.Default.P2PoolSettings = null;
                Settings.Default.CustomSettings = null;
                Settings.Default.MiningPoolHubSettings = null;
                Settings.Default.Pool2Settings = null;
            }


            uxIntervalSlider.Value = Settings.Default.MineIntensity;
            uxPoolSelectorDdl.SelectedIndex = Settings.Default.SelectedMiningPool;
            TxtPool.Text = MiningOperations.GetAddressForPool((MiningOperations.MiningPools)uxPoolSelectorDdl.SelectedIndex);
            TxtUsername.Text = Settings.Default.MiningPoolUsername;
            TxtPassword.Text = Settings.Default.MiningPoolPassword;
            uxAutoIntensityChk.IsChecked = Settings.Default.UseAutoIntensity;
            UxIntensityTxt.Text = Settings.Default.MineIntensity.ToString();
            UxCpuTgl.IsChecked = Settings.Default.CPUMining;
            uxnVidiaRb.IsChecked = (MiningOperations.GpuMiningSettings)Settings.Default.GPUMining == MiningOperations.GpuMiningSettings.NVidia;
            uxnAMDRb.IsChecked = (MiningOperations.GpuMiningSettings)Settings.Default.GPUMining == MiningOperations.GpuMiningSettings.Amd;


            if (uxAutoIntensityChk.IsChecked == true)
            {
                UxIntensityTxt.Visibility = Visibility.Collapsed;
                uxIntervalSlider.Visibility = Visibility.Collapsed;
            }
            else
            {
                UxIntensityTxt.Visibility = Visibility.Visible;
                uxIntervalSlider.Visibility = Visibility.Visible;
            }

            //if (Settings.Default.P2PoolSettings == null) {
            //    Settings.Default.P2PoolSettings = new StringCollection {
            //        MiningOperations.GetAddressForPool(MiningOperations.MiningPools.P2Pool),
            //        MiningOperations.GetUsernameForPool(MiningOperations.MiningPools.P2Pool),
            //        MiningOperations.GetPasswordForPool(MiningOperations.MiningPools.P2Pool)
            //        };
            //}
            if (Settings.Default.CustomSettings == null)
            {
                Settings.Default.CustomSettings = new StringCollection {
                    MiningOperations.GetAddressForPool(MiningOperations.MiningPools.Custom),
                    MiningOperations.GetUsernameForPool(MiningOperations.MiningPools.Custom),
                    MiningOperations.GetPasswordForPool(MiningOperations.MiningPools.Custom)
                 };
            }
            //if (Settings.Default.MiningPoolHubSettings == null) {
            //    Settings.Default.MiningPoolHubSettings = new StringCollection {
            //        MiningOperations.GetAddressForPool(MiningOperations.MiningPools.MiningPoolHub),
            //        MiningOperations.GetUsernameForPool(MiningOperations.MiningPools.MiningPoolHub),
            //        MiningOperations.GetPasswordForPool(MiningOperations.MiningPools.MiningPoolHub)
            //     };
            //}
            if (Settings.Default.Pool2Settings == null)
            {
                Settings.Default.Pool2Settings = new StringCollection {
                    MiningOperations.GetAddressForPool(MiningOperations.MiningPools.Mynt2),
                    MiningOperations.GetUsernameForPool(MiningOperations.MiningPools.Mynt2),
                    MiningOperations.GetPasswordForPool(MiningOperations.MiningPools.Mynt2)
                };
            }
            Settings.Default.Save();
        }

        private void RangeBase_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (UxIntensityTxt != null)
            {
                UxIntensityTxt.Text = e.NewValue.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void SaveSettings()
        {
            Settings.Default.GrsWalletAddress = TxtUsername.Text;
            Settings.Default.SelectedMiningPool = (byte)uxPoolSelectorDdl.SelectedIndex;

            switch ((MiningOperations.MiningPools)uxPoolSelectorDdl.SelectedIndex)
            {
                case MiningOperations.MiningPools.Mynt1:
                    Settings.Default.MiningPoolUsername = TxtUsername.Text;
                    Settings.Default.MiningPoolPassword = TxtPassword.Text;
                    break;
                //case MiningOperations.MiningPools.MiningPoolHub:
                //    Settings.Default.MiningPoolHubSettings[0] = TxtUsername.Text;
                //    Settings.Default.MiningPoolHubSettings[1] = TxtPassword.Text;
                //    break;
                case MiningOperations.MiningPools.Mynt2:
                    Settings.Default.Pool2Settings[0] = TxtUsername.Text;
                    Settings.Default.Pool2Settings[1] = TxtPassword.Text;
                    break;
                //case MiningOperations.MiningPools.P2Pool:
                //    Settings.Default.P2PoolSettings[0] = TxtPool.Text;
                //    Settings.Default.P2PoolSettings[1] = TxtUsername.Text;
                //    Settings.Default.P2PoolSettings[2] = TxtPassword.Text;
                //    break;
                case MiningOperations.MiningPools.Custom:
                    Settings.Default.CustomSettings[0] = TxtPool.Text;
                    Settings.Default.CustomSettings[1] = TxtUsername.Text;
                    Settings.Default.CustomSettings[2] = TxtPassword.Text;
                    break;
            }
            Settings.Default.MineIntensity = int.Parse(UxIntensityTxt.Text);
            Settings.Default.CPUMining = UxCpuTgl.IsChecked == true;
            Settings.Default.UseAutoIntensity = uxAutoIntensityChk.IsChecked == true;

            if (uxnVidiaRb.IsChecked == true)
            {
                Settings.Default.GPUMining = (byte)MiningOperations.GpuMiningSettings.NVidia;
            }
            else if (uxnAMDRb.IsChecked == true)
            {
                Settings.Default.GPUMining = (byte)MiningOperations.GpuMiningSettings.Amd;
            }
            else
            {
                Settings.Default.GPUMining = (byte)MiningOperations.GpuMiningSettings.None;
            }
            Settings.Default.Save();
        }

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this?.DragMove();
        }

        private void Rectangle_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this?.DragMove();
        }

        private void UxAdvancedSettings_OnExpanded(object sender, RoutedEventArgs e)
        {
            UxLogsExpander.IsExpanded = false;
            UxStandardSettings.IsExpanded = false;
        }

        private void UxAmdRb_OnChecked(object sender, RoutedEventArgs e)
        {
            uxnVidiaRb.Checked -= UxnVidiaRb_OnChecked;
            uxnVidiaRb.IsChecked = false;
            uxnVidiaRb.Checked += UxnVidiaRb_OnChecked;
        }

        private void UxnAMDRb_OnUnchecked(object sender, RoutedEventArgs e)
        {
        }

        private void UxnVidiaRb_OnChecked(object sender, RoutedEventArgs e)
        {
            uxnAMDRb.Checked -= UxAmdRb_OnChecked;
            uxnAMDRb.IsChecked = false;
            uxnAMDRb.Checked += UxAmdRb_OnChecked;
        }

        private void UxAutoIntensityChk_OnChecked(object sender, RoutedEventArgs e)
        {
            uxIntervalSlider.Visibility = Visibility.Collapsed;
            UxIntensityTxt.Visibility = Visibility.Collapsed;
        }

        private void UxAutoIntensityChk_OnUnchecked(object sender, RoutedEventArgs e)
        {
            UxIntensityTxt.Visibility = Visibility.Visible;
            uxIntervalSlider.Visibility = Visibility.Visible;
        }

        private void UxCpuLog_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Clipboard.SetText(uxCpuLog.Text);
                MessageBox.Show(this, "Copied to Clipboard");
            }
            catch
            {
            }
        }

        private void UxGetWalletAddressTxt_Click(object sender, RoutedEventArgs e)
        {
            if (!MiningOperations.WalletFileExists)
            {
                StartingGuide guide = new StartingGuide
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                };
                guide.FromMainWindow = true;
                guide.ShowDialog();
            }
            else
            {
                //if (TxtAddress.Text == MiningOperations.GetAddress()) return;
                var messageBoxResult = MessageBox.Show(this, "Warning: Resetting your mining address will reset your rewards. Are you sure?", "Address Warning", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    //TxtAddress.Text = MiningOperations.GetAddress();
                }
            }
        }

        private void UxGpuLog_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Clipboard.SetText(uxGpuLog.Text);
                MessageBox.Show(this, "Copied to Clipboard");
            }
            catch
            {
            }
        }

        private void UxIntensityHelp_OnMouseEnter(object sender, MouseEventArgs e)
        {
            UxIntensityPopup.IsOpen = true;
        }

        private void UxIntensityHelp_OnMouseLeave(object sender, MouseEventArgs e)
        {
            UxIntensityPopup.IsOpen = false;
        }

        private void UxIntensityTxt_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void UxLogsExpander_OnExpanded(object sender, RoutedEventArgs e)
        {
            UxAdvancedSettings.IsExpanded = false;
        }

        private void UxStandardSettings_OnExpanded(object sender, RoutedEventArgs e)
        {
            if (UxAdvancedSettings != null)
            {
                UxAdvancedSettings.IsExpanded = false;
            }
        }


        private bool ValidateSettings(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(TxtUsername.Text))
            {
                errors.Add("Please specify a username/address before starting to mine.");
            }
            if (string.IsNullOrEmpty(TxtPool.Text))
            {
                errors.Add("Please specify a mining pool address.");
            }
            if (string.IsNullOrEmpty(TxtUsername.Text))
            {
                errors.Add("Please specify a mining pool username.");
            }
            if (string.IsNullOrEmpty(TxtPassword.Text))
            {
                errors.Add("Please specify a mining pool password.");
            }
            if (uxnAMDRb.IsChecked == false && uxnVidiaRb.IsChecked == false && UxCpuTgl.IsChecked == false)
            {
                errors.Add("Please select what to mine with (CPU, AMD / nVidia)");
            }
            return !errors.Any();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            KillProcesses();
        }

        private void UxPoolSelectorDdl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TxtUsername != null && TxtUsername != null && TxtPassword != null)
            {
                TxtPool.Text = MiningOperations.GetAddressForPool((MiningOperations.MiningPools)uxPoolSelectorDdl.SelectedIndex);
                TxtUsername.Text = MiningOperations.GetUsernameForPool((MiningOperations.MiningPools)uxPoolSelectorDdl.SelectedIndex);
                TxtPassword.Text = MiningOperations.GetPasswordForPool((MiningOperations.MiningPools)uxPoolSelectorDdl.SelectedIndex);
            }
            SetStatsURL();
        }

        private void SetStatsURL()
        {

        }

        private void TxtPool_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            SetStatsURL();
        }

        #endregion Private Methods



    }
}
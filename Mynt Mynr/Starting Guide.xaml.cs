﻿using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using Mynt_Mynr.Business_Logic;
using Mynt_Mynr.Properties;

namespace Mynt_Mynr {
    /// <summary>
    /// Interaction logic for Starting_Guide.xaml
    /// </summary>
    public partial class StartingGuide {



        public bool FromMainWindow { get; set; }

        public byte Attempts = 0;

        public StartingGuide() {
            InitializeComponent();
        }



        private void Populate() {
            /*if (File.Exists(MiningOperations.WalletFolder)) {
                uxStepContent.Text =
                    "Groestlcoin EasyMiner has detected your receiving address from your default Electrum-GRS wallet. You can change this at any time if required." + Environment.NewLine + Environment.NewLine + "Receiving Address: " + MiningOperations.GetAddress();
                uxCheckInstallBtn.Visibility = Visibility.Collapsed;
                uxStepContent2.Visibility = Visibility.Collapsed;
                uxStepContent3.Visibility = Visibility.Collapsed;
            }
            else {
                uxStepContent2.Inlines.Clear();
                StringBuilder sb = new StringBuilder();
                sb.Append("Groestlcoin EasyMiner will automatically detect a receiving address from your Electrum-GRS Wallet.");
                sb.Append("You can download the Electrum-GRS Wallet from here:" + Environment.NewLine);
                uxStepContent.Text = sb.ToString();
                uxCheckInstallBtn.Visibility = Visibility.Visible;
                uxStepContent2.Visibility = Visibility.Visible;
                uxStepContent3.Visibility = Visibility.Visible;
                Hyperlink link = new Hyperlink {
                    NavigateUri = new Uri("https://www.groestlcoin.org/groestlcoin-electrum-wallet/"),
                    Foreground = Brushes.White
                };
                link.RequestNavigate += (sender, args) => {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(args.Uri.AbsoluteUri));
                    args.Handled = true;
                };
                link.Inlines.Add("Electrum GRS");
                uxStepContent2.Inlines.Add(link);
                uxStepContent3.Text =
                    "Install the Electrum-GRS Wallet. Once it is installed, click 'Check Electrum Install'. This should find your receiving address.";

                if (Attempts >= 5) {
                    System.Windows.MessageBox.Show(
                        "Electrum-GRS has not been found. Please ensure you have the latest Electrum-GRS wallet installed.");
                }
            }
            */

            //if (MiningOperations.HasNVidia) {
            //    uxHardwareTxt.Text = "Setup has detected that you are using an nVidia graphics card. This will be automatically set. If this is wrong, please change before starting to mine.";
            //}
            //else if (MiningOperations.HasAmd) {
            //  uxHardwareTxt.Text = "Setup has detected that you are using an AMD graphics card. This will be automatically set. If this is wrong, please change before starting to mine.";
            //}
           // else {
                uxHardwareTxt.Text = "Under 'Standard Settings' enter your Mynt wallet address and select your desired miner (Cpu, Nvidia or AMD). Next under 'Advanced Settings' enter your Mynt address and any password. Finally, select a pool and start mining.";
            //}


        }

        private void UxContinueBtn_OnClick(object sender, RoutedEventArgs e) {
            //if (this.Owner == null) {
                //if (Settings.Default.FirstLaunch) {
                    ShowMainWindow();
                //}
            //}
            Hide();
        }

        private void ShowMainWindow() {
            //Set defaults for GPUs
            //if (Settings.Default.FirstLaunch) {
            //    if (MiningOperations.HasNVidia) {
            //        Settings.Default.GPUMining = (byte)MiningOperations.GpuMiningSettings.NVidia;
            //    }
            //    else if (MiningOperations.HasAmd) {
            //        Settings.Default.GPUMining = (byte)MiningOperations.GpuMiningSettings.Amd;
            //    }
            //    else {
            //        Settings.Default.CPUMining = true;
            //        Settings.Default.GPUMining = (byte)MiningOperations.GpuMiningSettings.None;
            //    }
            //    //Settings.Default.GrsWalletAddress = MiningOperations.GetAddress();
            //    Settings.Default.FirstLaunch = false;
                Settings.Default.Save();
            //}
            MainWindow main = new MainWindow { WindowStartupLocation = WindowStartupLocation.CenterScreen };
            main.Show();
        }

        private void UxCheckInstallBtn_OnClick(object sender, RoutedEventArgs e) {
            Attempts++;
            Populate();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            //if (!Settings.Default.FirstLaunch && this.Owner == null) {
            //    ShowMainWindow();
            //    this.Close();
            //}
            Attempts = 0;
            Populate();
        }
    }
}

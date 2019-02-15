using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using Newtonsoft.Json;
using Mynt_Mynr.Properties;

namespace Mynt_Mynr.Business_Logic {
    class MiningOperations {
        public enum GpuMiningSettings {
            None = 0,
            NVidia = 1,
            Amd = 2
        }

        public enum MiningPools {
            Mynt1 = 0,
            Mynt2 = 1,
            //MiningPoolHub = 3,
            //P2Pool = 4,
            Custom = 2
        }

        public static string WalletFolder
            => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Electrum-grs\wallets\default_wallet";

        public static bool WalletFileExists => File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Electrum-grs\wallets\default_wallet");

        public static string ExecutingDirectory => Directory.GetCurrentDirectory();

        public static string CpuDirectory => Path.Combine(ExecutingDirectory, @"Resources\Miners\CPU Miner\minerd.exe");
        public static string AMDDirectory => Path.Combine(ExecutingDirectory, @"Resources\Miners\AMD Miner\sgminer.exe");
        public static string NVididiaDirectory => Path.Combine(ExecutingDirectory, @"Resources\Miners\nVidia Miner\ccminer.exe");

        public static List<string> GpuModels {
            get {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                return (from ManagementBaseObject mo in searcher.Get() select mo.Properties["Description"].ToString()).ToList();
            }
        }

        public static bool HasNVidia {
            get {
                bool hasGpu;
                try {
                    hasGpu = GpuModels.Any(d => d.ToLower().Contains("nvidia") || d.ToLower().Contains("quadro") || d.Contains("NVIDIA"));
                }
                catch {
                    hasGpu = false;
                }
                return hasGpu;
            }
        }

        public static bool HasAmd {
            get {
                bool hasGpu;
                try {
                    hasGpu = GpuModels.Any(d => d.ToLower().Contains("amd") || d.ToLower().Contains("firepro") || d.Contains("AMD"));
                }
                catch {
                    hasGpu = false;
                }
                return hasGpu;
            }
        }


        public static bool CpuStarted { get; set; } = false;
        public static bool GpuStarted { get; set; } = false;


        public static bool UseDwarfPool => Settings.Default.UseDwarfPool;
        public static string WalletAddress => Settings.Default.GrsWalletAddress;
        public static bool UseAutoIntensity => Settings.Default.UseAutoIntensity;
        public static int MiningIntensity => Settings.Default.MineIntensity;

        public static string MiningPoolAddress {
            get {
                switch (SelectedMiningPool) {
                    case MiningPools.Mynt1:
                        return GetAddressForPool(MiningPools.Mynt1);
                    case MiningPools.Mynt2:
                        return GetAddressForPool(MiningPools.Mynt2);
                    //case MiningPools.MiningPoolHub:
                    //    return GetAddressForPool(MiningPools.MiningPoolHub);
                    //case MiningPools.P2Pool:
                    //    return GetAddressForPool(MiningPools.P2Pool);
                    case MiningPools.Custom:
                        return GetAddressForPool(MiningPools.Custom);
                }
                return "";
            }
        }

        public static string MiningPoolUsername {
            get {
                switch (SelectedMiningPool) {
                    case MiningPools.Mynt1:
                        return GetUsernameForPool(MiningPools.Mynt1);
                    case MiningPools.Mynt2:
                        return GetUsernameForPool(MiningPools.Mynt2);
                    //case MiningPools.MiningPoolHub:
                    //    return GetUsernameForPool(MiningPools.MiningPoolHub);
                    //case MiningPools.P2Pool:
                    //    return GetUsernameForPool(MiningPools.P2Pool);
                    case MiningPools.Custom:
                        return GetUsernameForPool(MiningPools.Custom);
                }
                return "";
            }
        }

        public static string MiningPoolPassword {
            get {
                switch (SelectedMiningPool) {
                    case MiningPools.Mynt1:
                        return GetPasswordForPool(MiningPools.Mynt1);
                    case MiningPools.Mynt2:
                        return GetPasswordForPool(MiningPools.Mynt2);
                    case MiningPools.Custom:
                        return GetPasswordForPool(MiningPools.Custom);
                }
                return "";
            }
        }

        public static MiningPools SelectedMiningPool => (MiningPools)Settings.Default.SelectedMiningPool;

        /// <summary>
        /// Common mining poo variables used in all miners
        /// </summary>
        public static PublicMiningArgs CommonMiningPoolVariables => new PublicMiningArgs(MiningPoolAddress, MiningPoolUsername, MiningPoolPassword);


        public static string GetAddressForPool(MiningPools pool) {
            switch (pool) {
                case MiningPools.Mynt1:
                    return "pool.myntcurrency.org:3032";
                case MiningPools.Mynt2:
                    return "pool2.myntcurrency.org:3032";
                //case MiningPools.MiningPoolHub:
                //    return Settings.Default.MiningPoolHubSettings == null ? "hub.miningpoolhub.com:12004" : Settings.Default.MiningPoolHubSettings[0];
                //case MiningPools.P2Pool:
                //    return Settings.Default.P2PoolSettings == null ? "" : Settings.Default.P2PoolSettings[0];
                case MiningPools.Custom:
                    return Settings.Default.CustomSettings == null ? "stratum+tcp://" : Settings.Default.CustomSettings[0];
                default:
                    return "";
            }
        }

        public static string GetUsernameForPool(MiningPools pool) {
            switch (pool) {
                case MiningPools.Mynt1:
                case MiningPools.Mynt2:
                    return WalletAddress;
                //case MiningPools.MiningPoolHub:
                //    return WalletAddress;
                //case MiningPools.P2Pool:
                //    return WalletAddress;
                case MiningPools.Custom:
                    return WalletAddress;
                default:
                    return WalletAddress;

            }
        }

        public static string GetPasswordForPool(MiningPools pool) {
            switch (pool) {
                case MiningPools.Mynt1:
                    //return "x";
                    return Settings.Default.Pool2Settings == null ? "x" : Settings.Default.Pool2Settings[2];
                case MiningPools.Mynt2:
                    return Settings.Default.Pool2Settings == null ? "x" : Settings.Default.Pool2Settings[2];
                //case MiningPools.MiningPoolHub:
                //    return Settings.Default.MiningPoolHubSettings == null ? "x" : Settings.Default.MiningPoolHubSettings[2];
                //    //return "x";
                //case MiningPools.P2Pool:
                //    return Settings.Default.P2PoolSettings == null ? "x" : Settings.Default.P2PoolSettings[2];
                case MiningPools.Custom:
                    return Settings.Default.CustomSettings == null ? "x" : Settings.Default.CustomSettings[2];
                default:
                    return "x";
            }
        }



      public static string GetCPUCommandLine(PublicMiningArgs arguments) {
            return $"{arguments}";
        }

        public static string GetNVidiaCommandLine(PublicMiningArgs arguments, bool useAutoIntensity, string intensity) {
            var sb = new StringBuilder();
            //if (SelectedMiningPool == MiningPools.P2Pool) {
            //    sb.Append("--submit-stale");
            //}
            var intensityArgs = useAutoIntensity ? string.Empty : $" -i {intensity}";
            sb.Append(intensityArgs);
            sb.Append(arguments);

            return sb.ToString();
        }

        public static string GetAMDCommandLine(PublicMiningArgs arguments, bool useAutoIntensity, string intensity, string kernal = "") {
            var sb = new StringBuilder();
            //sb.Append(SelectedMiningPool != MiningPools.P2Pool ? " --no-submit-stale" : "");

            var intensityInt = int.Parse(intensity);

            var intensityArgs = useAutoIntensity ? string.Empty : $" -X {intensityInt * 64} ";

            sb.Append(arguments);
            sb.Append(intensityArgs);
            sb.Append("--text-only ");

            if (!string.IsNullOrEmpty(kernal)) {
                sb.Append($" --kernelfile {kernal}");
            }

            return sb.ToString();
        }


        /*public static string GetAddress() {
            try {
                //Get the pubkey
                var pubkey = String.Empty;
                //If electrum default wallet exists, read the file. 
                if (File.Exists(WalletFolder)) {
                    using (StreamReader r = new StreamReader(WalletFolder)) {
                        string json = r.ReadToEnd();
                        //Deserialize the json string to a dynamic array.
                        dynamic array = JsonConvert.DeserializeObject(json);
                        foreach (var item in array) {
                            //Deserialise the inner json string to get the receiving addresses
                            dynamic line = JsonConvert.DeserializeObject(item.Value.ToString());
                            foreach (var item2 in line) {
                                //Get the first address and break from loop
                                pubkey = item2.Value.receiving.First;
                                break;
                            }
                            break;
                        }
                    }
                }
                //If it didn't manage to get any public key, give up..
                if (String.IsNullOrEmpty(pubkey)) return String.Empty;

                //Get coin util directory
                var coinUtilLocation = $@"{Directory.GetCurrentDirectory()}\Resources\coin-util.exe";

                //if CoinUtil file doesn't exist (AV?) then give up..
                if (!File.Exists(coinUtilLocation)) return String.Empty;

                //Fire up coin util to get the public key. Return the output.
                using (var process = new Process()) {
                    ProcessStartInfo info = new ProcessStartInfo {
                        FileName = @"cmd.exe",
                        Arguments = $@"/C " + "\"" + coinUtilLocation + "\"" + $" -a GRS pubkey-to-addr {pubkey}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };
                    process.StartInfo = info;
                    process.EnableRaisingEvents = true;
                    process.Start();
                    var address = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return address;
                }
            }
            catch {
                return string.Empty;
            }
        }*/


        public class PublicMiningArgs {
            public string PoolAddress;
            public string PoolUsername;
            public string PoolPassword;

            public PublicMiningArgs(string PoolAddress, string PoolUsername, string PoolPassword) {
                this.PoolAddress = PoolAddress;
                this.PoolUsername = PoolUsername;
                this.PoolPassword = PoolPassword;
            }

            public override string ToString() {
                return $" -o stratum+tcp://{PoolAddress.ToLower().Replace("stratum+tcp://", "").Trim()} -u {PoolUsername.Trim()} -p {PoolPassword.Trim()} ";
            }
        }

    }
}

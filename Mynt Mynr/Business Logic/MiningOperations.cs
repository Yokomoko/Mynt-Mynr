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
            LetsHashIt = 0,
            MeCrypto = 1,
            //MiningPoolHub = 3,
            //P2Pool = 4,
            Custom = 2
        }

        public static string WalletFolder
            => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Electrum-grs\wallets\default_wallet";

        public static bool WalletFileExists => File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Electrum-grs\wallets\default_wallet");

        public static string ExecutingDirectory => Directory.GetCurrentDirectory();

        public static string CpuDirectory => Path.Combine(ExecutingDirectory, @"Resources\Miners\CPU Miner\minerd.exe");
        public static string AMDDirectory => Path.Combine(ExecutingDirectory, @"Resources\Miners\wildrig.exe");
        public static string NVididiaDirectory => Path.Combine(ExecutingDirectory, @"Resources\Miners\wildrig.exe");

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
                    case MiningPools.LetsHashIt:
                    case MiningPools.MeCrypto:
                        return GetAddressForPool(SelectedMiningPool);
                   case MiningPools.Custom:
                        return GetAddressForPool(MiningPools.Custom);
                }
                return "";
            }
        }

        public static string MiningPoolUsername {
            get {
                switch (SelectedMiningPool) {
                    case MiningPools.LetsHashIt:
                        return GetUsernameForPool(MiningPools.LetsHashIt);
                    case MiningPools.MeCrypto:
                        return GetUsernameForPool(MiningPools.MeCrypto);
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
                    case MiningPools.LetsHashIt:
                        return GetPasswordForPool(MiningPools.LetsHashIt);
                    case MiningPools.MeCrypto:
                        return GetPasswordForPool(MiningPools.MeCrypto);
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
                case MiningPools.LetsHashIt:
                    return "eu.letshash.it:3663";
                case MiningPools.MeCrypto:
                    return "eu.mecrypto.club:4874";
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
                case MiningPools.LetsHashIt:
                case MiningPools.MeCrypto:
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
                case MiningPools.LetsHashIt:
                    //return "x";
                    return Settings.Default.Pool2Settings == null ? "x" : Settings.Default.Pool2Settings[2];
                case MiningPools.MeCrypto:
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

        public static string GetGpuCommandLine(PublicMiningArgs arguments, bool useAutoIntensity, string intensity) {
            var sb = new StringBuilder();
            sb.Append(arguments.WildRigArguments());
            return sb.ToString();
        }

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

            public string WildRigArguments() {
                return $"--print-full -a x16s -o stratum+tcp://{PoolAddress.ToLower().Replace("stratum+tcp://", "").Trim()} -u {PoolUsername.Trim()} -p c=MYNT";
            }
        }

    }
}

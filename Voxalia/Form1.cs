//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of the MIT license.
// See README.md or LICENSE.txt for contents of the MIT license.
// If these are not available, see https://opensource.org/licenses/MIT
//

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Web;
using System.Threading;

namespace VoxaliaLauncher
{
    public partial class Form1 : Form
    {
        public static Encoding encoding = new UTF8Encoding(false);

        public Form1()
        {
            try
            {
                Microsoft.Win32.Registry.SetValue("HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION", "VoxaliaLauncher.exe", 11001, Microsoft.Win32.RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update registry: " + ex.ToString());
            }
            InitializeComponent();
            UpdateLoginDataFromFile();
            webBrowser1.Url = new Uri("https://github.com/FreneticXYZ/Voxalia/blob/master/README.md#voxalia");
        }

        public string UserName = null;

        public void UpdateLoginDataFromFile()
        {
            UserName = null;
            if (File.Exists("logindata.dat"))
            {
                try
                {
                    UserName = File.ReadAllText("logindata.dat").Split('=')[0].Replace('\n', ' ').Replace('\r', ' ');
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Internal exception reading logindata!" + Environment.NewLine + ex.ToString(), "Error");
                }
            }
            FixButtons();
        }

        public void FixButtons()
        {
            if (UserName == null)
            {
                loggedAs.Text = "Logged out";
                playButton.Enabled = false;
            }
            else
            {
                loggedAs.Text = "Logged in as: " + UserName;
                playButton.Enabled = true;
            }
        }

        private void changeLogin_Click(object sender, EventArgs e)
        {
            GlobalLoginAttempt(usernameBox.Text, passwordBox.Text, tfaBox.Text);
        }

        public bool Trying = false;

        public const string GlobalServerAddress = "https://frenetic.xyz/";

        public void GlobalLoginAttempt(string user, string pass, string tfa)
        {
            if (Trying)
            {
                MessageBox.Show("Already attempting a login...", "Error");
                return;
            }
            Trying = true;
            changeLogin.Enabled = false;
            progressBar1.Enabled = true;
            progressBar1.Style = ProgressBarStyle.Marquee;
            Task.Factory.StartNew(() =>
            {
                using (ShortWebClient wb = new ShortWebClient())
                {
                    try
                    {
                        NameValueCollection data = new NameValueCollection();
                        data["formtype"] = "login";
                        data["username"] = user;
                        data["password"] = pass;
                        data["tfa_code"] = tfa;
                        data["session_id"] = "0";
                        byte[] response = wb.UploadValues(GlobalServerAddress + "account/micrologin", "POST", data);
                        string resp = encoding.GetString(response).Trim(' ', '\n', '\r', '\t');
                        if (resp.StartsWith("ACCEPT=") && resp.EndsWith(";"))
                        {
                            string key = resp.Substring("ACCEPT=".Length, resp.Length - 1 - "ACCEPT=".Length);
                            Invoke(new Action(() =>
                            {
                                changeLogin.Enabled = true;
                                progressBar1.Enabled = false;
                                progressBar1.Style = ProgressBarStyle.Blocks;
                                Trying = false;
                                File.WriteAllText("logindata.dat", user + "=" + key);
                                UserName = user;
                                FixButtons();
                            }));
                        }
                        else
                        {
                            Invoke(new Action(() =>
                            {
                                changeLogin.Enabled = true;
                                progressBar1.Enabled = false;
                                progressBar1.Style = ProgressBarStyle.Blocks;
                                Trying = false;
                                MessageBox.Show("Login refused: " + resp);
                                Logout();
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Invoke(new Action(() =>
                        {
                            changeLogin.Enabled = true;
                            progressBar1.Enabled = false;
                            progressBar1.Style = ProgressBarStyle.Blocks;
                            Trying = false;
                            MessageBox.Show("Login failed: " + ex.ToString());
                            Logout();
                        }));
                    }
                }
            });
        }

        private void playButton_Click(object sender, EventArgs e)
        {
            Process.Start("Voxalia.exe");
            Thread.Sleep(1000);
            Close();
        }

        private void logoutButton_Click(object sender, EventArgs e)
        {
            Logout();
        }

        public void Logout()
        {
            if (File.Exists("logindata.dat"))
            {
                File.Delete("logindata.dat");
            }
            UserName = null;
            FixButtons();
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            webBrowser1.Document.Body.Style = "zoom:75%";
        }
    }

    class ShortWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 30 * 1000;
            return w;
        }
    }
}

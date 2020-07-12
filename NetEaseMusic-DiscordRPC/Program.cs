using System;
using System.Windows.Forms;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Drawing;
using System.Windows.Forms.VisualStyles;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

namespace NetEaseMusic_DiscordRPC
{
    class RPAsset
    {
        public string id;
        public string name;
        public string type;
    }

    class AlbumAsset
    {
        public string id;
        public string name;
        public long date;
        public string hash;
    }

    static class info
    {
        public static string ApplicationId   = "";
        public static string DefaultPresenceImageKey = "";
        public static string DiscordUserToken = "";
        public static string AlbumAssetPrefix = "a";
    }

    static class global
    {
        public static DiscordRpc.EventHandlers events = new DiscordRpc.EventHandlers();
        public static WebClient musicidClient = new WebClient();
        public static WebClient musicinfoClient = new WebClient();

        public static WebClient discordClient = new WebClient();
        public static HttpClient httpClient = new HttpClient();
        public static DiscordRpc.RichPresence presence = null;

        public static Dictionary<string, bool> deletedAssets = new Dictionary<string, bool>();
        public static bool noToken = false;
    }

    static class player
    {
        public static string currentPlaying = null;
        public static string albumHash = "";
        public static string albumName = "Unknown Album";
        public static string albumKey = null;
        public static string albumArtUrl = "";
        public static string musicId = "";
        public static bool loadingApi = false;
        public static bool requireUpdate = false;
        public static long startPlaying = 0;
        public static long endPlaying = 0;
    }

    static class tray
    {
        public static ContextMenu notifyMenu;
        public static NotifyIcon notifyIcon;
        public static MenuItem exitButton;
        public static MenuItem neteaseStatusDisplay;
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // check run once
            Mutex self = new Mutex(true, "NetEase Cloud Music DiscordRPC", out bool allow);
            if (!allow)
            {
                MessageBox.Show("NetEase Cloud Music DiscordRPC is already running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }

            // Check Rpc Dll
            if (!System.IO.File.Exists(Application.StartupPath + "\\discord-rpc.dll"))
            {
                MessageBox.Show("discord-rpc.dll does not exists!", "Error");
                if (MessageBox.Show("Do you want to download the missing files?", "Rpc Client", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    Environment.Exit(-1);
                }

                try
                {
                    using (WebClient web = new WebClient())
                    {
                        web.DownloadFile("http://build.kxnrl.com/_Raw/NetEaseMusicDiscordRpc/discord-rpc.dll", Application.StartupPath + "\\discord-rpc.dll");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to download discord-rpc.dll !", "Fatal Error");
                    Environment.Exit(-1);
                }
            }

            if (!System.IO.File.Exists(Application.StartupPath + "\\secret.txt"))
            {
                MessageBox.Show("secret.txt does not exist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }

            try
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(Application.StartupPath + "\\secret.txt", Encoding.UTF8))
                {
                    info.ApplicationId = sr.ReadLine().Trim();
                    info.DefaultPresenceImageKey = sr.ReadLine().Trim();
                    info.DiscordUserToken = sr.ReadLine().Trim();
                    if(!sr.ReadLine().Trim().Trim().Equals("true"))
                        // Hide window
                        Win32Api.User32.ShowWindow(Process.GetCurrentProcess().MainWindowHandle, Win32Api.User32.SW_HIDE);
                }
            }catch(Exception e)
            {
                MessageBox.Show("Failed to read secret.txt, refer to readme.md for help!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }

            if(info.ApplicationId.Length < 1 || info.DefaultPresenceImageKey.Length < 1)
            {
                MessageBox.Show("Failed to read secret.txt, refer to readme.md for help!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }

            // Auto Startup
            Win32Api.Registry.SetAutoStartup();

            // web client event
            global.musicinfoClient.DownloadStringCompleted += MusicInfoRequestCompleted;
            global.musicinfoClient.Encoding = Encoding.UTF8;
            global.musicidClient.DownloadStringCompleted += MusicIdRequestCompleted;
            global.musicidClient.Encoding = Encoding.UTF8;

            if (info.DiscordUserToken.Length > 1)
                // Setting up http client authorization
                global.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(info.DiscordUserToken);

            // Discord event
            global.events.readyCallback += DiscordRpc_Connected;

            // start new thread to update status.
            new Thread(
                delegate ()
                {
                    while (true)
                    {
                        //   明明对你念念不忘        思前想后愈发紧张
                        //                   Yukiim
                        //         想得不可得        是最难割舍的

                        UpdateStatus();
                    }
                }
            ).Start();

            tray.notifyMenu = new ContextMenu();
            tray.exitButton = new MenuItem("Exit");
            tray.neteaseStatusDisplay= new MenuItem("Starting...");
            tray.neteaseStatusDisplay.Enabled = false;
            tray.notifyMenu.MenuItems.Add(0, tray.neteaseStatusDisplay);
            tray.notifyMenu.MenuItems.Add(1, tray.exitButton);


            tray.notifyIcon = new NotifyIcon()
            {
                BalloonTipIcon = ToolTipIcon.Info,
                ContextMenu = tray.notifyMenu,
                Text = "NetEase Cloud Music DiscordRPC",
                Icon = Properties.Resources.icon,
                Visible = true,
            };

            tray.exitButton.Click += new EventHandler(ApplicationHandler_TrayIcon);

            if (info.DiscordUserToken.Length < 1)
            {
                global.noToken = true;
                Console.WriteLine("Missing Token!");

                tray.notifyIcon.BalloonTipTitle = "NetEase Music DiscordRPC";
                tray.notifyIcon.BalloonTipText = "Discord token in secret.txt is missing. Uploading new album art is disabled.";
                tray.notifyIcon.ShowBalloonTip(7000);
            }
            else
            {
                // Show notification
                tray.notifyIcon.BalloonTipTitle = "NetEase Music DiscordRPC";
                tray.notifyIcon.BalloonTipText = "External Plugin Started!";
                tray.notifyIcon.ShowBalloonTip(5000);
            }

            // Run
            Application.Run();
        }

        private static void DiscordRpc_Connected(ref DiscordRpc.DiscordUser connectedUser)
        {
            Console.WriteLine("Discord Connected: " + Environment.NewLine + connectedUser.userId + Environment.NewLine + connectedUser.username + Environment.NewLine + connectedUser.avatar + Environment.NewLine + connectedUser.discriminator);
        }

        private static void MusicIdRequestCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            player.musicId = "";
            long idNum = 0;
            try
            {
                idNum = long.Parse(e.Result);
            }catch(Exception ex)
            {
                Console.WriteLine("No music id found for current song");
                player.albumName = "Unknown Album";
                player.endPlaying = -1;
                UpdatePresenceAdvancedInfo();
                player.loadingApi = false;
                return;
            }

            player.musicId += idNum;
            Console.WriteLine("Music ID: " + player.musicId);
            global.musicinfoClient.DownloadStringAsync(new Uri("https://api.imjad.cn/cloudmusic/?type=detail&id=" + player.musicId));
        }

        private static void MusicInfoRequestCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            player.endPlaying = -1;
            string lastAlbum = player.albumName;
            player.albumName = "Unknown Album";
            player.albumArtUrl = null;
            try
            {
                //Json parsing
                dynamic result = JsonConvert.DeserializeObject(e.Result);

                if (result.songs[0].dt != null)
                    player.endPlaying = (long)Math.Round((double)(result.songs[0].dt / 1000.0), MidpointRounding.AwayFromZero) + player.startPlaying;
                if(result.songs[0].al.name != null)
                    player.albumName = result.songs[0].al.name;
                if (result.songs[0].al.picUrl != null)
                    player.albumArtUrl = result.songs[0].al.picUrl + "?param=128y128";

                Console.WriteLine("Succesffuly retrieved song info from results from netease API");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing json from http request from netease API");
                Console.WriteLine("The error causing json: ");
                Console.WriteLine(e.Result);
            }

            UpdatePresenceAdvancedInfo();

            player.albumHash = Utility.CreateMD5(player.albumName).ToLower();
            Console.WriteLine("album hash of " + player.albumName + " is " + player.albumHash);
            player.albumHash = player.albumHash.Substring(0, 8);

            if (player.albumArtUrl != null && lastAlbum != player.albumName && !player.albumName.Equals("Unknown Album"))
            {
                ManageAssests();
            }

            player.loadingApi = false;
        }

        private static async void ManageAssests()
        {
            bool needUpload = true;
            string rawCurrentAssets;
            RPAsset[] allAssests;

            try
            {
                rawCurrentAssets = global.discordClient.DownloadString("https://discord.com/api/v6/oauth2/applications/" + info.ApplicationId + "/assets");
                allAssests = JsonConvert.DeserializeObject<RPAsset[]>(rawCurrentAssets);
            }catch(Exception e)
            {
                Console.WriteLine("Error parsing assets list");
                Console.WriteLine(e.StackTrace);
                return;
            }

            List<AlbumAsset> albumAssets = new List<AlbumAsset>();
            Dictionary<string, bool> cachedAssets = new Dictionary<string, bool>();

            foreach (RPAsset asset in allAssests)
            {
                if (!asset.name.StartsWith(info.AlbumAssetPrefix)) continue;

                //Skip assets that are deleted but not uploaded yet
                bool deleted = false;
                global.deletedAssets.TryGetValue(asset.id, out deleted);
                if (deleted) continue;


                string[] assetArray = asset.name.Split('_');

                //If the album art is already uploaded
                if (player.albumHash.Equals(assetArray[1]))
                {
                    Console.WriteLine("Found existing assest for current album: " + player.albumName + " with hash: " + player.albumHash);
                    Console.WriteLine("Art Key: " + (string)asset.name);

                    player.albumKey = (string)asset.name;
                    needUpload = false;

                    UpdatePresenceAdvancedInfo();

                    break;
                }

                bool duplicate = false;
                cachedAssets.TryGetValue(assetArray[1], out duplicate);
                if (duplicate)
                {
                    await Task.Run(() => DeleteAsset(asset.id));
                    Console.WriteLine("Deleting duplicated asset with hash: " + assetArray[1]);
                    continue;
                }
                
                cachedAssets.Add(assetArray[1], true);

                AlbumAsset albumAsset = new AlbumAsset();
                albumAsset.name = asset.name;
                albumAsset.id = asset.id;
                albumAsset.date = long.Parse(assetArray[2]);
                albumAsset.hash = assetArray[1];
                albumAssets.Add(albumAsset);
            }

            //delete the 3 oldests assests if there are more than 125 assests right now
            if(allAssests.Length > 125 && albumAssets.Count > 3)
            {
                albumAssets.Sort((x, y) => x.date.CompareTo(y.date));
                await Task.Run(()=>DeleteAsset(albumAssets[0].id));
                await Task.Run(()=>DeleteAsset(albumAssets[1].id));
                await Task.Run(()=>DeleteAsset(albumAssets[2].id));
            }


            if (needUpload)
                UploadArt(player.albumArtUrl, player.albumHash);
        }

        private static void FaultyToken()
        {
            Console.WriteLine("Faulty Token!");

            global.noToken = true;
            tray.notifyIcon.BalloonTipTitle = "NetEase Music DiscordRPC";
            tray.notifyIcon.BalloonTipText = "Discord token in secret.txt is faulty. Uploading new album art is disabled. Restart the program to retry.";
            tray.notifyIcon.ShowBalloonTip(8000);
        }

        private static async void DeleteAsset(string id)
        {
            if (global.noToken)
                return;    
            try
            {
                Console.WriteLine("Deleting " + id);
                var response = await global.httpClient.DeleteAsync("https://discord.com/api/v6/oauth2/applications/" + info.ApplicationId + "/assets/" + id);
                Console.WriteLine((int)response.StatusCode + " when deleting " + id);
                if (response.StatusCode == HttpStatusCode.Unauthorized) FaultyToken();
                if(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.NoContent)
                    global.deletedAssets.Add(id, true);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error deleting an asset");
                Console.WriteLine(e.StackTrace);
            }
        }

        private static async void UploadArt(string albumArtUrl, string albumNameHash)
        {
            if (global.noToken)
                return;

            string imageString = null;

            try
            {
                if (!Directory.Exists(Application.StartupPath + "\\temp"))
                {
                    Directory.CreateDirectory(Application.StartupPath + "\\temp");
                }
                global.discordClient.DownloadFile(albumArtUrl, Application.StartupPath + "\\temp\\albumcover.jpg");
                Console.WriteLine("Successfully downloaded new album art");

                imageString = Utility.ImageBase64(Application.StartupPath + "\\temp\\albumcover.jpg");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error downloading cover art");
                Console.WriteLine(e.StackTrace);
                return;
            }

            if (imageString == null)
                return;

            string albumKey = info.AlbumAssetPrefix + "_" + albumNameHash + "_" + (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/v6/oauth2/applications/" + info.ApplicationId + "/assets");

            try
            {
                request.Content = new StringContent("{\"name\":\"" + albumKey + "\", \"type\" : \"1\", \"image\": \"data:image/png;base64," + imageString + "\"}", Encoding.UTF8, "application/json");
                var response = await global.httpClient.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.Unauthorized) {
                    player.albumKey = info.DefaultPresenceImageKey;
                    FaultyToken();
                }
                Console.WriteLine("Uploaded new album art, response code " + (int)response.StatusCode);
            }catch(Exception e)
            {
                Console.WriteLine("Error uploading new album art");
                Console.WriteLine(e.StackTrace);
                return;
            }

            if (global.noToken) return;

            //Wait 10 second to apply the new albumkey, as the image takes time to be processed at discord's end
            Thread.Sleep(10000);
            if (!albumNameHash.Equals(player.albumHash)) return;
                player.albumKey = albumKey;
                UpdatePresenceAdvancedInfo();
        }

        private static void ApplicationHandler_TrayIcon(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            if (item == tray.exitButton)
            {
                ClearPresence();
                tray.notifyIcon.Visible = false;
                tray.notifyIcon.Dispose();
                Thread.Sleep(50);
                Environment.Exit(0);
            }
        }

        private static string currentPlaying = null;
        private static StringBuilder strbuilder = new StringBuilder(256);
        private static bool playerRunning = false;

        private static int updateCounter = 0;
        private static void UpdateStatus()
        {
            updateCounter++;

            if (updateCounter % 15 == 0) UpdatePresenceAdvancedInfo();
            // Block thread.
            Thread.Sleep(1000);

            // clear data
            strbuilder.Clear();

            // set flag
            playerRunning = false;

            // Check Player
            Win32Api.User32.EnumWindows
            (
                delegate (IntPtr hWnd, int lParam)
                {
                    Win32Api.User32.GetClassName(hWnd, strbuilder, 256);

                    if (strbuilder.ToString().Equals("OrpheusBrowserHost"))
                    {
                        // clear data
                        strbuilder.Clear();
                        int length = Win32Api.User32.GetWindowTextLength(hWnd);
                        Win32Api.User32.GetWindowText(hWnd, strbuilder, length + 1);
                        currentPlaying = strbuilder.ToString();
                        playerRunning = true;
                    }

                    return true;
                },
                IntPtr.Zero
            );

            // maybe application has been exited ?
            if (!playerRunning)
            {
                // fresh status.
                ClearPresence();
                Console.WriteLine("Player exited!");
                return;
            }

            // Has resultes?
            if (String.IsNullOrWhiteSpace(currentPlaying))
            {
                // fresh status.
                ClearPresence();
                Console.WriteLine("Fatal ERROR!");
                return;
            }

            // New song & the previous song is already loaded? else we'll try to load again the next second
            if (!currentPlaying.Equals(player.currentPlaying) && !player.loadingApi && !global.musicinfoClient.IsBusy)
            {
                player.currentPlaying = currentPlaying;
                player.startPlaying = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                player.albumKey = info.DefaultPresenceImageKey;
                player.albumName = "Loading Album...";

                Console.WriteLine("New song, resetting settings");

                player.loadingApi = true;
                global.musicidClient.DownloadStringAsync(new Uri("https://music.kxnrl.com/api/v3/?engine=netease&format=string&data=id&song=" + currentPlaying));

                player.requireUpdate = true;
            }

            // Runing full screen Application?
            if (Win32Api.User32.IsFullscreenAppRunning())
            {
                // fresh status.
                ClearPresence();
                player.requireUpdate = true;
                Console.WriteLine("Runing fullscreen Application.");
                return;
            }
                
            // Running whitelist Application?
            if (Win32Api.User32.IsWhitelistAppRunning())
            {
                // fresh status.
                ClearPresence();
                player.requireUpdate = true;
                Console.WriteLine("Running whitelist Application.");
                return;
            }

            // check discord
            CheckRpc();

            // Update?
            if (player.requireUpdate)
            {
                player.requireUpdate = false;

                // RPC data
                
                string[] text = Utility.ReplaceFirstOccurrence(currentPlaying, " - ", "\t").Split('\t');
                if (text.Length > 1)
                {
                    //Updates the status in taskbar, to help user debug
                    tray.neteaseStatusDisplay.Text = "Now Playing: " + text[0];
                    global.presence.details = text[0];
                    if (text[1].Length > 0)
                        global.presence.state = "by " + text[1]; // like spotify
                    else
                        global.presence.state = "by Unknown Artist"; // like spotify
                }
                else
                {
                    global.presence.details = currentPlaying;
                    global.presence.state = string.Empty;
                }

                UpdatePresenceAdvancedInfo();

                // logging
                Console.WriteLine("Updated Rich Presence");
            }
        }

        private static void UpdatePresenceAdvancedInfo()
        {
            if (global.presence == null)
            {
                // uninitialized
                return;
            }

            global.presence.largeImageKey = player.albumKey;
            global.presence.largeImageText = player.albumName;

            global.presence.startTimestamp = player.startPlaying;
            global.presence.endTimestamp = player.endPlaying;

            // Update status
            DiscordRpc.UpdatePresence(global.presence);

            Console.WriteLine("updated presence information");
        }

        private static void ClearPresence()
        {
            if (global.presence == null)
            {
                // uninitialized
                return;
            }

            global.presence.FreeMem();
            global.presence = null;

            DiscordRpc.ClearPresence();
            DiscordRpc.Shutdown();
        }

        private static void CheckRpc()
        {
            if (global.presence != null)
            {
                // Initialized
                return;
            }

            global.presence = new DiscordRpc.RichPresence();
            //global.events = new DiscordRpc.EventHandlers();

            // Discord Api initializing...
            DiscordRpc.Initialize(info.ApplicationId, ref global.events, false, null);
        }
    }
}

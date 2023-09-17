using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace BotMonitor
{
    class Program
    {
        static void Main(string[] args)
        => new Program().StartAsync().GetAwaiter().GetResult();
        System.Timers.Timer _timer = new System.Timers.Timer(50000) { Enabled = false };

        private DiscordSocketClient _client;
        public class SavedData
        {
            public List<ulong> NotifyUsers { get; set; }
            public List<Bot> Bots { get; set; }
        }
        public class Bot
        {
            public string startLocation { get; set; }
            public string processName { get; set; }
            public ulong BotID { get; set; }
            public int RestartAttempts { get; set; }
        }
        public SavedData _data = new SavedData();

        public async Task StartAsync()
        {
            _timer.Elapsed += _timer_Elapsed;
            await Log("Setting up the bot", ConsoleColor.Green);
            _client = new DiscordSocketClient();
            await Log("Logging in...", ConsoleColor.Green);
            await _client.LoginAsync(TokenType.Bot, "##########################################################");
            await Log("Connecting...", ConsoleColor.Green);
            GatherData();
            await _client.StartAsync();
            _client.GuildAvailable += _client_GuildAvailable;
            await Task.Delay(-1);
        }
        string dataFile = "MonitorData.json";

        private void GatherData()
        {
            if (File.Exists(dataFile))
            {
                _data = JsonConvert.DeserializeObject<SavedData>(File.ReadAllText(dataFile));
                Log("Saved data Loaded!", ConsoleColor.Green);
            }
            else
            {
                var example = new SavedData { Bots = new List<Bot>() { new Bot() { BotID = 1000000000001, processName = "Example.exe", RestartAttempts = 0, startLocation = @"c:\bots\test.exe" }, new Bot() { BotID = 2000000000002, processName = "Example2.exe", RestartAttempts = 0, startLocation = @"c:\bots\test2.exe" } }, NotifyUsers = new List<ulong>() { 1111111111 } };
                File.WriteAllText(dataFile, JsonConvert.SerializeObject(example, Formatting.Indented));

            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Log("Tick!", ConsoleColor.Cyan);
            Monitor();
            Log("Tack!", ConsoleColor.Cyan);

        }


        private void Monitor()
        {

            foreach (var bot in _data.Bots)
            {
                try
                {
                    Console.WriteLine(bot.processName);
                    Log("OFFLINE BOT FOUND!!!", ConsoleColor.Green);
                    //var bot = _data.Bots.Single(x => x.BotID == user.Id);
                    if (bot.RestartAttempts == 0)
                    {
                        NotifyUsers($"Bot {bot.processName} have gone offline! \n Trying to restart it now...");
                        RestartServer(bot.BotID);
                    }
                    else if (bot.RestartAttempts < 11)
                    {
                        NotifyUsers($"Bot {bot.processName} is still offline! \n Trying to restart it again for the {bot.RestartAttempts} time....");
                        RestartServer(bot.BotID);
                    }
                    else if (bot.RestartAttempts == 11)
                    {
                        NotifyUsers($"MonitorBot where not able to restart bot {bot.processName}, Check on the bot manually!");
                        bot.RestartAttempts++;
                        File.WriteAllText(dataFile, JsonConvert.SerializeObject(_data, Formatting.Indented));

                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
                
            }
            Environment.Exit(1);
            /*
            foreach (var server in _client.Guilds.Where(z=> z.Id == 435790619240038400))
            {
                foreach (var user in server.Users.Where(x => x.IsBot && x.Status == UserStatus.Offline))
                {
                    if (OwnBot(user))
                    {
                        Log("OFFLINE BOT FOUND!!!", ConsoleColor.Green);
                        var bot = _data.Bots.Single(x => x.BotID == user.Id);
                        if (bot.RestartAttempts == 0)
                        {
                            NotifyUsers(user, $"Bot {user.Username} have gone offline! \n Trying to restart it now...");
                            RestartServer(user.Id);
                        }
                        else if (bot.RestartAttempts < 11)
                        {
                            NotifyUsers(user, $"Bot {user.Username} is still offline! \n Trying to restart it again for the {bot.RestartAttempts} time....");
                            RestartServer(user.Id);
                        }
                        else if (bot.RestartAttempts == 11)
                        {
                            NotifyUsers(user, $"MonitorBot where not able to restart bot {user.Username}, Check on the bot manually!");
                            bot.RestartAttempts++;
                            File.WriteAllText(dataFile, JsonConvert.SerializeObject(_data, Formatting.Indented));

                        }
                    }
                }
                foreach (var user in server.Users.Where(x => x.IsBot && x.Status == UserStatus.Online))
                {
                    if (OwnBot(user))
                    {
                        var bot = _data.Bots.Single(x => x.BotID == user.Id);
                        bot.RestartAttempts = 0;
                    }
                }
            }
            foreach (var server in _client.Guilds.Where(x=> x.Id != 435790619240038400))
            {
                server.LeaveAsync();
            }
            */
        }

        private void NotifyUsers(string Text)
        {
            foreach (var userID in _data.NotifyUsers)
            {
                var nuser = _client.GetUser(userID);
                nuser.SendMessageAsync(Text);
            }

        }

        private void RestartServer(ulong id)
        {
            try
            {

                var bot = _data.Bots.Single(x => x.BotID == id);
                bot.RestartAttempts++;
                File.WriteAllText(dataFile, JsonConvert.SerializeObject(_data, Formatting.Indented));
                foreach (var _process in Process.GetProcessesByName(bot.processName))
                {
                    _process.Kill();
                }
                Thread.Sleep(100);
                var file = bot.startLocation;
                ProcessStartInfo info = new ProcessStartInfo();
                info.WorkingDirectory = file.Replace(file.Split(new string[] { "\\" }, StringSplitOptions.None).Last(), "");
                info.FileName = file;
                Process.Start(info);
            }
            catch (Exception)
            {
            }
        }

        private bool OwnBot(SocketGuildUser user)
        {
            return _data.Bots.Exists(x => x.BotID == user.Id);
        }
        private async Task _client_GuildAvailable(SocketGuild arg)
        {
            await Log(arg.Name + " Connected!", ConsoleColor.Green);
            _timer.Enabled = true;
        }
        public static async Task Log(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(DateTime.Now + " : " + message, color);
            Console.ResetColor();
        }



    }
}

using SysBot.Base;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Discord;
using System.Threading.Tasks;

namespace SysBot.Pokemon.WinForms
{
    public partial class BotController : UserControl
    {
        public PokeBotState State { get; private set; } = new();
        private PokeBotRunner? Runner;
        public EventHandler? Remove;

        public BotController()
        {
            InitializeComponent();
            var opt = (BotControlCommand[])Enum.GetValues(typeof(BotControlCommand));

            for (int i = 1; i < opt.Length; i++)
            {
                var cmd = opt[i];
                var item = new ToolStripMenuItem(cmd.ToString());
                item.Click += (_, __) => SendCommand(cmd);

                RCMenu.Items.Add(item);
            }

            var remove = new ToolStripMenuItem("Remove");
            remove.Click += (_, __) => TryRemove();
            RCMenu.Items.Add(remove);
            RCMenu.Opening += RcMenuOnOpening;

            var controls = Controls;
            foreach (var c in controls.OfType<Control>())
            {
                c.MouseEnter += BotController_MouseEnter;
                c.MouseLeave += BotController_MouseLeave;
            }
        }

        private void RcMenuOnOpening(object sender, CancelEventArgs e)
        {
            if (Runner == null)
                return;

            bool runOnce = Runner.RunOnce;
            var bot = Runner.GetBot(State);
            if (bot == null)
                return;

            foreach (var tsi in RCMenu.Items.OfType<ToolStripMenuItem>())
            {
                var text = tsi.Text;
                tsi.Enabled = Enum.TryParse(text, out BotControlCommand cmd)
                    ? runOnce && cmd.IsUsable(bot.IsRunning, bot.IsPaused)
                    : !bot.IsRunning;
            }
        }

        public void Initialize(PokeBotRunner runner, PokeBotState cfg)
        {
            Runner = runner;
            State = cfg;
            ReloadStatus();
            L_Description.Text = string.Empty;
        }

        public void ReloadStatus()
        {
            var bot = GetBot().Bot;
            L_Left.Text = $"{bot.Connection.Name}{Environment.NewLine}{State.InitialRoutine}";
        }

        private DateTime LastUpdateStatus = DateTime.Now;

        public async void ReloadStatus(BotSource<PokeBotState> b)
        {
            ReloadStatus();
            var bot = b.Bot;
            L_Description.Text = $"[{bot.LastTime:hh:mm:ss}] {bot.Connection.Label}: {bot.LastLogged}";
            L_Left.Text = $"{bot.Connection.Name}{Environment.NewLine}{State.InitialRoutine}";

            var lastTime = bot.LastTime;
            if (!b.IsRunning)
            {
                if (Discord.TradeModule.Hub.Config.TradeBot.channelchanger)
                {
                    var chanarray = Discord.TradeModule.Hub.Config.TradeBot.tradebotchannel.Split(',');
                    foreach (string c in chanarray)
                    {
                        ulong.TryParse(c, out var tchan);
                        var tradechan = (ITextChannel)Discord.SysCord._client.GetChannel(tchan);
                        if (tradechan.Name.Contains("✅"))
                        {
                            var role = tradechan.Guild.EveryoneRole;
                            await tradechan.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny));
                            await tradechan.ModifyAsync(prop => prop.Name = $"{Discord.TradeModule.Hub.Config.TradeBot.channelname}❌");
                            var offembed = new EmbedBuilder();
                            offembed.AddField($"{Discord.SysCord._client.CurrentUser.Username} Bot Announcement", "LGPE Trade Bot is Offline");
                            await tradechan.SendMessageAsync(embed: offembed.Build());
                        }
                        var cah = (ITextChannel)Discord.SysCord._client.GetChannelAsync(Discord.TradeModule.Hub.Config.Discord.wtpchannelid).Result;
                        if (cah.Name.Contains("✅"))
                        {
                            LetsGoTrades.wtpsource.Cancel();

                            await cah.SendMessageAsync("\"Who's That Pokemon\" mode stopped.");

                            await cah.ModifyAsync(newname => newname.Name = cah.Name.Replace("✅", "❌"));
                            await cah.AddPermissionOverwriteAsync(cah.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny));
                        }
                    }
                }


                PB_Lamp.BackColor = System.Drawing.Color.Transparent;
                return;
            }

            var cfg = bot.Config;
            if (cfg.CurrentRoutineType == PokeRoutineType.Idle && cfg.NextRoutineType == PokeRoutineType.Idle)
            {
                PB_Lamp.BackColor = System.Drawing.Color.Yellow;
                return;
            }
            if (LastUpdateStatus == lastTime)
                return;

            // Color decay from Green based on time
            const int threshold = 100;
            System.Drawing.Color good = System.Drawing.Color.Green;
            System.Drawing.Color bad = System.Drawing.Color.Red;

            var delta = DateTime.Now - lastTime;
            var seconds = delta.Seconds;

            LastUpdateStatus = lastTime;
            if (seconds > 2 * threshold)
                return; // already changed by now

            if (seconds > threshold)
            {
                if (PB_Lamp.BackColor == bad)
                    return; // should we notify on change instead?
                PB_Lamp.BackColor = bad;
            }
            else
            {
                // blend from green->red, favoring green until near saturation
                var factor = seconds / (double)threshold;
                var blend = Blend(bad, good, factor * factor);
                PB_Lamp.BackColor = blend;
            }
        }

        private static System.Drawing.Color Blend(System.Drawing.Color color, System.Drawing.Color backColor, double amount)
        {
            byte r = (byte)((color.R * amount) + (backColor.R * (1 - amount)));
            byte g = (byte)((color.G * amount) + (backColor.G * (1 - amount)));
            byte b = (byte)((color.B * amount) + (backColor.B * (1 - amount)));
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        public void TryRemove()
        {
            var bot = GetBot();
            bot.Stop();
            Remove?.Invoke(this, EventArgs.Empty);
        }

        public async void SendCommand(BotControlCommand cmd, bool echo = true)
        {


            var bot = GetBot();
            switch (cmd)
            {
                case BotControlCommand.Idle: bot.Pause(); break;
                case BotControlCommand.Start: bot.Start(); break;
                case BotControlCommand.Stop: bot.Stop(); break;
                case BotControlCommand.Resume: bot.Resume(); break;
                case BotControlCommand.Restart:
                    {
                        var prompt = WinFormsUtil.Prompt(MessageBoxButtons.YesNo, "Are you sure you want to restart the connection?");
                        if (prompt != DialogResult.Yes)
                            return;

                        bot.Bot.Connection.Reset();
                        bot.Start();
                        break;
                    }
                default:
                    WinFormsUtil.Alert($"{cmd} is not a command that can be sent to the Bot.");
                    return;
            }
            if (Discord.TradeModule.Hub.Config.TradeBot.channelchanger)
            {
                if (cmd == BotControlCommand.Start)
                {

                    if (bot.Bot.Config.NextRoutineType == PokeRoutineType.LGPETradeBot)
                    {
                        var chanarray = Discord.TradeModule.Hub.Config.TradeBot.tradebotchannel.Split(',');
                        foreach (string i in chanarray)
                        {
                            ulong.TryParse(i, out var tchan);
                            var tradechan = (ITextChannel)Discord.SysCord._client.GetChannel(tchan);
                            if (tradechan.Name.Contains("❌"))
                            {
                                var role = tradechan.Guild.EveryoneRole;
                                await tradechan.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Allow));
                                await tradechan.ModifyAsync(prop => prop.Name = $"{Discord.TradeModule.Hub.Config.TradeBot.channelname}✅");
                                var offembed = new EmbedBuilder();
                                offembed.AddField($"{Discord.SysCord._client.CurrentUser.Username} Bot Announcement", "LGPE Trade Bot is Online");
                                await tradechan.SendMessageAsync("<@&898901020678176839>",embed: offembed.Build());
                            }
                        }
                        if (Discord.TradeModule.Hub.Config.Discord.wtpbool)
                            Discord.WTPSB.WhoseThatPokemon();
                    }
                }
                if (cmd == BotControlCommand.Stop)
                {
                    var chanarray = Discord.TradeModule.Hub.Config.TradeBot.tradebotchannel.Split(',');
                    foreach (string b in chanarray)
                    {
                        ulong.TryParse(b, out var tchan);
                        var tradechan = (ITextChannel)Discord.SysCord._client.GetChannel(tchan);
                        if (tradechan.Name.Contains("✅"))
                        {
                            var role = tradechan.Guild.EveryoneRole;
                            await tradechan.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny));
                            await tradechan.ModifyAsync(prop => prop.Name = $"{Discord.TradeModule.Hub.Config.TradeBot.channelname}❌");
                            var offembed = new EmbedBuilder();
                            offembed.AddField($"{Discord.SysCord._client.CurrentUser.Username} Bot Announcement", "LGPE Trade Bot is Offline");
                            await tradechan.SendMessageAsync(embed: offembed.Build());
                        }
                    }
                }
            }
        }
        private BotSource<PokeBotState> GetBot()
        {
            if (Runner == null)
                throw new ArgumentNullException(nameof(Runner));

            var bot = Runner.GetBot(State);
            if (bot == null)
                throw new ArgumentNullException(nameof(bot));
            return bot;
        }

        private void BotController_MouseEnter(object? sender, EventArgs e) => BackColor = System.Drawing.Color.LightSkyBlue;
        private void BotController_MouseLeave(object? sender, EventArgs e) => BackColor = System.Drawing.Color.Transparent;

        public void ReadState()
        {
            var bot = GetBot();

            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() => ReloadStatus(bot)));
            }
            else
            {
                ReloadStatus(bot);
            }
        }
    }

    public enum BotControlCommand
    {
        None,
        Start,
        Stop,
        Idle,
        Resume,
        Restart,
    }

    public static class BotControlCommandExtensions
    {
        public static bool IsUsable(this BotControlCommand cmd, bool running, bool paused)
        {
            return cmd switch
            {
                BotControlCommand.Start => !running,
                BotControlCommand.Stop => running,
                BotControlCommand.Idle => running && !paused,
                BotControlCommand.Resume => paused,
                BotControlCommand.Restart => true,
                _ => false,
            };
        }
    }
}

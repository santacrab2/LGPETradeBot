using Discord.Commands;
using SysBot.Base;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
   public class ExtraModule : ModuleBase<SocketCommandContext> 
    {

        [Command("charge")]
        [Summary("tells you the charge")]
        [RequireOwner]
        public async Task GetChargeAsync([Remainder] string ip)
        {
            var source = new CancellationTokenSource();
            var token = source.Token;

            var bot = GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot found with the specified address ({ip}).").ConfigureAwait(false);
                return;
            }

            var c = bot.Bot.Connection;
            var chnumb = c.GetCharge(token).Result;
            await Context.Channel.SendMessageAsync(chnumb);
        }

        [Command("test")]
        [RequireOwner]
        public async Task test()
        {
            await ReplyAsync("test");
        }


        private static BotSource<PokeBotState>? GetBot(string ip)
        {
            var r = SysCordInstance.Runner;
            return r.GetBot(ip) ?? r.Bots.Find(x => x.IsRunning); // safe fallback for users who mistype IP address for single bot instances
        }
    }
}

using Discord.Commands;
using SysBot.Base;
using System;
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
            var ch = await GetCharge(ip).ConfigureAwait(false);
            await Context.Channel.SendMessageAsync(string.Format("{0:000}", ch));
        }

        [Command("test")]
        [RequireOwner]
        public async Task test()
        {
            await ReplyAsync("test");
        }

        public async Task<int> GetCharge(string ip)
        {
            var bot = GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                return -1;
            }
            var b = bot.Bot;
            var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
            var chnumb = await b.Connection.SendAsync(SwitchCommand.charge(crlf), CancellationToken.None).ConfigureAwait(false);
            return chnumb;
        }

        private static BotSource<PokeBotState>? GetBot(string ip)
        {
            var r = SysCordInstance.Runner;
            return r.GetBot(ip) ?? r.Bots.Find(x => x.IsRunning); // safe fallback for users who mistype IP address for single bot instances
        }
    }
}

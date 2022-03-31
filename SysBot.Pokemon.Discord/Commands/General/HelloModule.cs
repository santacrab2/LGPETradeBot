using Discord.Commands;
using Discord.Interactions;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class HelloModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("hello","say hello to the bot")]
        
        public async Task PingAsync()
        {
            var str = SysCordInstance.Settings.HelloResponse;
            var msg = string.Format(str, Context.User.Mention);
            await RespondAsync(msg).ConfigureAwait(false);
        }
    }
}
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using static PKHeX.Core.Species;
using System.Reflection;

namespace SysBot.Pokemon.Discord
{
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ViewChannel)]
    public class WTPSB : InteractionModuleBase<SocketInteractionContext>
    {
        
        public static bool buttonpressed = false;
        public static bool tradepokemon = false;
        public static PokeTradeHub<PK8> Hub = SysCordInstance.Self.Hub;
        public static GameVersion Game = GameVersion.GE;
        public static string guess = "";
        public static SocketUser usr;
        public static ushort randspecies;
        public static SocketInteractionContext con;
     
        public static async Task WhoseThatPokemon()
        {
            ITextChannel wtpchannel = (ITextChannel)SysCord._client.GetChannelAsync(Hub.Config.Discord.wtpchannelid).Result;
            await wtpchannel.ModifyAsync(newname => newname.Name = wtpchannel.Name.Replace("❌", "✅"));
            await wtpchannel.AddPermissionOverwriteAsync(wtpchannel.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Allow));
            await wtpchannel.SendMessageAsync("\"who's that pokemon\" mode started!");
            while (!LetsGoTrades.wtpsource.IsCancellationRequested)
            {
                Stopwatch sw = new();
                sw.Restart();
                Random random = new Random();
                var code = random.Next(99999999);
                var Dex = GetPokedex();
                randspecies = Dex[random.Next(Dex.Length)];
                EmbedBuilder embed = new EmbedBuilder();
                embed.Title = "Who's That Pokemon";
                embed.AddField(new EmbedFieldBuilder { Name = "instructions", Value = "Type /guess <pokemon name> to guess the name of the pokemon displayed and you get that pokemon in your actual game!" });
                if (randspecies < 891)
                    embed.ImageUrl = $"https://logoassetsgame.s3.us-east-2.amazonaws.com/wtp/pokemon/{randspecies}q.png";
                else
                    embed.ImageUrl = $"https://raw.githubusercontent.com/santacrab2/SysBot.NET/RNGstuff/finalimages/{randspecies}q.png";
                await wtpchannel.SendMessageAsync(embed: embed.Build());
                while (guess.ToLower() != ((Species)randspecies).ToString().ToLower() && sw.ElapsedMilliseconds / 1000 < 600)
                {
                    await Task.Delay(25);
                }
                var entry = File.ReadAllLines("DexFlavor.txt")[randspecies];
                embed = new EmbedBuilder().WithFooter(entry);
                embed.Title = $"It's {(Species)randspecies}";
                embed.AddField(new EmbedFieldBuilder { Name = "instructions", Value = $"Type /guess <pokemon name> to guess the name of the pokemon displayed and you get that pokemon in your actual game!" });
                if (randspecies < 891)
                    embed.ImageUrl = $"https://logoassetsgame.s3.us-east-2.amazonaws.com/wtp/pokemon/{randspecies}a.png";
                else
                    embed.ImageUrl = $"https://raw.githubusercontent.com/santacrab2/SysBot.NET/RNGstuff/finalimages/{randspecies}a.png";
                await wtpchannel.SendMessageAsync(embed: embed.Build());
              
                if (guess.ToLower() == ((Species)randspecies).ToString().ToLower())
                {
                    var compmessage = new ComponentBuilder().WithButton("Yes", "wtpyes",ButtonStyle.Success).WithButton("No", "wtpno", ButtonStyle.Danger);
                    var embedmes = new EmbedBuilder();
                    embedmes.AddField("Receive Pokemon?", $"Would you like to receive {(Species)randspecies} in your game?");
                    await wtpchannel.SendMessageAsync($"<@{usr.Id}>",embed: embedmes.Build(), components: compmessage.Build());

                    while (!buttonpressed)
                    {
                        await Task.Delay(25);
                    }
                    if (tradepokemon)
                    {
                        var set = new ShowdownSet($"{SpeciesName.GetSpeciesNameGeneration(randspecies,2,7)}\nShiny: Yes");
                        var template = new RegenTemplate(set);
                        var sav = SaveUtil.GetBlankSAV(GameVersion.GE,"Piplup");
                        var pk = sav.GetLegalFromSet(template, out var result);
                        pk = pk.Legalize();
                        if (!new LegalityAnalysis(pk).Valid)
                        {
                            set = new ShowdownSet(SpeciesName.GetSpeciesNameGeneration(randspecies, 2, 7));
                            template = new RegenTemplate(set);
                            sav = SaveUtil.GetBlankSAV(GameVersion.GE, "Piplup");
                            pk = sav.GetLegalFromSet(template, out result);
                            pk = pk.Legalize();
                        }
                        pk.Ball = BallApplicator.ApplyBallLegalByColor(pk);
                        ushort[] sugmov = MoveSetApplicator.GetMoveSet(pk, true);
                        pk.SetMoves(sugmov);
                        int natue = random.Next(24);
                        pk.Nature = natue;


                        var queueitem = new TheQobject { commandtype = LetsGoTrades.commandtype.trade, discordcontext = con, tradepkm = (PB7)pk };
                        LetsGoTrades.TheQ.Enqueue(queueitem);
                        await con.Interaction.ModifyOriginalResponseAsync(x => x.Content = $"{con.User.Username} - Added to the LGPE Link Trade Queue. Current Position: {LetsGoTrades.TheQ.Count}. Receiving: {(pk.IsShiny ? "Shiny" : "")} {(Species)pk.Species}{(pk.Form == 0 ? "" : "-" + ShowdownParsing.GetStringFromForm(pk.Form, GameInfo.Strings, pk.Species, pk.Context))}");
                    }
                    usr = null;
                    guess = "";
                    tradepokemon = false;
                    buttonpressed = false;
                }
                usr = null;
                guess = "";
            }
           LetsGoTrades.wtpsource = new();
        }
        [SlashCommand("guess","guess what the pokemon displayed is")]
       
        public async Task WTPguess([Summary("pokemon","put the pokemon name here")]string userguess)
        {
            await DeferAsync();
            if (LetsGoTrades.TheQ.Any(z => z.discordcontext.User == Context.User))
            {
                await FollowupAsync("please wait until you are out of queue to guess to avoid double queueing.");
                return;
            }
            if (userguess.ToLower() == ((Species)randspecies).ToString().ToLower())
            {
                await FollowupAsync($"{Context.User.Username} You are correct! It's {userguess}");
                guess = userguess;
                usr = Context.User;
                con = Context;
            }
            else
                await FollowupAsync($"{Context.User.Username} You are incorrect. It is not {userguess}");
        }
        [SlashCommand("wtpcancel","owner only")]
        [RequireOwner]
        public async Task wtpcancel()
        {
           LetsGoTrades.wtpsource.Cancel();
            await RespondAsync("\"Who's That Pokemon\" mode stopped.",ephemeral:true);
            ITextChannel wtpchannel = (ITextChannel)Context.Channel;
            await wtpchannel.ModifyAsync(newname => newname.Name = wtpchannel.Name.Replace("✅","❌"));
            await wtpchannel.AddPermissionOverwriteAsync(wtpchannel.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny));
        }

        public static ushort[] GetPokedex()
        {
            List<ushort> dex = new();
            for (ushort i = 1; i < (Game == GameVersion.BDSP ? 494 : Game == GameVersion.SWSH? 899:Game == GameVersion.GE? 152:906); i++)
            {
                
                if (!PersonalTable.GG.IsPresentInGame(i, 0))
                    continue;

                var species = SpeciesName.GetSpeciesNameGeneration(i, 2,Game == GameVersion.GE? 7: 8);
                var set = new ShowdownSet($"{species}{(i == (int)NidoranF ? "-F" : i == (int)NidoranM ? "-M" : "")}");
                var template = AutoLegalityWrapper.GetTemplate(set);
                var sav = SaveUtil.GetBlankSAV(GameVersion.GE, "Piplup");
                _ = (PB7)sav.GetLegal(template, out string result);

                if (result == "Regenerated")
                    dex.Add(i);
            }
            return dex.ToArray();
        }
        private async Task HandleMessageAsync(SocketMessage arg)
        {
            if (arg is not SocketUserMessage msg)
                return;
            guess = msg.ToString();
        }
    }
}

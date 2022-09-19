using System;
using System.Collections;
using System.Text;
using Discord.Interactions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using PKHeX.Drawing;

using System.Linq;
using System.IO;

namespace SysBot.Pokemon.Discord
{

    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ViewChannel)]
   public class TradeModule : InteractionModuleBase<SocketInteractionContext>
    {
        public static PokeTradeHub<PK8> Hub = SysCordInstance.Self.Hub;



        [SlashCommand("trade", "Trades You a pokemon from showdown text in Lets Go games")]

        public async Task Trade([Summary("PokemonText","put your copied showdown text here")]string ShowdownSet = "", Attachment Pb7 = default)
        {
            await DeferAsync();
            if (ShowdownSet != "")
            {
                if (LetsGoTrades.TheQ.Any(z=>z.discordcontext.User == Context.User))
                {
                    await FollowupAsync("you are already in queue",ephemeral:true);
                    return;
                }
                var correctchannelcheck = Hub.Config.TradeBot.tradebotchannel.Split(',');
                if (!correctchannelcheck.Contains(Context.Channel.Id.ToString()))
                {
                    await FollowupAsync("You can not use that command in this channel",ephemeral:true);

                    return;
                }
                
                var set = await ConvertToShowdown(ShowdownSet);
                RegenTemplate rset = new(set);
           

                try
                {
                    var trainer = TrainerSettings.GetSavedTrainerData(GameVersion.GE, 7);
                    var sav = SaveUtil.GetBlankSAV((GameVersion)trainer.Game, trainer.OT);
                    var pkm = sav.GetLegalFromSet(rset, out var res);
                    pkm = pkm.Legalize();
                    if (pkm.Species == 151)
                        pkm.SetAwakenedValues(set);
                    var la = new LegalityAnalysis(pkm);
                    var spec = GameInfo.Strings.Species[set.Species];
            


                    if (!la.Valid)
                    {
                        var reason = res == LegalizationResult.Timeout ? $"That {spec} set took too long to generate." : $"I wasn't able to create a {spec} from that set.";
                        var imsg = $"Oops! {reason}";
                        if (res == LegalizationResult.Failed || !la.Valid)
                            imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(set, sav, pkm)}";
                        await FollowupAsync(imsg,ephemeral:true).ConfigureAwait(false);
                        return;
                    }
                    if(pkm.PartyStatsPresent)
                        pkm.ResetPartyStats();
                    try { await Context.User.SendMessageAsync("I've added you to the queue! I'll message you here when your trade is starting."); }
                    catch { await FollowupAsync("Please enable direct messages from server members to be queued",ephemeral:true); return; };
                    var queueitem = new TheQobject { commandtype = LetsGoTrades.commandtype.trade, discordcontext = Context, tradepkm = (PB7)pkm };
                    LetsGoTrades.TheQ.Enqueue(queueitem);
                    await FollowupAsync($"{Context.User.Username} - Added to the LGPE Link Trade Queue. Current Position: {LetsGoTrades.TheQ.Count}. Receiving: {(pkm.IsShiny ? "Shiny" : "")} {(Species)pkm.Species}{(pkm.Form == 0 ? "" : "-" + ShowdownParsing.GetStringFromForm(pkm.Form, GameInfo.Strings, pkm.Species, pkm.Context))}");

                }
                catch
                {
                    var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
                    await FollowupAsync(msg,ephemeral:true).ConfigureAwait(false);
                }
            }
            if(Pb7 != default)
            {
                if (LetsGoTrades.TheQ.Any(z => z.discordcontext.User == Context.User))
                {
                    await FollowupAsync("you are already in queue",ephemeral:true);
                    return;
                }
                var correctchannelcheck = Hub.Config.TradeBot.tradebotchannel.Split(',');
                if (!correctchannelcheck.Contains(Context.Channel.Id.ToString()))
                {
                    await FollowupAsync("You can not use that command in this channel",ephemeral:true);
                    return;
                }
                if (!EncounterEvent.Initialized)
                    EncounterEvent.RefreshMGDB(Hub.Config.TradeBot.mgdbpath);
                var attachment = Pb7;
                if (attachment == default)
                {
                    await FollowupAsync("No attachment provided!",ephemeral:true).ConfigureAwait(false);
                    return;
                }

                var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);

                if (att == null)
                {
                    await FollowupAsync("something went wrong with grabbing your attachment",ephemeral:true);
                    return;
                }
                var pkm = GetRequest(att);


                if (pkm is not PB7 || !new LegalityAnalysis(pkm).Valid)
                {

                    var imsg = $"Oops! This file is illegal Here's the legality report: ";
                    await FollowupAsync(imsg + new LegalityAnalysis(pkm).Report(),ephemeral:true).ConfigureAwait(false);
                    return;
                }
                try { await Context.User.SendMessageAsync("I've added you to the queue! I'll message you here when your trade is starting."); }
                catch { await FollowupAsync("Please enable direct messages from server members to be queued",ephemeral:true); return; };
                var queueitem = new TheQobject { commandtype = LetsGoTrades.commandtype.trade, discordcontext = Context, tradepkm = (PB7)pkm };
                LetsGoTrades.TheQ.Enqueue(queueitem);

                await FollowupAsync($"{Context.User.Username} - Added to the LGPE Link Trade Queue. Current Position: {LetsGoTrades.TheQ.Count}. Receiving: {(pkm.IsShiny ? "Shiny" : "")} {(Species)pkm.Species}{(pkm.Form == 0 ? "" : "-" + ShowdownParsing.GetStringFromForm(pkm.Form, GameInfo.Strings, pkm.Species, pkm.Context))}");

            
            }
        }
  
       

        private static PB7? GetRequest(Download<PKM> dl)
        {
            if (!dl.Success)
                return null;
            return dl.Data switch
            {
                null => null,
                PB7 pkm => pkm,
                _ => EntityConverter.ConvertToType(dl.Data, typeof(PB7), out _) as PB7
            };
        }
        [SlashCommand("dump","get pb7 of pokemon in your box without trading")]
        public async Task dump()
        {
            await DeferAsync();
            try { await Context.User.SendMessageAsync("I've added you to the queue! I'll message you here when your trade is starting."); }
            catch { await FollowupAsync("Please enable direct messages from server members to be queued", ephemeral: true); return; };
            var queueitem = new TheQobject { commandtype = LetsGoTrades.commandtype.trade, discordcontext = Context, tradepkm = null };
            LetsGoTrades.TheQ.Enqueue(queueitem);

            await FollowupAsync($"{Context.User.Username} - Added to the LGPE Dump Queue. Current Position: {LetsGoTrades.TheQ}.");

        }
        [SlashCommand("queue","shows the queue")]
        
        public async Task queue()
        {
            await DeferAsync();
            var arr = LetsGoTrades.TheQ.ToArray();
            var sb = new System.Text.StringBuilder();
            var embed = new EmbedBuilder();
            if (arr.Length == 0)
            {
                await FollowupAsync("queue is empty",ephemeral:true);
            }
            int r = 0;
            foreach (object i in arr)
            {

                sb.AppendLine((r + 1).ToString() + ". " + arr[r].discordcontext.User.Username);
                r++;
            }
            embed.AddField(x =>
            {

                x.Name = "Trade Queue:";
                x.Value = sb.ToString();
                x.IsInline = false;


            });
            
                await FollowupAsync(embed: embed.Build(),ephemeral:true);
        
        }

        [SlashCommand("convert","makes a pb7 file from showdown text")]

        public async Task pbjmaker([Summary("PokemonText")]string ShowdownSet)
        {
            await DeferAsync();
           
            var set = await ConvertToShowdown(ShowdownSet);
            RegenTemplate rset = new(set);
             
  

            try
            {
                var trainer = TrainerSettings.GetSavedTrainerData(GameVersion.GE,7);
                var sav = SaveUtil.GetBlankSAV((GameVersion)trainer.Game, trainer.OT);
                var pkm = sav.GetLegalFromSet(rset, out var result);
        
                if (pkm.Species == 151 || pkm.Species == 150)
                    pkm.SetAwakenedValues(set);
               
                var spec = GameInfo.Strings.Species[set.Species];
          
                if (!new LegalityAnalysis(pkm).Valid) 
                {
                    var reason = result == LegalizationResult.Timeout ? $"That {spec} set took too long to generate." : $"I wasn't able to create a {spec} from that set.";
                    var imsg = $"Oops! {reason}";
                    if (result == LegalizationResult.Failed || !new LegalityAnalysis(pkm).Valid)
                        imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(set, sav, pkm)}";
                    string temppokewait2 = $"{Path.GetTempPath()}//{pkm.FileName}";
                    File.WriteAllBytes(temppokewait2, pkm.DecryptedBoxData);
                    await FollowupWithFileAsync(temppokewait2, pkm.FileName, "Here is your illegal pk file");
                    File.Delete(temppokewait2);
                    return;
                }
                if (pkm.PartyStatsPresent)
                    pkm.ResetPartyStats();
                string temppokewait = $"{Path.GetTempPath()}//{pkm.FileName}";
                File.WriteAllBytes(temppokewait, pkm.DecryptedBoxData);
                await FollowupWithFileAsync(temppokewait,pkm.FileName,"Here is your legalized pk file");
                File.Delete(temppokewait);
               
                return;

            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
               
                var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
                await FollowupAsync(msg,ephemeral:true).ConfigureAwait(false);
            }

            

        
        }
        public static async Task<ShowdownSet> ConvertToShowdown(string setstring)
        {
            // LiveStreams remove new lines, so we are left with a single line set
            var restorenick = string.Empty;

            var nickIndex = setstring.LastIndexOf(')');
            if (nickIndex > -1)
            {
                restorenick = setstring[..(nickIndex + 1)];
                if (restorenick.TrimStart().StartsWith("("))
                    return null;
                setstring = setstring[(nickIndex + 1)..];
            }

            foreach (string i in splittables)
            {
                if (setstring.Contains(i))
                    setstring = setstring.Replace(i, $"\r\n{i}");
            }
         
            var finalset = restorenick + setstring;
          
            return new ShowdownSet(finalset);
        }

        private static readonly string[] splittables =
        {
            "Ability:", "EVs:", "IVs:", "Shiny:", "Gigantamax:", "Ball:", "- ", "Level:",
            "Happiness:", "Language:", "OT:", "OTGender:", "TID:", "SID:", "Alpha:",
            "Adamant Nature", "Bashful Nature", "Brave Nature", "Bold Nature", "Calm Nature",
            "Careful Nature", "Docile Nature", "Gentle Nature", "Hardy Nature", "Hasty Nature",
            "Impish Nature", "Jolly Nature", "Lax Nature", "Lonely Nature", "Mild Nature",
            "Modest Nature", "Naive Nature", "Naughty Nature", "Quiet Nature", "Quirky Nature",
            "Rash Nature", "Relaxed Nature", "Sassy Nature", "Serious Nature", "Timid Nature",
        };
    }
}

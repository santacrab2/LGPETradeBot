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
    

   public class TradeModule : InteractionModuleBase<SocketInteractionContext>
    {
        public static PokeTradeHub<PK8> Hub = SysCordInstance.Self.Hub;



        [SlashCommand("trade", "Trades You a pokemon from showdown text in Lets Go games")]

        public async Task Trade([Summary("PokemonText","put your copied showdown text here")]string ShowdownSet = "", Attachment Pb7 = default)
        {
            if (ShowdownSet != "")
            {
                if (LetsGoTrades.discordID.Contains(Context.User.Id))
                {
                    await RespondAsync("you are already in queue",ephemeral:true);
                    return;
                }
                var correctchannelcheck = Hub.Config.TradeBot.tradebotchannel.Split(',');
                if (!correctchannelcheck.Contains(Context.Channel.Id.ToString()))
                {
                    await RespondAsync("You can not use that command in this channel",ephemeral:true);

                    return;
                }
                
                var set = ConvertToShowdown(ShowdownSet);
                var template = AutoLegalityWrapper.GetTemplate(set);
                if (set.InvalidLines.Count != 0)
                {
                    var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                    await RespondAsync(msg,ephemeral:true).ConfigureAwait(false);
                    return;
                }

                try
                {
                    var sav = SaveUtil.GetBlankSAV(GameVersion.GE, "piplup");
                    var pkm = sav.GetLegalFromSet(template, out var result);
                    var res = result.ToString();
                 
                    if (pkm.Nickname.ToLower() == "egg" && Breeding.CanHatchAsEgg(pkm.Species))
                        EggTrade((PB7)pkm);

                    var la = new LegalityAnalysis(pkm);
                    var spec = GameInfo.Strings.Species[template.Species];
            


                    if (!la.Valid)
                    {
                        var reason = res == "Timeout" ? $"That {spec} set took too long to generate." : $"I wasn't able to create a {spec} from that set.";
                        var imsg = $"Oops! {reason}";
                        if (res == "Failed")
                            imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                        await RespondAsync(imsg,ephemeral:true).ConfigureAwait(false);
                        return;
                    }
                    pkm.ResetPartyStats();
                    try { await Context.User.SendMessageAsync("I've added you to the queue! I'll message you here when your trade is starting."); }
                    catch { await RespondAsync("Please enable direct messages from server members to be queued",ephemeral:true); return; };
                    LetsGoTrades.discordname.Enqueue(Context.User);
                    LetsGoTrades.discordID.Enqueue(Context.User.Id);
                    LetsGoTrades.Channel.Enqueue(Context.Channel);
                    LetsGoTrades.tradepkm.Enqueue(pkm);
                    LetsGoTrades.Commandtypequ.Enqueue(LetsGoTrades.commandtype.trade);
                    await RespondAsync($"{Context.User.Username} - Added to the LGPE Link Trade Queue. Current Position: {LetsGoTrades.discordID.Count}. Receiving: {(pkm.IsShiny ? "Shiny" : "")} {(Species)pkm.Species}{(pkm.Form == 0 ? "" : "-" + ShowdownParsing.GetStringFromForm(pkm.Form, GameInfo.Strings, pkm.Species, pkm.Format))}");

                }
                catch
                {
                    var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
                    await RespondAsync(msg,ephemeral:true).ConfigureAwait(false);
                }
            }
            if(Pb7 != default)
            {
                if (LetsGoTrades.discordID.Contains(Context.User.Id))
                {
                    await RespondAsync("you are already in queue",ephemeral:true);
                    return;
                }
                var correctchannelcheck = Hub.Config.TradeBot.tradebotchannel.Split(',');
                if (!correctchannelcheck.Contains(Context.Channel.Id.ToString()))
                {
                    await RespondAsync("You can not use that command in this channel",ephemeral:true);
                    return;
                }
                if (!EncounterEvent.Initialized)
                    EncounterEvent.RefreshMGDB(Hub.Config.TradeBot.mgdbpath);
                var attachment = Pb7;
                if (attachment == default)
                {
                    await RespondAsync("No attachment provided!",ephemeral:true).ConfigureAwait(false);
                    return;
                }

                var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);

                if (att == null)
                {
                    await RespondAsync("something went wrong with grabbing your attachment",ephemeral:true);
                    return;
                }
                var pkm = GetRequest(att);


                if (pkm is not PB7 || !new LegalityAnalysis(pkm).Valid)
                {

                    var imsg = $"Oops! This file is illegal Here's the legality report: ";
                    await RespondAsync(imsg + new LegalityAnalysis(pkm).Report(),ephemeral:true).ConfigureAwait(false);
                    return;
                }
                try { await Context.User.SendMessageAsync("I've added you to the queue! I'll message you here when your trade is starting."); }
                catch { await RespondAsync("Please enable direct messages from server members to be queued",ephemeral:true); return; };
                LetsGoTrades.discordname.Enqueue(Context.User);
                LetsGoTrades.discordID.Enqueue(Context.User.Id);
                LetsGoTrades.Channel.Enqueue(Context.Channel);
                LetsGoTrades.tradepkm.Enqueue(pkm);
                LetsGoTrades.Commandtypequ.Enqueue(LetsGoTrades.commandtype.trade);
             
                await RespondAsync($"{Context.User.Username} - Added to the LGPE Link Trade Queue. Current Position: {LetsGoTrades.discordID.Count}. Receiving: {(pkm.IsShiny ? "Shiny" : "")} {(Species)pkm.Species}{(pkm.Form == 0 ? "" : "-" + ShowdownParsing.GetStringFromForm(pkm.Form, GameInfo.Strings, pkm.Species, pkm.Format))}");

            
            }
        }
  
        public static PB7 EggTrade(PB7 pk)
        {
           
            pk.IsNicknamed = true;
            pk.Nickname = pk.Language switch
            {
                1 => "タマゴ",
                3 => "Œuf",
                4 => "Uovo",
                5 => "Ei",
                7 => "Huevo",
                8 => "알",
                9 or 10 => "蛋",
                _ => "Egg",
            };

            pk.IsEgg = true;
            pk.Egg_Location = 60002;
            pk.MetDate = DateTime.Parse("2020/10/20");
            pk.EggMetDate = pk.MetDate;
            pk.HeldItem = 0;
            pk.CurrentLevel = 1;
            pk.EXP = 0;
           
            pk.Met_Level = 1;
            pk.Met_Location = 30002;
            pk.CurrentHandler = 0;
            pk.OT_Friendship = 1;
            pk.HT_Name = "";
            pk.HT_Friendship = 0;
           
            pk.HT_Gender = 0;
            pk.HT_Memory = 0;
            pk.HT_Feeling = 0;
            pk.HT_Intensity = 0;
            pk.StatNature = pk.Nature;
            pk.EVs = new int[] { 0, 0, 0, 0, 0, 0 };
            pk.Markings = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            
            pk.ClearRelearnMoves();
            pk.Moves = new int[] { 0, 0, 0, 0 };
            var la = new LegalityAnalysis(pk);
            var enc = la.EncounterMatch;
            pk.CurrentFriendship = enc is EncounterStatic s ? s.EggCycles : pk.PersonalInfo.HatchCycles;
            pk.RelearnMoves = MoveBreed.GetExpectedMoves(pk.RelearnMoves, la.EncounterMatch);
            pk.Moves = pk.RelearnMoves;
            pk.Move1_PPUps = pk.Move2_PPUps = pk.Move3_PPUps = pk.Move4_PPUps = 0;
            pk.SetMaximumPPCurrent(pk.Moves);
            pk.SetSuggestedHyperTrainingData();
            pk.SetSuggestedRibbons(la.EncounterMatch);
            return pk;
        }

        private static PB7? GetRequest(Download<PKM> dl)
        {
            if (!dl.Success)
                return null;
            return dl.Data switch
            {
                null => null,
                PB7 pkm => pkm,
                _ => PKMConverter.ConvertToType(dl.Data, typeof(PB7), out _) as PB7
            };
        }
        [SlashCommand("dump","get pb7 of pokemon in your box without trading")]
        public async Task dump()
        {
            try { await Context.User.SendMessageAsync("I've added you to the queue! I'll message you here when your trade is starting."); }
            catch { await RespondAsync("Please enable direct messages from server members to be queued", ephemeral: true); return; };
            LetsGoTrades.discordname.Enqueue(Context.User);
            LetsGoTrades.discordID.Enqueue(Context.User.Id);
            LetsGoTrades.Channel.Enqueue(Context.Channel);
            LetsGoTrades.tradepkm.Enqueue(null);
            LetsGoTrades.Commandtypequ.Enqueue(LetsGoTrades.commandtype.dump);

            await RespondAsync($"{Context.User.Username} - Added to the LGPE Dump Queue. Current Position: {LetsGoTrades.discordID.Count}.");

        }
        [SlashCommand("queue","shows the queue")]
        
        public async Task queue()
        {
            Object[] arr = LetsGoTrades.discordname.ToArray();
            var sb = new System.Text.StringBuilder();
            var embed = new EmbedBuilder();
            if (arr.Length == 0)
            {
                await RespondAsync("queue is empty",ephemeral:true);
            }
            int r = 0;
            foreach (object i in arr)
            {

                sb.AppendLine((r + 1).ToString() + ". " + arr[r].ToString());
                r++;
            }
            embed.AddField(x =>
            {

                x.Name = "Trade Queue:";
                x.Value = sb.ToString();
                x.IsInline = false;


            });
            
                await RespondAsync(embed: embed.Build(),ephemeral:true);
        
        }

        [SlashCommand("convert","makes a pb7 file from showdown text")]

        public async Task pbjmaker([Summary("PokemonText")]string ShowdownSet)
        {
            //ShowdownSet = ReusableActions.StripCodeBlock(ShowdownSet);
            var set = ConvertToShowdown(ShowdownSet);
      
            if (set.InvalidLines.Count != 0)
            {
                var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                await RespondAsync(msg,ephemeral:true).ConfigureAwait(false);
                return;
            }

            try
            {
                var sav = SaveUtil.GetBlankSAV(GameVersion.GE,"piplup");
                var pkm = (PB7)sav.GetLegalFromSet(set, out var result);
                pkm = (PB7)pkm.Legalize();
               var res = result.ToString();

                if (pkm.Nickname.ToLower() == "egg" && Breeding.CanHatchAsEgg(pkm.Species))
                    EggTrade((PB7)pkm);

                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[set.Species];

                if (!la.Valid)
                {
                    var reason = res == "Timeout" ? $"That {spec} set took too long to generate." : $"I wasn't able to create a {spec} from that set.";
                    var imsg = $"Oops! {reason}";
                    if (res == "Failed")
                        imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(set, sav, pkm)}";
                    await RespondAsync(imsg,ephemeral:true).ConfigureAwait(false);
                    return;
                }
                pkm.ResetPartyStats();
                string temppokewait = $"{Path.GetTempPath()}//{pkm.FileName}";
                File.WriteAllBytes(temppokewait, pkm.EncryptedBoxData);
                await RespondWithFileAsync(temppokewait, text:"Here is your legalized pk file");
                File.Delete(temppokewait);
                return;

            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
               
                var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
                await RespondAsync(msg,ephemeral:true).ConfigureAwait(false);
            }

            

        
        }
        public static ShowdownSet? ConvertToShowdown(string setstring)
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

using System;
using System.Collections;
using System.Text;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using PKHeX.Core.AutoMod;


using System.Linq;

namespace SysBot.Pokemon.Discord
{
    

    class TradeModule : ModuleBase<SocketCommandContext>
    {

       
        [Command("Trade")]
        [Alias("t")]
        public async Task Trade([Remainder]string Content)
        {
            var set = new ShowdownSet(Content);
            if (set.InvalidLines.Count != 0)
            {
                var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }
           try
            {
                var sav = SaveUtil.GetBlankSAV(GameVersion.GE, "Piplup");
                var pkm = sav.GetLegalFromSet(set, out var result);
                if (pkm.Nickname.ToLower() == "egg" && Breeding.CanHatchAsEgg(pkm.Species))
                    pkm= EggTrade((PB7)pkm);
                if (pkm is not PB7 || !new LegalityAnalysis(pkm).Valid)
                {
                    var reason = result.ToString() == "Timeout" ? "That set took too long to generate." : "I wasn't able to create something from that.";
                    var imsg = $"Oops! {reason} Here's the legality report: ";
                    await Context.Channel.SendMessageAsync(imsg + new LegalityAnalysis(pkm).Report()).ConfigureAwait(false);
                    return;
                }

                pkm.ResetPartyStats();
               LetsGoTrades.discordname.Enqueue(Context.User);
                LetsGoTrades.discordID.Enqueue(Context.User.Id);
                LetsGoTrades.Channel.Enqueue(Context.Channel);
                LetsGoTrades.tradepkm.Enqueue(pkm);
            } catch
            {
                var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
                await ReplyAsync(msg).ConfigureAwait(false);
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
            pk.ClearRecordFlags();
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
    }
}

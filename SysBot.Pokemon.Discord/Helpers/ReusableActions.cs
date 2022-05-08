using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public static class ReusableActions 
    {
     


        public static async Task RepostPKMAsShowdownAsync(this ISocketMessageChannel channel, IAttachment att)
        {
            if (!EntityDetection.IsSizePlausible(att.Size))
                return;
            var result = await NetUtil.DownloadPKMAsync(att).ConfigureAwait(false);
            if (!result.Success)
                return;

            var pkm = result.Data!;
            await channel.SendPKMAsShowdownSetAsync(pkm).ConfigureAwait(false);
        }


        public static async Task EchoAndReply(this ISocketMessageChannel channel, string msg)
        {
            // Announce it in the channel the command was entered only if it's not already an echo channel.
            EchoUtil.Echo(msg);
            if (!EchoModule.IsEchoChannel(channel))
                await channel.SendMessageAsync(msg).ConfigureAwait(false);
        }

        public static async Task SendPKMAsShowdownSetAsync(this ISocketMessageChannel channel, PKM pkm)
        {
            var txt = GetFormattedShowdownText(pkm);
            await channel.SendMessageAsync(txt).ConfigureAwait(false);
        }

        public static string GetFormattedShowdownText(PKM pkm)
        {

            var newShowdown = new List<string>();
            var showdown = ShowdownParsing.GetShowdownText(pkm);
            foreach (var line in showdown.Split('\n'))
                newShowdown.Add(line);
            var pb7conv = (PB7)pkm;
            int[] AVs = new int[] { pb7conv.AV_HP, pb7conv.AV_ATK, pb7conv.AV_DEF, pb7conv.AV_SPA, pb7conv.AV_SPD, pb7conv.AV_SPE };
            newShowdown.Insert(1, $"AVs: {AVs[0]} HP / {AVs[1]} Atk / {AVs[2]} Def / {AVs[3]} SpA / {AVs[4]} SpD / {AVs[5]} Spe");
            if (pkm.IsEgg)
                newShowdown.Insert(1, "IsEgg: Yes");
            if (pkm.Ball > (int)Ball.None)
                newShowdown.Insert(newShowdown.FindIndex(z => z.Contains("Nature")), $"Ball: {(Ball)pkm.Ball} Ball");
            if (pkm.IsShiny)
            {
                var index = newShowdown.FindIndex(x => x.Contains("Shiny: Yes"));
                if (pkm.ShinyXor == 0 || pkm.FatefulEncounter)
                    newShowdown[index] = "Shiny: Square\r";
                else newShowdown[index] = "Shiny: Star\r";
            }
            var SID = string.Format("{0:0000}", pkm.DisplaySID);
            var TID = string.Format("{0:000000}", pkm.DisplayTID);

            newShowdown.InsertRange(1, new string[] { $"OT: {pkm.OT_Name}", $"TID: {TID}", $"SID: {SID}", $"OTGender: {(Gender)pkm.OT_Gender}", $"Language: {(LanguageID)pkm.Language}" });
            return Format.Code(string.Join("\n", newShowdown).TrimEnd());
      
        }

        public static List<string> GetListFromString(string str)
        {
            // Extract comma separated list
            return str.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static string StripCodeBlock(string str) => str.Replace("`\n", "").Replace("\n`", "").Replace("`", "").Trim();
    }
}
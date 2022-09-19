using PKHeX.Core;
using PKHeX.Core.Searching;
using PKHeX.Core.AutoMod;
using SysBot.Base;
using System;
using System.Drawing;
using System.Linq;
using PKHeX.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;
using System.Collections;
using System.Collections.Generic;
using Discord;
using System.Diagnostics;


namespace SysBot.Pokemon
{
    public class TheQobject
    {
        public IInteractionContext discordcontext { get; set; }
      
        public PB7 tradepkm { get; set; }
        public LetsGoTrades.commandtype commandtype { get; set; }
    }
    public class LetsGoTrades : PokeRoutineExecutor
    {

        public static CancellationTokenSource wtpsource = new CancellationTokenSource();
        public static SAV7b sav = new();
        public static PB7 pkm = new();
        public static PokeTradeHub<PK8> Hub;
        public static Queue<TheQobject> TheQ = new();
        public static int initialloop = 0;
        int passes = 0;
        public LetsGoTrades(PokeTradeHub<PK8> hub, PokeBotState cfg) : base(cfg)
        {
            Hub = hub;
           
        }
        public override async Task MainLoop(CancellationToken token)
        {
            if (!EncounterEvent.Initialized)
                EncounterEvent.RefreshMGDB(Hub.Config.TradeBot.mgdbpath);
            APILegality.AllowBatchCommands = true;
            APILegality.AllowTrainerOverride = true;
            APILegality.ForceSpecifiedBall = true;
            APILegality.SetMatchingBalls = true;
            Legalizer.EnableEasterEggs = false;

            Log("Identifying trainer data of the host console.");
          sav = await LGIdentifyTrainer(token).ConfigureAwait(false);

            Log("Starting main TradeBot loop.");
         
                Config.IterateNextRoutine();
                var task = Config.CurrentRoutineType switch
                {
                    
                     PokeRoutineType.LGPETradeBot => DoTrades(token),
                     _ => DoNothing(token)
                };
                await task.ConfigureAwait(false);
            
            await DetachController(token);
            Hub.Bots.Remove(this);
            
        }

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        public async Task DoNothing(CancellationToken token)
        {
            int waitCounter = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.Idle)
            {
                if (waitCounter == 0)
                    Log("No task assigned. Waiting for new task assignment.");
                waitCounter++;
                await Task.Delay(1_000, token).ConfigureAwait(false);
            }
        }
        private static readonly byte[] BlackPixel = // 1x1 black pixel
       {
            0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00
        };
        public async Task DoTrades(CancellationToken token)
        {
            Stopwatch btimeout = new();
            var BoxStart = 0x533675B0;
            var SlotSize = 260;
            var GapSize = 380;
            var SlotCount = 25;
            var read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
            overworld = read[0];
            uint GetBoxOffset(int box) => 0x533675B0;
            uint GetSlotOffset(int box, int slot) => GetBoxOffset(box) + (uint)((SlotSize + GapSize) * slot);
            Random dpoke = new Random();
            while (!token.IsCancellationRequested)
            {
                
                int waitCounter = 0;
                while (TheQ.Count == 0 && !Hub.Config.TradeBot.distribution)
                {
                    
                   
                    if (waitCounter == 0)
                        Log("Nothing to check, waiting for new users...") ;
                    waitCounter++;
                    await Task.Delay(1_000, token).ConfigureAwait(false);

                }
                while (TheQ.Count == 0 && Hub.Config.TradeBot.distribution)
                {
                    
                    Log("Starting Distribution");
                    
                  
                    var dcode = new List<pictocodes>();
                    for (int i = 0; i <= 2; i++)
                    {

                        dcode.Add(pictocodes.Pikachu);

                    }
                    var dspecies = dpoke.Next(150);
                    ShowdownSet set = new ShowdownSet($"Piplup.net({(Species)dspecies})\nLevel: 90\nShiny: Yes");
                    var trainer = TrainerSettings.GetSavedTrainerData(GameVersion.GG, 7);
                    var sav = SaveUtil.GetBlankSAV((GameVersion)trainer.Game, trainer.OT);
                    var dpkm = sav.GetLegalFromSet(set, out var result);
                    try
                    {
                        dpkm = dpkm.Legalize();
                    }
                    catch(Exception ex) { continue; }
                    if (!new LegalityAnalysis(dpkm).Valid)
                        continue;
                    var dslotofs = GetSlotOffset(1, 0);
                    var dStoredLength = SlotSize - 0x1C;
                    await Connection.WriteBytesAsync(dpkm.EncryptedBoxData.Slice(0, dStoredLength), BoxSlot1, token);
                    await Connection.WriteBytesAsync(dpkm.EncryptedBoxData.AsSpan(dStoredLength).ToArray(), (uint)(dslotofs + dStoredLength + 0x70), token);

                    System.IO.File.WriteAllText($"{System.IO.Directory.GetCurrentDirectory()}//LGPEDistrib.txt", $"LGPE Giveaway: Shiny {(Species)dpkm.Species}");
            
                    await SetController(token);
                    for (int i = 0; i < 3; i++)
                        await Click(A, 1000, token);
                    read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token);
                   
                    //while (read[0] != overworld)
                   // {

                        //await Click(B, 1000, token);
                       // read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token);
                   // }

                    await Click(X, 2000, token).ConfigureAwait(false);
                    Log("opening menu");
                  while(BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff,4,token),0) != menuscreen)
                    {
                        await Click(B, 2000, token);
                        await Click(X, 2000, token);
                    }
                    Log("selecting communicate");
                    await SetStick(SwitchStick.RIGHT, 30000, 0, 0, token).ConfigureAwait(false);
                    await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                    while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 4, token), 0) == menuscreen)
                    {
                        await Click(A, 1000, token);
                        if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == savescreen || BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == savescreen2)
                        {
                            read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                            while (read[0] != overworld)
                            {

                                await Click(B, 1000, token);
                                read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                            }
                            await Click(X, 2000, token).ConfigureAwait(false);
                            Log("opening menu");
                            while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 4, token), 0) != menuscreen)
                            {
                                await Click(B, 2000, token);
                                await Click(X, 2000, token);
                            }
                            Log("selecting communicate");
                            await SetStick(SwitchStick.RIGHT, 30000, 0, 0, token).ConfigureAwait(false);
                            await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                        }
                    }
                    await Task.Delay(2000);
                    Log("selecting faraway connection");
                  
                    
                    await SetStick(SwitchStick.RIGHT, 0, -30000, 0, token).ConfigureAwait(false);
                    await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                    await Click(A, 10000, token).ConfigureAwait(false);
                   
                    await Click(A, 1000, token).ConfigureAwait(false);
                 
                    Log("Entering distribution Link Code");

                    foreach (pictocodes pc in dcode)
                    {
                        if ((int)pc > 4)
                        {
                            await SetStick(SwitchStick.RIGHT, 0, -30000, 100, token).ConfigureAwait(false);
                            await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                        }
                        if ((int)pc <= 4)
                        {
                            for (int i = (int)pc; i > 0; i--)
                            {
                                await SetStick(SwitchStick.RIGHT, 30000, 0, 100, token).ConfigureAwait(false);
                                await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                                await Task.Delay(500).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            for (int i = (int)pc - 5; i > 0; i--)
                            {
                                await SetStick(SwitchStick.RIGHT, 30000, 0, 100, token).ConfigureAwait(false);
                                await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                                await Task.Delay(500).ConfigureAwait(false);
                            }
                        }
                        await Click(A, 200, token).ConfigureAwait(false);
                        await Task.Delay(500).ConfigureAwait(false);
                        if ((int)pc <= 4)
                        {
                            for (int i = (int)pc; i > 0; i--)
                            {
                                await SetStick(SwitchStick.RIGHT, -30000, 0, 100, token).ConfigureAwait(false);
                                await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                                await Task.Delay(500).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            for (int i = (int)pc - 5; i > 0; i--)
                            {
                                await SetStick(SwitchStick.RIGHT, -30000, 0, 100, token).ConfigureAwait(false);
                                await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                                await Task.Delay(500).ConfigureAwait(false);
                            }
                        }

                        if ((int)pc > 4)
                        {
                            await SetStick(SwitchStick.RIGHT, 0, 30000, 100, token).ConfigureAwait(false);
                            await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                        }
                    }

                    Log("Searching for distribution user");
                    btimeout.Restart();
                    var dnofind = false;
                    while (await LGIsinwaitingScreen(token))
                    {
                        await Task.Delay(100);
                        if (btimeout.ElapsedMilliseconds >= 45_000)
                        {
                            
                            Log("User not found");
                            dnofind = true;
                            read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                            while (read[0] != overworld)
                            {
                                await Click(B, 1000, token);
                                read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                            }
                        }
                    }
                    if (dnofind == true)
                        continue;
                    await Task.Delay(10000);
                    var tradepartnersav = new SAV7b();
                    var tradepartnersav2 = new SAV7b();
                    var tpsarray = await SwitchConnection.ReadBytesAsync(TradePartnerData, 0x168, token);
                    var tpsarray2 = await SwitchConnection.ReadBytesAsync(TradePartnerData2, 0x168, token);
                    tpsarray2.CopyTo(tradepartnersav2.Blocks.Status.Data, tradepartnersav2.Blocks.Status.Offset);
                    tpsarray.CopyTo(tradepartnersav.Blocks.Status.Data, tradepartnersav.Blocks.Status.Offset);
                
                    if(tradepartnersav.OT != sav.OT) 
                        Log($"{tradepartnersav.DisplayTID},{tradepartnersav.DisplaySID},{tradepartnersav.OT},{(GameVersion)tradepartnersav.Blocks.Status.Game}");
                    if (tradepartnersav2.OT != sav.OT)
                        Log($"{tradepartnersav2.DisplayTID},{tradepartnersav2.DisplaySID},{tradepartnersav2.OT},{(GameVersion)tradepartnersav2.Blocks.Status.Game}");
                    while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == Boxscreen)
                    {
                        await Click(A, 1000, token);
                    }
                    Log("waiting on trade screen");
              


                    await Task.Delay(15_000);
             
                    await Click(A, 200, token).ConfigureAwait(false);
                    Log("Distribution trading...");
                    await Task.Delay(15000);

                    while (await LGIsInTrade(token))
                        await Click(A, 1000, token);
                    
                   
                    Log("Trade should be completed, exiting box");
                    passes = 0;
                    while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) != menuscreen)
                    {
                        if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == menuscreen)
                            break;
                        await Click(B, 1000, token);
                        if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == menuscreen)
                            break;
                        await Click(A, 1000, token);
                        if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == menuscreen)
                            break;
                        await Click(B, 1000, token);
                        if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == menuscreen)
                            break;
                        await Click(B, 1000, token);
                        if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == menuscreen)
                            break;
                       
                        if (passes == 30)
                        {
                            for (int i = 0; i < 7; i++)
                            {
                                await Click(A, 1000, token);
                            }
                        }
                        passes++;
                    }
         
                    btimeout.Restart();
                    int dacount = 4;
                    Log("spamming b to get back to overworld");
                    read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                    passes = 0;
                    while (read[0] != overworld)
                    {
                        await Click(B, 1000, token);
                        read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                        if (passes == 30)
                        {
                            for (int i = 0; i < 7; i++)
                            {
                                await Click(A, 1000, token);
                            }
                        }
                        passes++;

                    }
                    await Click(B, 1000, token);
                    await Click(B, 1000, token);
                    Log("done spamming b");
                    await Task.Delay(2500);
                    initialloop++;
                    continue;
                }
                if (TheQ.Count == 0)
                    continue;
                Log("starting a trade sequence");
               

                var code = new List<pictocodes>();
                for (int i = 0; i <= 2; i++)
                {
                    code.Add((pictocodes)Util.Rand.Next(10));
                    //code.Add(pictocodes.Pikachu);
                    
                }
                TradebotSettings.generatebotsprites(code);
                var code0 = System.Drawing.Image.FromFile($"{System.IO.Directory.GetCurrentDirectory()}//code0.png");
                var code1 = System.Drawing.Image.FromFile($"{System.IO.Directory.GetCurrentDirectory()}//code1.png");
                var code2 = System.Drawing.Image.FromFile($"{System.IO.Directory.GetCurrentDirectory()}//code2.png");
                var finalpic = Merge(code0, code1, code2);
                finalpic.Save($"{System.IO.Directory.GetCurrentDirectory()}//finalcode.png");
                var user = TheQ.Dequeue();
                var filename = System.IO.Path.GetFileName($"{System.IO.Directory.GetCurrentDirectory()}//finalcode.png");
                var lgpeemb = new EmbedBuilder().WithTitle($"{code[0]}, {code[1]}, {code[2]}").WithImageUrl($"attachment://{filename}").Build();
                await user.discordcontext.User.SendFileAsync(filename,text: $"My IGN is {Connection.Label.Split('-')[0]}\nHere is your link code:", embed: lgpeemb);
                var pkm =user.tradepkm;
                if (pkm != null)
                {
                    var slotofs = GetSlotOffset(1, 0);
                    var StoredLength = SlotSize - 0x1C;
                    await Connection.WriteBytesAsync(pkm.EncryptedBoxData.Slice(0, StoredLength), BoxSlot1, token);
                    await Connection.WriteBytesAsync(pkm.EncryptedBoxData.AsSpan(StoredLength).ToArray(), (uint)(slotofs + StoredLength + 0x70), token);
                }
             
                await SetController(token);
                for (int i = 0; i < 3; i++)
                    await Click(A, 1000, token);
                read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                //while (read[0] != overworld)
                //{

                    //await Click(B, 1000, token);
                    //read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
               // }
                await Click(X, 2000, token).ConfigureAwait(false);
                Log("opening menu");
                while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 4, token), 0) != menuscreen)
                {
                    await Click(B, 2000, token);
                    await Click(X, 2000, token);
                }
                Log("selecting communicate");
                await SetStick(SwitchStick.RIGHT, 30000, 0, 0, token).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
               while(BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff,2,token),0) == menuscreen || BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 4, token), 0) == waitingtotradescreen)
                {
                    
                    await Click(A, 1000, token);
                    if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == savescreen || BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == savescreen2)
                    {
                        read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                        while (read[0] != overworld)
                        {

                            await Click(B, 1000, token);
                            read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                        }
                        await Click(X, 2000, token).ConfigureAwait(false);
                        Log("opening menu");
                        while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 4, token), 0) != menuscreen)
                        {
                            await Click(B, 2000, token);
                            await Click(X, 2000, token);
                        }
                        Log("selecting communicate");
                        await SetStick(SwitchStick.RIGHT, 30000, 0, 0, token).ConfigureAwait(false);
                        await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                    }


                }
                await Task.Delay(2000);
                Log("selecting faraway connection");

                await SetStick(SwitchStick.RIGHT,0,-30000, 0, token).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                await Click(A, 10000, token).ConfigureAwait(false);
                
                await Click(A, 1000, token).ConfigureAwait(false);
               
                Log("Entering Link Code");
                System.IO.File.WriteAllBytes($"{System.IO.Directory.GetCurrentDirectory()}/Block.png", BlackPixel);
                foreach (pictocodes pc in code)
                {
                    if ((int)pc > 4)
                    {
                        await SetStick(SwitchStick.RIGHT, 0, -30000, 0, token).ConfigureAwait(false);
                        await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                    }
                    if ((int)pc <= 4)
                    {
                        for (int i = (int)pc; i > 0; i--)
                        {
                            await SetStick(SwitchStick.RIGHT, 30000, 0, 0, token).ConfigureAwait(false);
                            await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                            await Task.Delay(500).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        for (int i = (int)pc - 5; i > 0; i--)
                        {
                            await SetStick(SwitchStick.RIGHT, 30000, 0, 0, token).ConfigureAwait(false);
                            await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                            await Task.Delay(500).ConfigureAwait(false);
                        }
                    }
                    await Click(A, 200, token).ConfigureAwait(false);
                    await Task.Delay(500).ConfigureAwait(false);
                    if ((int)pc <= 4)
                    {
                        for (int i = (int)pc; i > 0; i--)
                        {
                            await SetStick(SwitchStick.RIGHT, -30000, 0, 0, token).ConfigureAwait(false);
                            await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                            await Task.Delay(500).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        for (int i = (int)pc - 5; i > 0; i--)
                        {
                            await SetStick(SwitchStick.RIGHT, -30000, 0, 0, token).ConfigureAwait(false);
                            await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                            await Task.Delay(500).ConfigureAwait(false);
                        }
                    }

                    if ((int)pc > 4)
                    {
                        await SetStick(SwitchStick.RIGHT, 0, 30000, 0, token).ConfigureAwait(false);
                        await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                    }
                }
                Log($"Searching for user {user.discordcontext.User.Username}");
                await user.discordcontext.User.SendMessageAsync("searching for you now, you have 45 seconds to match").ConfigureAwait(false);
                await Task.Delay(3000);
                btimeout.Restart();
            
                var nofind = false;
                while (await LGIsinwaitingScreen(token))
                {
                    await Task.Delay(100);
                    if (btimeout.ElapsedMilliseconds >= 45_000)
                    {
                        await user.discordcontext.User.SendMessageAsync("I could not find you, please try again!");
                        Log("User not found");
                        nofind = true;
                        read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                        while (read[0] != overworld)
                        {
                            await Click(B, 1000, token);
                            read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                        }
                        break;
                    }
                }
                if (nofind)
                {
                    System.IO.File.Delete($"{System.IO.Directory.GetCurrentDirectory()}/Block.png");
                  
                    continue;

                }

                Log("User Found");
                await Task.Delay(10000);
              
                System.IO.File.Delete($"{System.IO.Directory.GetCurrentDirectory()}/Block.png");
                commandtype command = user.commandtype;
                if (command == commandtype.trade)
                {
                    var tradepartnersav = new SAV7b();
                    var tradepartnersav2 = new SAV7b();
                    var tpsarray = await SwitchConnection.ReadBytesAsync(TradePartnerData, 0x168, token);
                    tpsarray.CopyTo(tradepartnersav.Blocks.Status.Data, tradepartnersav.Blocks.Status.Offset);
                    var tpsarray2 = await SwitchConnection.ReadBytesAsync(TradePartnerData2, 0x168, token);
                    tpsarray2.CopyTo(tradepartnersav2.Blocks.Status.Data, tradepartnersav2.Blocks.Status.Offset);
                    if (tradepartnersav.OT != sav.OT)
                    {
                        Log($"Found Link Trade Parter: {tradepartnersav.OT}, TID: {tradepartnersav.DisplayTID}, SID: {tradepartnersav.DisplaySID},Game: {(GameVersion)tradepartnersav.Game}");
                        await user.discordcontext.User.SendMessageAsync($"Found Link Trade Parter: {tradepartnersav.OT}, TID: {tradepartnersav.DisplayTID}, SID: {tradepartnersav.DisplaySID}, Game: {(GameVersion)tradepartnersav.Game}");
                    }
                    if(tradepartnersav2.OT != sav.OT)
                    {
                        Log($"Found Link Trade Parter: {tradepartnersav2.OT}, TID: {tradepartnersav2.DisplayTID}, SID: {tradepartnersav2.DisplaySID}");
                        await user.discordcontext.User.SendMessageAsync($"Found Link Trade Parter: {tradepartnersav2.OT}, TID: {tradepartnersav2.DisplayTID}, SID: {tradepartnersav2.DisplaySID}, Game: {(GameVersion)tradepartnersav.Game}");
                    }
                    while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == Boxscreen)
                    {
                        await Click(A, 1000, token);
                    }
                    await user.discordcontext.User.SendMessageAsync("You have 15 seconds to select your trade pokemon");
                    Log("waiting on trade screen");

                    await Task.Delay(15_000).ConfigureAwait(false);


                    await Click(A, 200, token).ConfigureAwait(false);
                    await user.discordcontext.User.SendMessageAsync("trading...");
                    Log("trading...");
                    await Task.Delay(15000);
                    while (await LGIsInTrade(token))
                        await Click(A, 1000, token);



                    Log("Trade should be completed, exiting box");
                    passes = 0;
                    while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) != menuscreen)
                    {
                        if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == menuscreen)
                            break;
                        await Click(B, 2000, token);
                        if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == menuscreen)
                            break;
                        await Click(A, 2000, token);
                        if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == menuscreen)
                            break;
                        await Click(B, 2000, token);
                        if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == menuscreen)
                            break;
                        await Click(B, 2000, token);
                        if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == menuscreen)
                            break;
                        if (passes >= 15)
                        {
                            Log("handling trade evolution");
                            for (int i = 0; i < 7; i++)
                            {
                                await Click(A, 1000, token);
                            }
                        }
                        passes++;
                    }
                    btimeout.Restart();
                    int acount = 4;
                    Log("spamming b to get back to overworld");
                    read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                    passes = 0;
                    while (read[0] != overworld)
                    {

                        await Click(B, 1000, token);
                        read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                        if (passes >= 20)
                        {
                            Log("handling trade evolution");
                            for (int i = 0; i < 7; i++)
                            {
                                await Click(A, 1000, token);
                            }
                        }
                        passes++;
                    }
                    await Click(B, 1000, token);
                    await Click(B, 1000, token);
                    Log("done spamming b");

                    btimeout.Stop();

                    var returnpk = await LGReadPokemon(BoxSlot1, token);
                    if (returnpk == null)
                    {
                        returnpk = new PB7();
                    }
                    if (SearchUtil.HashByDetails(returnpk) != SearchUtil.HashByDetails(pkm))
                    {
                        byte[] writepoke = returnpk.EncryptedBoxData;
                        var tpfile = System.IO.Path.GetTempFileName().Replace(".tmp", "." + returnpk.Extension);
                        tpfile = tpfile.Replace("tmp", returnpk.FileNameWithoutExtension);
                        System.IO.File.WriteAllBytes(tpfile, writepoke);
                        await user.discordcontext.User.SendFileAsync(tpfile, "here is the pokemon you traded me");
                        System.IO.File.Delete(tpfile);
                        Log($"{user.discordcontext.User.Username} completed their trade");
                    }
                    else
                    {
                        await user.discordcontext.User.SendMessageAsync("Something went wrong, no trade happened, please try again!");
                        Log($"{user.discordcontext.User.Username} did not complete their trade");
                    }
            
                    initialloop++;
                    await Task.Delay(2500);
                    continue;
                }
                if(command == commandtype.dump)
                {
                    var tradepartnersav = new SAV7b();
                    var tradepartnersav2 = new SAV7b();
                    var tpsarray = await SwitchConnection.ReadBytesAsync(TradePartnerData, 0x168, token);
                    tpsarray.CopyTo(tradepartnersav.Blocks.Status.Data, tradepartnersav.Blocks.Status.Offset);
                    var tpsarray2 = await SwitchConnection.ReadBytesAsync(TradePartnerData2, 0x168, token);
                    tpsarray2.CopyTo(tradepartnersav2.Blocks.Status.Data, tradepartnersav2.Blocks.Status.Offset);
                    if (tradepartnersav.OT != sav.OT)
                    {
                        Log($"Found Link Trade Parter: {tradepartnersav.OT}, TID: {tradepartnersav.DisplayTID}, SID: {tradepartnersav.DisplaySID},Game: {(GameVersion)tradepartnersav.Game}");
                        await user.discordcontext.User.SendMessageAsync($"Found Link Trade Parter: {tradepartnersav.OT}, TID: {tradepartnersav.DisplayTID}, SID: {tradepartnersav.DisplaySID},Game: {(GameVersion)tradepartnersav.Game}");
                    }
                    if (tradepartnersav2.OT != sav.OT)
                    {
                        Log($"Found Link Trade Parter: {tradepartnersav2.OT}, TID: {tradepartnersav2.DisplayTID}, SID: {tradepartnersav2.DisplaySID}");
                        await user.discordcontext.User.SendMessageAsync($"Found Link Trade Parter: {tradepartnersav2.OT}, TID: {tradepartnersav2.DisplayTID}, SID: {tradepartnersav2.DisplaySID}, Game: {(GameVersion)tradepartnersav.Game}");
                    }
                    await user.discordcontext.User.SendMessageAsync("Highlight the Pokemon in your box, you have 30 seconds");
                    var offereddata = await SwitchConnection.ReadBytesAsync(OfferedPokemon, 0x104, token);
                    var offeredpbm = new PB7(offereddata);
                    byte[] writepoke = offeredpbm.EncryptedBoxData;
                    var tpfile = $"{System.IO.Path.GetTempPath()}//{offeredpbm.FileName}";
                    System.IO.File.WriteAllBytes(tpfile, writepoke);
                    await user.discordcontext.User.SendFileAsync(tpfile, "here is the pokemon you showed me");
                    System.IO.File.Delete(tpfile);

                    var quicktime = new Stopwatch();
                    quicktime.Restart();
                    while(quicktime.ElapsedMilliseconds <= 30_000)
                    {
                        var newoffereddata = await SwitchConnection.ReadBytesAsync(OfferedPokemon, 0x104, token);
                        var newofferedpbm = new PB7(newoffereddata);
                        if(SearchUtil.HashByDetails(offeredpbm) != SearchUtil.HashByDetails(newofferedpbm))
                        {
                            writepoke = newofferedpbm.EncryptedBoxData;
                            tpfile = $"{System.IO.Path.GetTempPath()}//{newofferedpbm.FileName}";
                            System.IO.File.WriteAllBytes(tpfile, writepoke);
                            await user.discordcontext.User.SendFileAsync(tpfile, "here is the pokemon you showed me");
                            System.IO.File.Delete(tpfile);
                            offeredpbm = newofferedpbm;
                        }

                    }
                    await user.discordcontext.User.SendMessageAsync("Time is up!");
                    await Click(B, 1000, token);
                    await Click(A, 1000, token);
                    Log("spamming b to get back to overworld");
                    read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                   
                    while (read[0] != overworld)
                    {

                        await Click(B, 1000, token);
                        read = await SwitchConnection.ReadBytesMainAsync(ScreenOff, 1, token);
                     
                    }
                    await Click(B, 1000, token);
                    await Click(B, 1000, token);
                    Log("done spamming b");
          
                    initialloop++;
                    await Task.Delay(2500);
                    continue;
                }
            }
        }
        public static Bitmap Merge(System.Drawing.Image firstImage, System.Drawing.Image secondImage, System.Drawing.Image thirdImage)
        {
            if (firstImage == null)
            {
                throw new ArgumentNullException("firstImage");
            }

            if (secondImage == null)
            {
                throw new ArgumentNullException("secondImage");
            }

            if (thirdImage == null)
            {
                throw new ArgumentNullException("thirdImage");
            }

            int outputImageWidth = firstImage.Width + 20;

            int outputImageHeight = firstImage.Height - 65;

            Bitmap outputImage = new Bitmap(outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(outputImage))
            {
                graphics.DrawImage(firstImage, new Rectangle(0, 0, firstImage.Width, firstImage.Height),
                    new Rectangle(new Point(), firstImage.Size), GraphicsUnit.Pixel);
                graphics.DrawImage(secondImage, new Rectangle(50, 0, secondImage.Width, secondImage.Height),
                    new Rectangle(new Point(), secondImage.Size), GraphicsUnit.Pixel);
                graphics.DrawImage(thirdImage, new Rectangle(100, 0, thirdImage.Width, thirdImage.Height),
                    new Rectangle(new Point(), thirdImage.Size), GraphicsUnit.Pixel);
            }

            return outputImage;
        }
        public enum pictocodes
        {
            Pikachu,
            Eevee, 
            Bulbasaur,
            Charmander,
            Squirtle,
            Pidgey,
            Caterpie,
            Rattata,
            Jigglypuff,
            Diglett
        }
        public enum commandtype
        {
            trade,
            dump,
            clone
        }
        public static string GetLegalizationHint(IBattleTemplate set, ITrainerInfo sav, PKM pk) => set.SetAnalysis(sav, pk);
    }
}

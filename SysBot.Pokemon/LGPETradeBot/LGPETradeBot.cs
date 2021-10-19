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

    public class LetsGoTrades : PokeRoutineExecutor
    {
        
        
        public static SAV7b sav = new();
        public static PB7 pkm = new();
        public static PokeTradeHub<PK8> Hub;
        public static Queue discordname = new();
        public static Queue Channel = new();
        public static Queue discordID = new();
        public static Queue tradepkm = new();
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
         
            uint GetBoxOffset(int box) => 0x533675B0;
            uint GetSlotOffset(int box, int slot) => GetBoxOffset(box) + (uint)((SlotSize + GapSize) * slot);
            Random dpoke = new Random();
            while (!token.IsCancellationRequested)
            {
                int waitCounter = 0;
                while (tradepkm.Count == 0 && !Hub.Config.TradeBot.distribution)
                {
                    
                   
                    if (waitCounter == 0)
                        Log("Nothing to check, waiting for new users...") ;
                    waitCounter++;
                    await Task.Delay(1_000, token).ConfigureAwait(false);

                }
                while (tradepkm.Count == 0 && Hub.Config.TradeBot.distribution)
                {
                    Log("Starting Distribution");
                    var dcode = new List<pictocodes>();
                    for (int i = 0; i <= 2; i++)
                    {

                        dcode.Add(pictocodes.Pikachu);

                    }
                    var dspecies = dpoke.Next(151);
                    ShowdownSet set = new ShowdownSet($"{(Species)dspecies}\nShiny: Yes");
                    var dpkm = (PB7)sav.GetLegalFromSet(set, out _);
                    dpkm.OT_Name = "Piplup.net";

                    var dslotofs = GetSlotOffset(1, 0);
                    var dStoredLength = SlotSize - 0x1C;
                    await Connection.WriteBytesAsync(dpkm.EncryptedBoxData.Slice(0, dStoredLength), BoxSlot1, token);
                    await Connection.WriteBytesAsync(dpkm.EncryptedBoxData.SliceEnd(dStoredLength), (uint)(dslotofs + dStoredLength + 0x70), token);


                    await Click(X, 200, token).ConfigureAwait(false);
                    await Task.Delay(1000).ConfigureAwait(false);
                    await SetStick(SwitchStick.RIGHT, 30000, 0, 100, token).ConfigureAwait(false);
                    await SetStick(SwitchStick.RIGHT, 0, 0, 100, token).ConfigureAwait(false);
                    await Task.Delay(1000);
                    await Click(A, 200, token).ConfigureAwait(false);
                    await Task.Delay(1000);
                    await Click(A, 200, token).ConfigureAwait(false);
                    await Task.Delay(5000).ConfigureAwait(false);
                    await SetStick(SwitchStick.RIGHT, 0, -30000, 100, token).ConfigureAwait(false);
                    await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                    await Click(A, 200, token).ConfigureAwait(false);
                    await Task.Delay(10000).ConfigureAwait(false);
                    await Click(A, 200, token).ConfigureAwait(false);
                    await Task.Delay(1000).ConfigureAwait(false);
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
                            for (int m = 0; m < 10; m++)
                            {
                                await Click(B, 200, token);
                                await Task.Delay(500);


                            }
                        }
                    }
                    if (dnofind == true)
                        continue;
                    await Task.Delay(10000);
                   
                    await Click(A, 200, token).ConfigureAwait(false);
                    await Task.Delay(1000);
                    await Click(A, 200, token).ConfigureAwait(false);
                
                    Log("waiting on trade screen");
                    await Task.Delay(3_000).ConfigureAwait(false);


                    await Task.Delay(15_000);
                    await Click(A, 200, token).ConfigureAwait(false);
                   
                    await Task.Delay(10000);
                    while (await LGIsInTrade(token))
                        await Task.Delay(25);
                    await Task.Delay(10000);
                    Log("Trade should be completed");
                    await Click(B, 200, token);
                    await Task.Delay(500);
                    await Click(A, 200, token).ConfigureAwait(false);
                    await Task.Delay(500);

                    btimeout.Restart();
                    int dacount = 3;
                    Log("spamming b to get back to overworld");
                    while (btimeout.ElapsedMilliseconds <= 20_000)
                    {

                        await Click(B, 200, token).ConfigureAwait(false);
                        await Task.Delay(1000).ConfigureAwait(false);
                        if (dacount == 4)
                        {
                            await Click(A, 200, token);
                            await Task.Delay(500);
                            dacount = 0;
                            continue;
                        }
                        dacount++;
                    }
                    await Task.Delay(500);
                    await Click(B, 200, token).ConfigureAwait(false);
                    await Task.Delay(500);
                    await Click(B, 200, token).ConfigureAwait(false);
                    await Task.Delay(500);
                    await Click(B, 200, token).ConfigureAwait(false);
                    continue;
                }
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
                var user = (IUser)discordname.Peek();
                await user.SendMessageAsync($"My IGN is {Connection.Label.Split('-')[0]}\nHere is your link code:");
                await user.SendFileAsync($"{System.IO.Directory.GetCurrentDirectory()}//finalcode.png", $"{code[0]}, {code[1]}, {code[2]}");
                var pkm = (PB7)tradepkm.Peek();
                var slotofs = GetSlotOffset(1, 0);
                var StoredLength = SlotSize- 0x1C;
                await Connection.WriteBytesAsync(pkm.EncryptedBoxData.Slice(0, StoredLength), BoxSlot1,token);
                await Connection.WriteBytesAsync(pkm.EncryptedBoxData.SliceEnd(StoredLength), (uint)(slotofs + StoredLength + 0x70), token);

                await Click(X, 200, token).ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
               await SetStick(SwitchStick.RIGHT, 30000, 0, 100, token).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT, 0, 0, 100, token).ConfigureAwait(false);
                await Task.Delay(1000);
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(1000);
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(5000).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT,0,-30000, 100, token).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(10000).ConfigureAwait(false);
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
                Log("Entering Link Code");
                System.IO.File.WriteAllBytes($"{System.IO.Directory.GetCurrentDirectory()}/Block.png", BlackPixel);
                foreach (pictocodes pc in code)
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
                Log($"Searching for user {discordname.Peek()}");
                await user.SendMessageAsync("searching for you now, you have 45 seconds to match").ConfigureAwait(false);
                await Task.Delay(3000);
                btimeout.Restart();
            
                var nofind = false;
                while (await LGIsinwaitingScreen(token))
                {
                    await Task.Delay(100);
                    if (btimeout.ElapsedMilliseconds >= 45_000)
                    {
                        await user.SendMessageAsync("I could not find you, please try again!");
                        Log("User not found");
                        nofind = true;
                        for (int m = 0; m < 10; m++)
                        {
                            await Click(B, 200, token);
                            await Task.Delay(500);


                        }
                    }
                }
                if (nofind)
                {
                    System.IO.File.Delete($"{System.IO.Directory.GetCurrentDirectory()}/Block.png");
                    discordID.Dequeue();
                    discordname.Dequeue();
                    Channel.Dequeue();
                    tradepkm.Dequeue();
                    continue;

                }

                Log("User Found");
                await Task.Delay(10000);
                System.IO.File.Delete($"{System.IO.Directory.GetCurrentDirectory()}/Block.png");
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(1000);
                await Click(A, 200, token).ConfigureAwait(false);
                await user.SendMessageAsync("You have 15 seconds to select your trade pokemon");
                Log("waiting on trade screen");
     
                await Task.Delay(15_000).ConfigureAwait(false);
               
      
                await Click(A, 200, token).ConfigureAwait(false);
                await user.SendMessageAsync("trading...");
                Log("trading...");
                await Task.Delay(10000);
                while (await LGIsInTrade(token))
                    await Task.Delay(25);
                await Task.Delay(10000);
                Log("Trade should be completed");
                await Click(B, 200, token);
                await Task.Delay(1000);
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(1000);
                await Click(B, 200, token);
                await Task.Delay(1000);
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(1000);
                btimeout.Restart();
                int acount = 3;
                Log("spamming b to get back to overworld");
                while (btimeout.ElapsedMilliseconds <= 30_000)
                {
                    
                   await Click(B, 200, token).ConfigureAwait(false);
                   await Task.Delay(1000).ConfigureAwait(false);
                    if(acount == 4)
                    {
                        await Click(A, 200, token);
                        await Task.Delay(1000);
                       acount = 0;
                        continue;
                    }
                   acount++;
                }
                await Task.Delay(500);
                await Click(B, 200, token).ConfigureAwait(false);
                await Task.Delay(500);
                await Click(B, 200, token).ConfigureAwait(false);
                await Task.Delay(500);
                await Click(B, 200, token).ConfigureAwait(false);
             

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
                    await user.SendFileAsync(tpfile, "here is the pokemon you traded me");
                    Log($"{discordname.Peek()} completed their trade");
               }
                else
                {
                    await user.SendMessageAsync("Something went wrong, no trade happened, please try again!");
                    Log($"{discordname.Peek()} did not complete their trade");
                }
                discordID.Dequeue();
                discordname.Dequeue();
                Channel.Dequeue();
                tradepkm.Dequeue();
                continue;

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

            int outputImageWidth = firstImage.Width + secondImage.Width + thirdImage.Width + 2;

            int outputImageHeight = firstImage.Height;

            Bitmap outputImage = new Bitmap(outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(outputImage))
            {
                graphics.DrawImage(firstImage, new Rectangle(0,0, firstImage.Width,firstImage.Height),
                    new Rectangle(new Point(), firstImage.Size), GraphicsUnit.Pixel);
                graphics.DrawImage(secondImage, new Rectangle(50, 0, secondImage.Width,secondImage.Height),
                    new Rectangle(new Point(), secondImage.Size), GraphicsUnit.Pixel);
                graphics.DrawImage(thirdImage, new Rectangle(100, 0, thirdImage.Width,thirdImage.Height),
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
    }
}

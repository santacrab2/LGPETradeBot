﻿using PKHeX.Core;
using SysBot.Base;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;
using System.Collections;
using System.Collections.Generic;
using Discord;

namespace SysBot.Pokemon
{
    public class LetsGoTrades : PokeRoutineExecutor
    {
        public static PB7 pkm = new();
        private readonly PokeTradeHub<PK8> Hub;
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
            Log("Identifying trainer data of the host console.");
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);

            Log("Starting main TradeBot loop.");
            while (!token.IsCancellationRequested)
            {
                Config.IterateNextRoutine();
                var task = Config.CurrentRoutineType switch
                {
                    
                    _ => DoTrades(token)
                };
                await task.ConfigureAwait(false);
            }
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

        public async Task DoTrades(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                int waitCounter = 0;
                while (tradepkm.Count == 0)
                {
                    
                   
                    if (waitCounter == 0)
                        Log("Nothing to check, waiting for new users...") ;
                    waitCounter++;
                    await Task.Delay(1_000, token).ConfigureAwait(false);
                    
                }
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(3000).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT,0,-30000, 100, token).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(3000).ConfigureAwait(false);
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
                var code = new List<pictocodes>();
                for(int i = 0; i <= 2; i++)
                {
                    code.Add((pictocodes)Util.Rand.Next(10));
                    
                }
                System.Text.StringBuilder strbui = new System.Text.StringBuilder();
                foreach(pictocodes t in code)
                {
                    strbui.Append($"{t}, ");
                }
                await ((IMessageChannel)Channel.Peek()).SendMessageAsync($"Here is your link code: {strbui}\n My IGN is {Connection.Label.Split('-')[0]}");
                foreach(pictocodes pc in code)
                {
                    if((int)pc > 4)
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
                        for (int i = (int)pc-5; i > 0; i--)
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
                        for (int i = (int)pc-5; i > 0; i--)
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
               
            }
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

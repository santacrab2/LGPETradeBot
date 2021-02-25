﻿using PKHeX.Core;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public class DynamaxAdventureBot : PokeRoutineExecutor
    {
        private readonly PokeTradeHub<PK8> Hub;
        private readonly BotCompleteCounts Counts;
        private readonly IDumper DumpSetting;
        private readonly int[] DesiredIVs;
        private readonly byte[] BattleMenuReady = { 0, 0, 0, 255 };

        public DynamaxAdventureBot(PokeBotState cfg, PokeTradeHub<PK8> hub) : base(cfg)
        {
            Hub = hub;
            Counts = Hub.Counts;
            DumpSetting = Hub.Config.Folder;
            DesiredIVs = StopConditionSettings.InitializeTargetIVs(Hub);
        }

        private int encounterCount;

        public override async Task MainLoop(CancellationToken token)
        {
            Log("Identifying trainer data of the host console.");
            await IdentifyTrainer(token).ConfigureAwait(false);

            Log("Starting main EncounterBot loop.");
            Config.IterateNextRoutine();

            // Clear out any residual stick weirdness.
            await ResetStick(token).ConfigureAwait(false);

            var task = DoDynamaxAdventure(token);
            await task.ConfigureAwait(false);

            await ResetStick(token).ConfigureAwait(false);
            await DetachController(token).ConfigureAwait(false);
        }

        private async Task DoDynamaxAdventure(CancellationToken token)
        {
            //Initialization
            int adventureCompleted = 0;
            string mon = Hub.Config.StopConditions.StopOnSpecies.ToString();
            byte[] demageStandardState = BitConverter.GetBytes(0x7900E808);
            byte[] demageAlteredState = BitConverter.GetBytes(0x7900E81F);
            byte[] demageTemporalState;
            ulong mainbase = await SwitchConnection.GetMainNsoBaseAsync(token).ConfigureAwait(false);
            bool wasVideoClipActive = Hub.Config.StopConditions.CaptureVideoClip;

            //Check/set target parameters
            ushort searchmon;
            if (Enum.IsDefined(typeof(LairSpecies), mon))
                searchmon = (ushort)Enum.Parse(typeof(LairSpecies), mon);
            else
                searchmon = 0;

            byte[] current = await Connection.ReadBytesAsync(LairSpeciesSelector, 2, token).ConfigureAwait(false);
            byte[] wanted = BitConverter.GetBytes(searchmon);
            Log("Current Lair Boss bytes: " + String.Join(" ", current));
            if (mon != "None" && current != wanted && !Enum.IsDefined(typeof(LairSpecies), mon))
            {
                Log(mon + " is not an available Lair Boss species. Check your configurations and restart the bot.");
                return;
            }
            else if (mon != "None" && current != wanted && Enum.IsDefined(typeof(LairSpecies), mon))
            {
                await Connection.WriteBytesAsync(wanted, LairSpeciesSelector, token);
                Log(String.Join(" ", await Connection.ReadBytesAsync(LairSpeciesSelector, 2, token).ConfigureAwait(false)) + " (" + mon + ") ready to be hunted.");
            }
            else if (mon == "None")
            {
                Log("(Any) Legendary ready to be hunted.");
            }

            //Check ShinyXOR
            if (Hub.Config.StopConditions.ShinyTarget.ToString() == "SquareOnly")
            {
                Log("Lair Pokémon cannot be Square Shiny! Forced XOR=1. Check your settings and restart the bot.");
                return;
            }

            while (!token.IsCancellationRequested)
            {
                //Capture video clip is menaged internally
                if (Hub.Config.StopConditions.CaptureVideoClip == true)
                    Hub.Config.StopConditions.CaptureVideoClip = false;

                //Talk to the Lady
                while (!await IsInLairWait(token).ConfigureAwait(false))
                    await Click(A, 1_000, token).ConfigureAwait(false);

                //Select Solo Adventure
                await Click(DDOWN, 0_800, token).ConfigureAwait(false);
                await Click(A, 1_000, token).ConfigureAwait(false);

                //MAIN LOOP
                int raidCount = 1;
                bool inBattle = false;
                bool lost = false;
                while (!(await IsInLairEndList(token) || lost))
                {
                    await Click(A, 1_000, token).ConfigureAwait(false);
                    //IsInOverworld seems to be non working?
                    if (await IsOnOverworld(Hub.Config, token).ConfigureAwait(false))
                    {
                        lost = true;
                        //Also if the pg was in the overworld, this line was never printed, and the bot continued to loop.
                        Log("Lost at first raid.");
                    }
                    else if (!await IsInBattle(token).ConfigureAwait(false) && inBattle)
                        inBattle = false;
                    else if (await IsInBattle(token).ConfigureAwait(false) && !inBattle)
                    {
                        //Allows 1HKO
                        demageTemporalState = await SwitchConnection.ReadBytesMainAsync(demageOutputOffset, 4, token).ConfigureAwait(false);
                        if (demageStandardState.SequenceEqual(demageTemporalState))
                            await SwitchConnection.WriteBytesAbsoluteAsync(demageAlteredState, mainbase + demageOutputOffset, token).ConfigureAwait(false);

                        var pk = await ReadUntilPresent(RaidPokemonOffset, 2_000, 0_200, token).ConfigureAwait(false);
                        if (pk != null)
                            Log("Raid Battle " + raidCount + ": " + pk.Species.ToString() + " " + pk.Nickname);
                        else
                            Log("Raid Battle " + raidCount);

                        inBattle = true;
                        raidCount++;
                    }
                    else if (!await IsInBattle(token).ConfigureAwait(false) && !inBattle)
                    {
                        //Disable 1HKO
                        demageTemporalState = await SwitchConnection.ReadBytesMainAsync(demageOutputOffset, 4, token).ConfigureAwait(false);
                        if (demageAlteredState.SequenceEqual(demageTemporalState))
                            await SwitchConnection.WriteBytesAbsoluteAsync(demageStandardState, mainbase + demageOutputOffset, token).ConfigureAwait(false);
                    }
                }

                if (!lost) adventureCompleted++;
                if (raidCount < 4)
                    Log("Lost at battle n. " + raidCount + ", adventure n. " + adventureCompleted + ".");
                else
                    Log("Adventure n. " + adventureCompleted + " completed.");

                //Read data from dynamic pointers
                var pk1 = await ReadUntilPresent(await ParsePointer("[[[[main+28F4060]+1B0]+68]+58]+D0", token), 2_000, 0_200, token).ConfigureAwait(false);
                var pk2 = await ReadUntilPresent(await ParsePointer("[[[[main+28F4060]+1B0]+68]+60]+D0", token), 2_000, 0_200, token).ConfigureAwait(false);
                var pk3 = await ReadUntilPresent(await ParsePointer("[[[[main+28F4060]+1B0]+68]+68]+D0", token), 2_000, 0_200, token).ConfigureAwait(false);
                var pk4 = await ReadUntilPresent(await ParsePointer("[[[[main+28F4060]+1B0]+68]+70]+D0", token), 2_000, 0_200, token).ConfigureAwait(false);

                //Check for shinies, check all the StopConditions for the Legendary
                int found = 0;
                if (pk1 != null)
                {
                    await HandleEncounter(pk1, false, token).ConfigureAwait(false);
                    if (pk1.IsShiny)
                        found = 1;
                }
                if (pk2 != null)
                {
                    await HandleEncounter(pk2, false, token).ConfigureAwait(false);
                    if (pk2.IsShiny)
                        found = 2;
                }
                if (pk3 != null)
                {
                    await HandleEncounter(pk3, false, token).ConfigureAwait(false);
                    if (pk3.IsShiny)
                        found = 3;
                }
                if (pk4 != null)
                {
                    if (await HandleEncounter(pk4, true, token).ConfigureAwait(false))
                        found = 4;
                }

                //Ending routine
                if (found > 0)
                {
                    Log("A Shiny Pokémon has been found!");
                    await Task.Delay(1_500, token).ConfigureAwait(false);
                    for (int y = 1; y < found; y++)
                        await Click(DDOWN, 1_000, token).ConfigureAwait(false);
                    await Click(A, 1_200, token).ConfigureAwait(false);
                    await Click(DDOWN, 0_800, token).ConfigureAwait(false);
                    await Click(A, 2_300, token).ConfigureAwait(false);
                    await PressAndHold(CAPTURE, 2_000, 10_000, token).ConfigureAwait(false);
                    if (wasVideoClipActive == true)
                        Hub.Config.StopConditions.CaptureVideoClip = true;
                    if (found == 4)
                        return;
                    else
                    {
                        await Task.Delay(1_500, token).ConfigureAwait(false);
                        await Click(B, 1_500, token).ConfigureAwait(false);
                        while (!await IsOnOverworld(Hub.Config, token).ConfigureAwait(false))
                            await Click(A, 0_800, token).ConfigureAwait(false);
                    }
                }
                else
                {
                    Log("No result found, starting again");
                    await Task.Delay(1_500, token).ConfigureAwait(false);
                    if (!lost)
                        await Click(B, 1_000, token).ConfigureAwait(false);
                    while (!await IsOnOverworld(Hub.Config, token).ConfigureAwait(false))
                        await Click(A, 0_800, token).ConfigureAwait(false);
                }
            }
        }

        private async Task<bool> HandleEncounter(PK8 pk, bool legends, CancellationToken token)
        {
            encounterCount++;

            //Star/Square Shiny Recognition
            var showdowntext = ShowdownParsing.GetShowdownText(pk);
            if (pk.IsShiny && pk.ShinyXor == 0)
                showdowntext = showdowntext.Replace("Shiny: Yes", "Shiny: Square");
            else if (pk.IsShiny)
                showdowntext = showdowntext.Replace("Shiny: Yes", "Shiny: Star");

            Log($"Encounter: {encounterCount}{Environment.NewLine}{Environment.NewLine}{showdowntext}{Environment.NewLine}");
            if (legends)
                Counts.AddCompletedLegends();
            else
                Counts.AddCompletedEncounters();

            if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                DumpPokemon(DumpSetting.DumpFolder, legends ? "legends" : "encounters", pk);

            if (StopConditionSettings.EncounterFound(pk, DesiredIVs, Hub.Config.StopConditions))
            {
                Log("Result found! Stopping routine execution; restart the bot(s) to search again.");
                if (Hub.Config.StopConditions.CaptureVideoClip)
                {
                    await Task.Delay(Hub.Config.StopConditions.ExtraTimeWaitCaptureVideo, token).ConfigureAwait(false);
                    await PressAndHold(CAPTURE, 2_000, 1_000, token).ConfigureAwait(false);
                }
                return true;
            }
            return false;
        }

        private async Task ResetStick(CancellationToken token)
        {
            // If aborting the sequence, we might have the stick set at some position. Clear it just in case.
            await SetStick(LEFT, 0, 0, 0_500, token).ConfigureAwait(false); // reset
        }

        private async Task FleeToOverworld(CancellationToken token)
        {
            try
            {
                Log("Start flee");
                // This routine will always escape a battle.
                await Task.Delay(1_000, token).ConfigureAwait(false);
                await Click(DUP, 0_400, token).ConfigureAwait(false);
                await Click(A, 0_400, token).ConfigureAwait(false);
                await Click(B, 0_400, token).ConfigureAwait(false);
                await Click(B, 0_400, token).ConfigureAwait(false);
                Log("End flee");
            }
            catch (Exception)
            {
                Log("Stuck in there!");
            }
        }

        public enum LairSpecies : ushort
        {
            Articuno = 144,
            Zapdos = 145,
            Moltres = 146,
            Mewtwo = 150,
            Raikou = 243,
            Entei = 244,
            Suicune = 245,
            Lugia = 249,
            HoOh = 250,
            Latias = 380,
            Latios = 381,
            Kyogre = 382,
            Groudon = 383,
            Rayquaza = 384,
            Uxie = 480,
            Mesprit = 481,
            Azelf = 482,
            Dialga = 483,
            Palkia = 484,
            Heatran = 485,
            Giratina = 487,
            Cresselia = 488,
            Tornadus = 641,
            Thundurus = 642,
            Landorus = 645,
            Reshiram = 643,
            Zekrom = 644,
            Kyurem = 646,
            Xerneas = 716,
            Yveltal = 717,
            Zygarde = 718,
            TapuKoko = 785,
            TapuLele = 786,
            TapuBulu = 787,
            TapuFini = 788,
            Solgaleo = 791,
            Lunala = 792,
            Nihilego = 793,
            Buzzwole = 794,
            Pheromosa = 795,
            Xurkitree = 796,
            Celesteela = 797,
            Kartana = 798,
            Guzzlord = 799,
            Necrozma = 800,
            Stakataka = 805,
            Blacephalon = 806
        }
    }
}
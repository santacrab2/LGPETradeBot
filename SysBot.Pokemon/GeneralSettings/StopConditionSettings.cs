﻿using PKHeX.Core;
using System;
using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class StopConditionSettings
    {
        private const string StopConditions = nameof(StopConditions);
        public override string ToString() => "Stop Condition Settings";

        [Category(StopConditions), Description("Stops only on Pokémon of this species. No restrictions if set to \"None\".")]
        public Species StopOnSpecies { get; set; }

        [Category(StopConditions), Description("Stop only on Pokémon of the specified nature.")]
        public Nature TargetNature { get; set; } = Nature.Random;

        [Category(StopConditions), Description("Minimum accepted IVs in the format HP/Atk/Def/SpA/SpD/Spe. Use \"x\" for unchecked IVs and \"/\" as a separator.")]
        public string TargetMinIVs { get; set; } = "";

        [Category(StopConditions), Description("Maximum accepted IVs in the format HP/Atk/Def/SpA/SpD/Spe. Use \"x\" for unchecked IVs and \"/\" as a separator.")]
        public string TargetMaxIVs { get; set; } = "";
        [Category(StopConditions), Description("Selects the shiny type to stop on.")]
        public TargetShinyType ShinyTarget { get; set; } = TargetShinyType.DisableOption;

        [Category(StopConditions), Description("If set to \"Any\", the bot will target a Pokémon that has any of the Marks listed. Select a certain Mark if you're hunting for it specifically. No restrictions if set to \"None\". Ignored for LGPE encounters.")]
        public MarkIndex MarkTarget { get; set; } = MarkIndex.None;

        [Category(StopConditions), Description("Holds Capture button to record a 30 second clip when a matching Pokémon is found by EncounterBot or Fossilbot.")]
        public bool CaptureVideoClip { get; set; }

        [Category(StopConditions), Description("Extra time in milliseconds to wait after an encounter is matched before pressing Capture for EncounterBot or Fossilbot.")]
        public int ExtraTimeWaitCaptureVideo { get; set; } = 10000;

        [Category(StopConditions), Description("If set to TRUE, matches both ShinyTarget and TargetIVs settings. Otherwise, looks for either ShinyTarget or TargetIVs match.")]
        public bool MatchShinyAndIV { get; set; } = true;

        public static bool EncounterFound(PKM pk, int[] targetminIVs, int[] targetmaxIVs, StopConditionSettings settings)
        {
            // Match Nature and Species if they were specified.
            if (settings.StopOnSpecies != Species.None && settings.StopOnSpecies != (Species)pk.Species)
                return false;

            if (settings.TargetNature != Nature.Random && settings.TargetNature != (Nature)pk.Nature)
                return false;

            //If target is Any Mark then do the standard routine otherwise check for a specific Marker
            if (pk is PK8)
            {
                PK8 pk8 = new PK8(pk.Data);
                if ((settings.MarkTarget == MarkIndex.Any && !HasMark(pk8, settings.MarkTarget, false)) ||
                    (settings.MarkTarget != MarkIndex.None && settings.MarkTarget != MarkIndex.Any && !HasMark(pk8, settings.MarkTarget, true)))
                    return false;
            }

            if (settings.ShinyTarget != TargetShinyType.DisableOption)
            {
                bool shinymatch = settings.ShinyTarget switch
                {
                    TargetShinyType.AnyShiny => pk.IsShiny,
                    TargetShinyType.NonShiny => !pk.IsShiny,
                    TargetShinyType.StarOnly => pk.IsShiny && pk.ShinyXor != 0,
                    TargetShinyType.SquareOnly => pk.ShinyXor == 0,
                    TargetShinyType.DisableOption => true,
                    _ => throw new ArgumentException(nameof(TargetShinyType)),
                };

                // If we only needed to match one of the criteria and it shinymatch'd, return true.
                // If we needed to match both criteria and it didn't shinymatch, return false.
                if (!settings.MatchShinyAndIV && shinymatch)
                    return true;
                if (settings.MatchShinyAndIV && !shinymatch)
                    return false;
            }

            int[] pkIVList = pk.IVs;
            

            for (int i = 0; i < 6; i++)
            {
                // Match all 0's.
                if (targetminIVs[i] > pkIVList[i] || targetmaxIVs[i] < pkIVList[i])
                    return false;
            }
            return true;
        }

        public static void InitializeTargetIVs(PokeTradeHub<PK8> hub, out int[] min, out int[] max)
        {
            min = ReadTargetIVs(hub.Config.StopConditions, true);
            max = ReadTargetIVs(hub.Config.StopConditions, false);
            return;
        }

        private static int[] ReadTargetIVs(StopConditionSettings settings, bool min)
        {
            int[] targetIVs = new int[6];

            string[] splitIVs = min
                ? settings.TargetMinIVs.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                : settings.TargetMaxIVs.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            // Only accept up to 6 values.  Fill it in with default values if they don't provide 6.
            // Anything that isn't an integer will be a wild card.
            for (int i = 0; i < 6; i++)
            {
                if (i < splitIVs.Length)
                {
                    var str = splitIVs[i];
                    if (int.TryParse(str, out var val))
                    {
                        targetIVs[i] = val;
                        continue;
                    }
                }
                targetIVs[i] = min ? 0 : 31;
            }
            return targetIVs;
        }

        private static bool HasMark(IRibbonIndex pk, MarkIndex target, bool specific)
        {
            if (!specific) {
                for (var mark = RibbonIndex.MarkLunchtime; mark <= RibbonIndex.MarkSlump; mark++)
                    if ((!specific && pk.GetRibbon((int)mark)) || (specific && pk.GetRibbon((int)mark) && mark.Equals(target)))
                        return true;
            }
            else if (specific && pk.GetRibbon((int)target))
                return true;

            return false;
        }
    }
}

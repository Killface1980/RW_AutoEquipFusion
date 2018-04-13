namespace OutfitterInfused
{
    using System.Collections.Generic;
    using System.Linq;
    using Infused;
    using Outfitter;
    using RimWorld;
    using Verse;
    using Def = Infused.Def;

    public class GameComponent_OutfitterInfused : GameComponent
    {
        private readonly Game _game;

        public GameComponent_OutfitterInfused()
        {
            //
        }

        public GameComponent_OutfitterInfused(Game game)
        {
            _game = game;
            Log.Message("Outfitter with Infused Initialized");
            ApparelStatCache.ApparelScoreRaw_PawnStatsHandlers += ApparelScoreRaw_PawnStatsHandlers;
            ApparelStatCache.ApparelScoreRaw_FillInfusedStat   += ApparelScoreRaw_FillInfusedStat;
            ApparelStatCache.IgnoredWtHandlers                += Ignored_WTHandlers;
        }

        private static void ApparelScoreRaw_FillInfusedStat(
            Apparel              apparel,
            StatDef              parentStat,
            ref HashSet<StatDef> infusedOffsets)
        {
            if (apparel.TryGetInfusions(out InfusionSet inf))
            {
                Def prefix = inf.prefix;
                Def suffix = inf.suffix;

                if (prefix != null && prefix.TryGetStatValue(parentStat, out _))
                {
                    infusedOffsets.Add(parentStat);
                }

                if (suffix != null && suffix.TryGetStatValue(parentStat, out _))
                {
                    infusedOffsets.Add(parentStat);
                }
            }
        }

        private static void ApparelScoreRaw_PawnStatsHandlers(Apparel apparel, StatDef statPriority, out float val)
        {
            val = 0f;

            // string log = "Infused: " + apparel + " - " + statPriority + " - " + val;
            if (apparel.TryGetInfusions(out InfusionSet inf))
            {
                Def prefix = inf.prefix;
                Def suffix = inf.suffix;

                float statInfusedPrefix = 0f;
                float statInfusedSuffix = 0f;

                if (prefix != null && prefix.TryGetStatValue(statPriority, out StatMod mod))
                {
                    statInfusedPrefix += mod.offset;
                    statInfusedPrefix += mod.multiplier - 1;

                    // log += "\nprefix - " + mod.offset + " - " + mod.multiplier;
                }

                if (suffix != null && suffix.TryGetStatValue(statPriority, out mod))
                {
                    statInfusedSuffix += mod.offset;
                    statInfusedSuffix += mod.multiplier - 1;

                    // log += "\nsuffix - " + mod.offset + " - " + mod.multiplier;
                }

                // Log.Message(log);
                val += statInfusedPrefix + statInfusedSuffix;
            }
        }

        private static void Ignored_WTHandlers(ref List<StatDef> allApparelStats)
        {
            // add all stat modifiers from all infusions
            foreach (KeyValuePair<StatDef, StatMod> mod in DefDatabase<Def>.AllDefsListForReading.SelectMany(
                                                                                                             infusion =>
                                                                                                                 infusion
                                                                                                                    .stats)
            )
            {
                if (!allApparelStats.Contains(mod.Key))
                {
                    allApparelStats.Add(mod.Key);
                }
            }
        }
    }
}
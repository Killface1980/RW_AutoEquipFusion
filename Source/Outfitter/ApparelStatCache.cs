// Outfitter/StatCache.cs
//
// Copyright Karel Kroeze, 2016.
//
// Created 2016-01-02 13:58

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Outfitter.Enums;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitter
{
    public class ApparelStatCache
    {
        public const float MaxValue = 2.5f;

        // public List<Apparel> recentApparel = new List<Apparel>();
        public readonly List<StatPriority> Cache;

        public Dictionary<Apparel, Pawn> ToDropList = new Dictionary<Apparel, Pawn>();

        private readonly Pawn _pawn;

        private readonly SaveablePawn _pawnSave;

        private int _lastStatUpdate;

        private int _lastTempUpdate;

        private int _lastWeightUpdate;

        public ApparelStatCache(Pawn pawn)
            : this(pawn.GetSaveablePawn())
        {
        }

        // public NeededWarmth neededWarmth;
        public ApparelStatCache([NotNull] SaveablePawn saveablePawn)
        {
            this._pawn = saveablePawn.Pawn;
            this._pawnSave = this._pawn.GetSaveablePawn();
            this.Cache = new List<StatPriority>();
            this._lastStatUpdate = -5000;
            this._lastTempUpdate = -5000;
            this._lastWeightUpdate = -5000;
        }

        public delegate void ApparelScoreRawIgnored_WtHandlers(ref List<StatDef> statDef);

        public delegate void ApparelScoreRawInfusionHandlers(
            [NotNull] Apparel apparel,
            [NotNull] StatDef parentStat,
            ref HashSet<StatDef> infusedOffsets);

        public delegate void ApparelScoreRawStatsHandler(Apparel apparel, StatDef statDef, out float num);

        public static event ApparelScoreRawInfusionHandlers ApparelScoreRawFillInfusedStat;

        public static event ApparelScoreRawStatsHandler ApparelScoreRawPawnStatsHandlers;

        public static event ApparelScoreRawIgnored_WtHandlers IgnoredWtHandlers;

        [NotNull]
        public List<StatPriority> StatCache
        {
            get
            {
                // update auto stat priorities roughly between every vanilla gear check cycle
                if (Find.TickManager.TicksGame - this._lastStatUpdate
                    > JobGiver_OutfitterOptimizeApparel.ApparelStatCheck || this._pawnSave.ForceStatUpdate)
                {
                    // list of auto stats
                    if (this.Cache.Count < 1 && this._pawnSave.Stats.Count > 0)
                    {
                        foreach (Saveable_Pawn_StatDef statDef in this._pawnSave.Stats)
                        {
                            this.Cache.Add(new StatPriority(statDef.Stat, statDef.Weight, statDef.Assignment));
                        }
                    }

                    this.RawScoreDict.Clear();
                    this._pawnSave.Stats.Clear();

                    // clear auto priorities
                    this.Cache.RemoveAll(stat => stat.Assignment == StatAssignment.Automatic);
                    this.Cache.RemoveAll(stat => stat.Assignment == StatAssignment.Individual);

                    // loop over each (new) stat
                    // Armor only used by the Battle beacon, no relevance to jobs etc.
                    Pawn thisPawn = this._pawn;
                    if (this._pawnSave.ArmorOnly)
                    {
                        Dictionary<StatDef, float> updateArmorStats = thisPawn.GetWeightedApparelArmorStats();
                        foreach (KeyValuePair<StatDef, float> pair in updateArmorStats)
                        {
                            // find index of existing priority for this stat
                            int i = this.Cache.FindIndex(stat => stat.Stat == pair.Key);

                            // if index -1 it doesnt exist yet, add it
                            if (i < 0)
                            {
                                StatPriority armorStats = new StatPriority(pair.Key, pair.Value);
                                this.Cache.Add(armorStats);
                            }
                            else
                            {
                                // it exists, make sure existing is (now) of type override.
                                this.Cache[i].Weight += pair.Value;
                            }
                        }
                    }
                    else
                    {
                        Dictionary<StatDef, float> updateAutoPriorities = thisPawn.GetWeightedApparelStats();
                        Dictionary<StatDef, float> updateIndividualPriorities =
                            thisPawn.GetWeightedApparelIndividualStats();

                        // updateAutoPriorities = updateAutoPriorities.OrderBy(x => x.Key.label).ToDictionary(x => x.Key, x => x.Value);
                        updateAutoPriorities = updateAutoPriorities.OrderByDescending(x => Mathf.Abs(x.Value))
                            .ToDictionary(x => x.Key, x => x.Value);
                        updateIndividualPriorities = updateIndividualPriorities.OrderBy(x => x.Key.label)
                            .ToDictionary(x => x.Key, x => x.Value);

                        foreach (KeyValuePair<StatDef, float> pair in updateIndividualPriorities)
                        {
                            // find index of existing priority for this stat
                            int i = this.Cache.FindIndex(stat => stat.Stat == pair.Key);

                            // if index -1 it doesnt exist yet, add it
                            if (i < 0)
                            {
                                StatPriority individual =
                                    new StatPriority(pair.Key, pair.Value, StatAssignment.Individual);
                                this.Cache.Add(individual);
                            }
                            else
                            {
                                // if exists, make sure existing is (now) of type override.
                                this.Cache[i].Assignment = StatAssignment.Override;
                            }
                        }

                        foreach (KeyValuePair<StatDef, float> pair in updateAutoPriorities)
                        {
                            // find index of existing priority for this stat
                            int i = this.Cache.FindIndex(stat => stat.Stat == pair.Key);

                            // if index -1 it doesnt exist yet, add it
                            if (i < 0)
                            {
                                this.Cache.Add(new StatPriority(pair));
                            }
                            else
                            {
                                // if exists, make sure existing is (now) of type override.
                                this.Cache[i].Assignment = StatAssignment.Override;
                            }
                        }
                    }

                    // update our time check.
                    this._lastStatUpdate = Find.TickManager.TicksGame;
                    this._pawnSave.ForceStatUpdate = false;
                    this._pawnSave.ArmorOnly = false;

                    foreach (StatPriority statPriority in this.Cache.Where(
                        statPriority => statPriority.Assignment != StatAssignment.Automatic
                                        && statPriority.Assignment != StatAssignment.Individual))
                    {
                        bool exists = false;
                        foreach (Saveable_Pawn_StatDef stat in this._pawnSave.Stats.Where(
                            stat => stat.Stat.Equals(statPriority.Stat)))
                        {
                            stat.Weight = statPriority.Weight;
                            stat.Assignment = statPriority.Assignment;
                            exists = true;
                        }

                        if (exists)
                        {
                            continue;
                        }

                        Saveable_Pawn_StatDef stats =
                            new Saveable_Pawn_StatDef
                                {
                                    Stat = statPriority.Stat,
                                    Assignment = statPriority.Assignment,
                                    Weight = statPriority.Weight
                                };
                        this._pawnSave.Stats.Add(stats);
                    }
                }

                return this.Cache;
            }
        }

        public FloatRange TargetTemperatures
        {
            get
            {
                this.UpdateTemperatureIfNecessary();
                return this._pawnSave.TargetTemperatures;
            }

            set
            {
                this._pawnSave.TargetTemperatures = value;
                this._pawnSave.TargetTemperaturesOverride = true;
            }
        }

        private FloatRange TemperatureWeight
        {
            get
            {
                this.UpdateTemperatureIfNecessary(false, true);
                return this._pawnSave.Temperatureweight;
            }
        }

        public static List<StatDef> SpecialStats =
            new List<StatDef>
                {
                    StatDefOf.MentalBreakThreshold,
                    StatDefOf.PsychicSensitivity,
                    StatDefOf.ToxicSensitivity
                };

        public static float ApparelScoreRaw_ProtectionBaseStat(Apparel ap)
        {
            float num = ap.GetStatValue(StatDefOf.ArmorRating_Sharp)
                         + ap.GetStatValue(StatDefOf.ArmorRating_Blunt) * 0.5f;

            return num * 0.1f;
        }

        public static void DoApparelScoreRaw_PawnStatsHandlers(
            [NotNull] Apparel apparel,
            [NotNull] StatDef statDef,
            out float num)
        {
            num = 0f;
            ApparelScoreRawPawnStatsHandlers?.Invoke(apparel, statDef, out num);
        }



        public static void FillIgnoredInfused_PawnStatsHandlers(ref List<StatDef> allApparelStats)
        {
            IgnoredWtHandlers?.Invoke(ref allApparelStats);
        }

        public float ApparelScoreRaw([NotNull] Apparel ap)
        {
            if (this.RawScoreDict.ContainsKey(ap))
            {
                return this.RawScoreDict[ap];
            }

            // only allow shields to be considered if a primary weapon is equipped and is melee
            Pawn thisPawn = this._pawn;

            if (ap.def.thingClass == typeof(ShieldBelt) && thisPawn.equipment.Primary?.def.IsRangedWeapon == true)
            {
                return -1f;
            }

            // Fail safe to prevent pawns get out of the regular temperature.
            // Might help making pawn drop equipped apparel if it's too cold/warm.
            // this.GetInsulationStats(ap, out float insulationCold, out float insulationHeat);
            // FloatRange temperatureRange = this.pawn.ComfortableTemperatureRange();
            // if (ap.Wearer != thisPawn)
            // {
            // temperatureRange.min += insulationCold;
            // temperatureRange.max += insulationHeat;
            // }
            // if (temperatureRange.min > 12 && insulationCold > 0 || temperatureRange.max < 32 && insulationHeat < 0)
            // {
            // return -3f;
            // }

            // relevant apparel stats
            ApparelEntry entry = this.GetAllOffsets(ap);

            HashSet<StatDef> statBases = entry.StatBases;
            HashSet<StatDef> equippedOffsets = entry.EquippedOffsets;
            HashSet<StatDef> infusedOffsets = entry.InfusedOffsets;

            // start score at 1
            float score = 1;

            // add values for each statdef modified by the apparel
            List<StatPriority> stats = thisPawn.GetApparelStatCache().StatCache;

            foreach (StatPriority statPriority in stats.Where(statPriority => statPriority != null))
            {
                StatDef stat = statPriority.Stat;

                if (statBases.Contains(stat))
                {
                    float apStat = ap.GetStatValue(stat);

                    if (SpecialStats.Contains(stat))
                    {
                        CalculateScoreForSpecialStats(ap, statPriority, thisPawn, apStat, ref score);
                    }
                    else
                    {
                        // add stat to base score before offsets are handled 
                        // (the pawn's apparel stat cache always has armors first as it is initialized with it).
                        score += apStat * statPriority.Weight;
                    }

                }

                // equipped offsets, e.g. movement speeds
                if (equippedOffsets.Contains(stat))
                {
                    float apStat = ap.GetEquippedStatValue(this._pawn, stat);

                    if (SpecialStats.Contains(stat))
                    {
                        CalculateScoreForSpecialStats(ap, statPriority, thisPawn, apStat, ref score);
                    }
                    else
                    {
                        score += apStat * statPriority.Weight;
                    }

                    // multiply score to favour items with multiple offsets
                    // score *= adjusted;

                    // debug.AppendLine( statWeightPair.Key.LabelCap + ": " + score );
                }

                // infusions
                if (infusedOffsets.Contains(stat))
                {
                    // float statInfused = StatInfused(infusionSet, statPriority, ref dontcare);
                    DoApparelScoreRaw_PawnStatsHandlers(ap, stat, out float statInfused);

                    if (SpecialStats.Contains(stat))
                    {
                        CalculateScoreForSpecialStats(ap, statPriority, thisPawn, statInfused, ref score);

                    }
                    else
                    {
                        // Bug with Infused and "Ancient", it completely kills the pawn's armor
                        if (statInfused < 0 && (stat == StatDefOf.ArmorRating_Blunt
                            || stat == StatDefOf.ArmorRating_Sharp))
                        {
                            score = -2f;
                            return score;
                        }

                        score += statInfused * statPriority.Weight;
                    }
                }
            }

            score += ap.GetSpecialApparelScoreOffset();

            score += ApparelScoreRaw_ProtectionBaseStat(ap);

            // offset for apparel hitpoints
            if (ap.def.useHitPoints)
            {
                float x = ap.HitPoints / (float)ap.MaxHitPoints;
                score *= ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x);
            }

            if (ap.WornByCorpse && ThoughtUtility.CanGetThought(thisPawn, ThoughtDefOf.DeadMansApparel))
            {
                score -= 0.5f;
                if (score > 0f)
                {
                    score *= 0.1f;
                }
            }

            if (ap.Stuff == ThingDefOf.Human.race.leatherDef)
            {
                if (ThoughtUtility.CanGetThought(thisPawn, ThoughtDefOf.HumanLeatherApparelSad))
                {
                    score -= 0.5f;
                    if (score > 0f)
                    {
                        score *= 0.1f;
                    }
                }

                if (ThoughtUtility.CanGetThought(thisPawn, ThoughtDefOf.HumanLeatherApparelHappy))
                {
                    score *= 2f;
                }
            }

            score *= this.ApparelScoreRaw_Temperature(ap);

            this.RawScoreDict.Add(ap, score);

            return score;
        }

        public static void CalculateScoreForSpecialStats(Apparel ap, StatPriority statPriority, Pawn thisPawn, float apStat, ref float score)
        {
            float current = thisPawn.GetStatValue(statPriority.Stat);
            float goal = statPriority.Weight;
            float defaultStat = thisPawn.def.GetStatValueAbstract(statPriority.Stat);

            if (thisPawn.story != null)
            {
                for (int k = 0; k < thisPawn.story.traits.allTraits.Count; k++)
                {
                    defaultStat += thisPawn.story.traits.allTraits[k].OffsetOfStat(statPriority.Stat);
                }

                for (int n = 0; n < thisPawn.story.traits.allTraits.Count; n++)
                {
                    defaultStat *= thisPawn.story.traits.allTraits[n].MultiplierOfStat(statPriority.Stat);
                }
            }

            if (ap.Wearer == thisPawn)
            {
                current -= apStat;
            }
            else
            {
                foreach (Apparel worn in thisPawn.apparel.WornApparel)
                {
                    if (!ApparelUtility.CanWearTogether(worn.def, ap.def, thisPawn.RaceProps.body))
                    {
                        float stat1 = worn.GetStatValue(statPriority.Stat);
                        float stat2 = worn.GetEquippedStatValue(thisPawn, statPriority.Stat);
                        DoApparelScoreRaw_PawnStatsHandlers(worn, statPriority.Stat, out float stat3);
                        current -= stat1 + stat2 + stat3;
                    }
                }
            }

            if (Math.Abs(current - goal) > 0.01f)
            {
                float need = 1f - Mathf.InverseLerp(defaultStat, goal, current);
                score += Mathf.InverseLerp(current, goal, current + apStat) * need;
            }
        }

        private static readonly SimpleCurve Curve =
            new SimpleCurve { new CurvePoint(-5f, 0.1f), new CurvePoint(0f, 1f), new CurvePoint(100f, 4f) };

        public Dictionary<Apparel, float> RawScoreDict = new Dictionary<Apparel, float>();

        public float ApparelScoreRaw_Temperature([NotNull] Apparel apparel)
        {
            // float minComfyTemperature = pawnSave.RealComfyTemperatures.min;
            // float maxComfyTemperature = pawnSave.RealComfyTemperatures.max;
            Pawn thisPawn = this._pawn;
            float minComfyTemperature = thisPawn.ComfortableTemperatureRange().min;
            float maxComfyTemperature = thisPawn.ComfortableTemperatureRange().max;

            // temperature
            FloatRange targetTemperatures = this.TargetTemperatures;

            this.GetInsulationStats(apparel, out float insulationCold, out float insulationHeat);

            string log = apparel.LabelCap + " - InsCold: " + insulationCold + " - InsHeat: " + insulationHeat
                         + " - TargTemp: " + targetTemperatures + "\nMinComfy: " + minComfyTemperature + " - MaxComfy: "
                         + maxComfyTemperature;

            // if this gear is currently worn, we need to make sure the contribution to the pawn's comfy temps is removed so the gear is properly scored
            List<Apparel> wornApparel = thisPawn.apparel.WornApparel;
            if (!wornApparel.NullOrEmpty())
            {
                if (wornApparel.Contains(apparel))
                {
                    // log += "\nPawn is wearer of this apparel.";
                    minComfyTemperature -= insulationCold;
                    maxComfyTemperature -= insulationHeat;
                }
                else
                {
                    // check if the candidate will replace existing gear
                    for (int index = 0; index < wornApparel.Count; index++)
                    {
                        Apparel wornAp = wornApparel[index];
                        if (!ApparelUtility.CanWearTogether(wornAp.def, apparel.def, thisPawn.RaceProps.body))
                        {
                            this.GetInsulationStats(wornAp, out float insulationColdWorn, out float insulationHeatWorn);

                            minComfyTemperature -= insulationColdWorn;
                            maxComfyTemperature -= insulationHeatWorn;

                            // Log.Message(apparel +"-"+ insulationColdWorn + "-" + insulationHeatWorn + "-" + minComfyTemperature + "-" + maxComfyTemperature);
                        }
                    }
                }
            }

            log += "\nBasic stat not worn - MinComfy: " + minComfyTemperature + " - MaxComfy: " + maxComfyTemperature;

            // now for the interesting bit.
            FloatRange temperatureScoreOffset = new FloatRange(0f, 0f);

            // isolation_cold is given as negative numbers < 0 means we're underdressed
            float neededInsulationCold = targetTemperatures.min - minComfyTemperature;

            // isolation_warm is given as positive numbers.
            float neededInsulationWarmth = targetTemperatures.max - maxComfyTemperature;

            FloatRange tempWeight = this.TemperatureWeight;
            log += "\nWeight: " + tempWeight + " - NeedInsCold: " + neededInsulationCold + " - NeedInsWarmth: "
                   + neededInsulationWarmth;

            if (neededInsulationCold < 0)
            {
                // currently too cold
                // caps ap to only consider the needed temp and don't give extra points
                if (neededInsulationCold > insulationCold)
                {
                    temperatureScoreOffset.min += neededInsulationCold;
                }
                else
                {
                    temperatureScoreOffset.min += insulationCold;
                }
            }
            else
            {
                // currently warm enough
                if (insulationCold > neededInsulationCold)
                {
                    // this gear would make us too cold
                    temperatureScoreOffset.min += insulationCold - neededInsulationCold;
                }
            }

            // invert for scoring
            temperatureScoreOffset.min *= -1;

            if (neededInsulationWarmth > 0)
            {
                // currently too warm
                // caps ap to only consider the needed temp and don't give extra points
                if (neededInsulationWarmth < insulationHeat)
                {
                    temperatureScoreOffset.max += neededInsulationWarmth;
                }
                else
                {
                    temperatureScoreOffset.max += insulationHeat;
                }
            }
            else
            {
                // currently cool enough
                if (insulationHeat < neededInsulationWarmth)
                {
                    // this gear would make us too warm
                    temperatureScoreOffset.max += insulationHeat - neededInsulationWarmth;
                }
            }

            // Punish bad apparel
            // temperatureScoreOffset.min *= temperatureScoreOffset.min < 0 ? 2f : 1f;
            // temperatureScoreOffset.max *= temperatureScoreOffset.max < 0 ? 2f : 1f;

            // New
            log += "\nPre-Evaluate: " + temperatureScoreOffset.min + " / " + temperatureScoreOffset.max;

            temperatureScoreOffset.min = Curve.Evaluate(temperatureScoreOffset.min * tempWeight.min);
            temperatureScoreOffset.max = Curve.Evaluate(temperatureScoreOffset.max * tempWeight.max);

            log += "\nScoreOffsetMin: " + temperatureScoreOffset.min + " - ScoreOffsetMax: "
                   + temperatureScoreOffset.max + " *= " + (temperatureScoreOffset.min * temperatureScoreOffset.max);

            Log.Message(log);
            return temperatureScoreOffset.min * temperatureScoreOffset.max;

            // return 1 + (temperatureScoreOffset.min + temperatureScoreOffset.max) / 15;
        }

        public void GetInsulationStats(Apparel apparel, out float insulationCold, out float insulationHeat)
        {
            if (Outfitter.Cache.InsulationDict.TryGetValue(apparel, out FloatRange range))
            {
                insulationCold = range.min;
                insulationHeat = range.max;
                return;
            }

            // offsets on apparel
            insulationCold = apparel.GetStatValue(StatDefOf.Insulation_Cold);
            insulationHeat = apparel.GetStatValue(StatDefOf.Insulation_Heat);

            insulationCold += apparel.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Cold);
            insulationHeat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Heat);

            // offsets on apparel infusions
            DoApparelScoreRaw_PawnStatsHandlers(apparel, StatDefOf.ComfyTemperatureMin, out float infInsulationCold);
            DoApparelScoreRaw_PawnStatsHandlers(apparel, StatDefOf.ComfyTemperatureMax, out float infInsulationHeat);
            insulationCold += infInsulationCold;
            insulationHeat += infInsulationHeat;

            Outfitter.Cache.InsulationDict.Add(apparel, new FloatRange(insulationCold, insulationHeat));
        }

        public ApparelEntry GetAllOffsets([NotNull] Apparel ap)
        {
            if (Outfitter.Cache.ApparelEntries.ContainsKey(ap))
            {
                return Outfitter.Cache.ApparelEntries[ap];
            }

            ApparelEntry entry = new ApparelEntry();
            this.GetStatsOfApparel(ap, ref entry.EquippedOffsets, ref entry.StatBases);
            this.GetStatsOfApparelInfused(ap, ref entry.InfusedOffsets);

            Outfitter.Cache.ApparelEntries.Add(ap, entry);
            return entry;
        }

        public void UpdateTemperatureIfNecessary(bool force = false, bool forceweight = false)
        {
            Pawn thisPawn = this._pawn;

            if (Find.TickManager.TicksGame - this._lastTempUpdate > JobGiver_OutfitterOptimizeApparel.ApparelStatCheck
                || force)
            {
                // get desired temperatures
                if (!this._pawnSave.TargetTemperaturesOverride)
                {

                    // float temp = GenTemperature.GetTemperatureAtTile(thisPawn.Map.Tile);
                    float lowest = this.LowestTemperatureComing(thisPawn.Map);
                    float highest = this.HighestTemperatureComing(thisPawn.Map);

                    // float minTemp = Mathf.Min(lowest - 5f, temp - 15f);
                    this._pawnSave.TargetTemperatures = new FloatRange(Mathf.Min(12, lowest - 10f), Mathf.Max(32, highest + 10f));

                    WorkTypeDef cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
                    if (thisPawn.workSettings.WorkIsActive(cooking) && thisPawn.workSettings.GetPriority(cooking) < 3)
                    {
                        this._pawnSave.TargetTemperatures.min = Mathf.Min(this._pawnSave.TargetTemperatures.min, -3);
                    }

                    this._lastTempUpdate = Find.TickManager.TicksGame;
                }
            }

            // FloatRange RealComfyTemperatures = thisPawn.ComfortableTemperatureRange();
            float min = thisPawn.def.statBases.GetStatValueFromList(StatDefOf.ComfyTemperatureMin, StatDefOf.ComfyTemperatureMin.defaultBaseValue);
            float max = thisPawn.def.statBases.GetStatValueFromList(StatDefOf.ComfyTemperatureMax, StatDefOf.ComfyTemperatureMax.defaultBaseValue);

            if (Find.TickManager.TicksGame - this._lastWeightUpdate > JobGiver_OutfitterOptimizeApparel.ApparelStatCheck
                || forceweight)
            {
                FloatRange weight = new FloatRange(1f, 1f);

                if (this._pawnSave.TargetTemperatures.min < min)
                {
                    weight.min += Math.Abs((this._pawnSave.TargetTemperatures.min - min) / 10);
                }

                if (this._pawnSave.TargetTemperatures.max > max)
                {
                    weight.max += Math.Abs((this._pawnSave.TargetTemperatures.max - max) / 10);
                }

                this._pawnSave.Temperatureweight = weight;
                this._lastWeightUpdate = Find.TickManager.TicksGame;
            }
        }

        private static void FillInfusionHashset_PawnStatsHandlers(
            Apparel apparel,
            StatDef parentStat,
            ref HashSet<StatDef> infusedOffsets)
        {
            ApparelScoreRawFillInfusedStat?.Invoke(apparel, parentStat, ref infusedOffsets);
        }

        private void GetStatsOfApparel(
            [NotNull] Apparel ap,
            ref HashSet<StatDef> equippedOffsets,
            ref HashSet<StatDef> statBases)
        {
            if (ap.def.equippedStatOffsets != null)
            {
                foreach (StatModifier equippedStatOffset in ap.def.equippedStatOffsets)
                {
                    equippedOffsets.Add(equippedStatOffset.stat);
                }
            }

            if (ap.def.statBases != null)
            {
                foreach (StatModifier statBase in ap.def.statBases)
                {
                    statBases.Add(statBase.stat);
                }
            }
        }

        private void GetStatsOfApparelInfused(Apparel ap, ref HashSet<StatDef> infusedOffsets)
        {
            foreach (StatPriority statPriority in this._pawn.GetApparelStatCache().StatCache)
            {
                FillInfusionHashset_PawnStatsHandlers(ap, statPriority.Stat, ref infusedOffsets);
            }
        }

        private float GetTemperature(Twelfth twelfth, [NotNull] Map map)
        {
            return GenTemperature.AverageTemperatureAtTileForTwelfth(map.Tile, twelfth);
        }

        private float LowestTemperatureComing([NotNull] Map map)
        {
            Twelfth twelfth = GenLocalDate.Twelfth(map);
            float a = this.GetTemperature(twelfth, map);
            for (int i = 0; i < 3; i++)
            {
                twelfth = twelfth.NextTwelfth();
                a = Mathf.Min(a, this.GetTemperature(twelfth, map));
            }

            return Mathf.Min(a, map.mapTemperature.OutdoorTemp);
        }

        private float HighestTemperatureComing([NotNull] Map map)
        {
            Twelfth twelfth = GenLocalDate.Twelfth(map);
            float a = this.GetTemperature(twelfth, map);
            for (int i = 0; i < 3; i++)
            {
                twelfth = twelfth.NextTwelfth();
                a = Mathf.Max(a, this.GetTemperature(twelfth, map));
            }

            return Mathf.Max(a, map.mapTemperature.OutdoorTemp);
        }

        // ReSharper disable once CollectionNeverUpdated.Global
    }
}
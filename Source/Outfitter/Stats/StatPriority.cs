// Outfitter/StatCache.cs
//
// Copyright Karel Kroeze, 2016.
//
// Created 2016-01-02 13:58

using System.Collections.Generic;
using JetBrains.Annotations;
using Outfitter.Enums;
using RimWorld;
using Verse;

namespace Outfitter.Stats
{
    public class StatPriority
    {
        public StatPriority(StatDef stat, float priority, StatAssignment assignment = StatAssignment.Automatic)
        {
            this.Stat = stat;
            this.Weight = priority;
            this.Assignment = assignment;
        }

        public StatPriority(
            KeyValuePair<StatDef, float> statDefWeightPair,
            StatAssignment assignment = StatAssignment.Automatic)
        {
            this.Stat = statDefWeightPair.Key;
            this.Weight = statDefWeightPair.Value;
            this.Assignment = assignment;
        }

        public StatAssignment Assignment { get; set; }

        public StatDef Stat { get; }

        public float Weight { get; set; }

        public void Delete([NotNull] Pawn pawn)
        {
            pawn.GetApparelStatCache().Cache.Remove(this);

            pawn.GetSaveablePawn().Stats.RemoveAll(i => i.Stat == this.Stat);
        }

        public void Reset(Pawn pawn)
        {
            Dictionary<StatDef, float> stats = pawn.GetWeightedApparelStats();
            Dictionary<StatDef, float> indiStats = pawn.GetWeightedApparelIndividualStats();

            if (stats.ContainsKey(this.Stat))
            {
                this.Weight = stats[this.Stat];
                this.Assignment = StatAssignment.Automatic;
            }

            if (indiStats.ContainsKey(this.Stat))
            {
                this.Weight = indiStats[this.Stat];
                this.Assignment = StatAssignment.Individual;
            }

            SaveablePawn pawnSave = pawn.GetSaveablePawn();
            pawnSave.Stats.RemoveAll(i => i.Stat == this.Stat);
        }
    }

    // ReSharper disable once CollectionNeverUpdated.Global
}
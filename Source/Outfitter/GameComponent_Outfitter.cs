using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Outfitter.Window;
using RimWorld;
using Verse;

namespace Outfitter
{
    public class GameComponent_Outfitter : GameComponent
    {
        private readonly Game _game;

        [NotNull]
        public List<SaveablePawn> PawnCache = new List<SaveablePawn>();

        public GameComponent_Outfitter()
        {
        }

        // ReSharper disable once UnusedMember.Global
        public GameComponent_Outfitter(Game game)
        {
            this._game = game;
            if (Controller.Settings.UseEyes)
            {
                foreach (BodyDef bodyDef in DefDatabase<BodyDef>.AllDefsListForReading)
                {
                    if (bodyDef.defName != "Human")
                    {
                        continue;
                    }

                    BodyPartRecord neck = bodyDef.corePart.parts.FirstOrDefault(x => x.def == BodyPartDefOf.Neck);
                    BodyPartRecord head = neck?.parts.FirstOrDefault(x => x.def == BodyPartDefOf.Head);
                    if (head == null)
                    {
                        continue;
                    }
                    //    if (!head.groups.Contains(BodyPartGroupDefOf.Eyes))
                    {
                        //     head.groups.Add(BodyPartGroupDefOf.Eyes);
                        BodyPartRecord leftEye = head.parts.FirstOrDefault(x => x.def == BodyPartDefOf.LeftEye);
                        BodyPartRecord rightEye = head.parts.FirstOrDefault(x => x.def == BodyPartDefOf.RightEye);
                        BodyPartRecord jaw = head.parts.FirstOrDefault(x => x.def == BodyPartDefOf.Jaw);
                        leftEye?.groups.Remove(BodyPartGroupDefOf.FullHead);
                        rightEye?.groups.Remove(BodyPartGroupDefOf.FullHead);
                        jaw?.groups.Remove(BodyPartGroupDefOf.FullHead);
                        Log.Message("Outfitter patched Human eyes and jaw.");
                        break;
                    }

                }
            }

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.Where(
                td => td.category == ThingCategory.Pawn && td.race.Humanlike))
            {
                // if (def.inspectorTabs == null)
                // {
                //     def.inspectorTabs = new List<Type>();
                // }
                //
                // if (def.inspectorTabsResolved == null)
                // {
                //     def.inspectorTabsResolved = new List<InspectTabBase>();
                // }
                if (def.inspectorTabs == null || def.inspectorTabsResolved == null)
                {
                    return;
                }

                if (def.inspectorTabs.Contains(typeof(Tab_Pawn_Outfitter)))
                {
                    return;
                }

                def.inspectorTabs.Add(typeof(Tab_Pawn_Outfitter));
                def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(Tab_Pawn_Outfitter)));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref this.PawnCache, "Pawns", LookMode.Deep);
        }
    }
}
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitter
{
    class Controller : Mod
    {
        public Controller(ModContentPack content)
            : base(content)
        {
            Settings = this.GetSettings<Settings>();

        }
        public static Settings Settings;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }

        [NotNull]
        public override string SettingsCategory()
        {
            return "Outfitter";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            Settings.Write();

            if (Current.ProgramState == ProgramState.Playing)
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
                        BodyPartRecord leftEye = head.parts.FirstOrDefault(x => x.def == BodyPartDefOf.Eye);
                        BodyPartRecord jaw = head.parts.FirstOrDefault(x => x.def == BodyPartDefOf.Jaw);

                        if (Settings.UseEyes)
                        {
                            leftEye?.groups.Remove(BodyPartGroupDefOf.FullHead);
                            jaw?.groups.Remove(BodyPartGroupDefOf.FullHead);
                            Log.Message("Outfitter removed FullHead from Human eyes.");
                        }
                        else
                        {
                            if (leftEye != null && !leftEye.groups.Contains(BodyPartGroupDefOf.FullHead))
                            {
                                leftEye?.groups.Add(BodyPartGroupDefOf.FullHead);
                            }

                            if (jaw != null && !jaw.groups.Contains(BodyPartGroupDefOf.FullHead))
                            {
                                jaw?.groups.Add(BodyPartGroupDefOf.FullHead);
                            }
                            Log.Message("Outfitter re-added FullHead to Human eyes.");
                        }
                        break;
                    }

                }

            }
        }
    }
}

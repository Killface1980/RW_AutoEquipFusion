// namespace Outfitter
// {
//    public class Outfitter_ModBase : HugsLib.ModBase
//    {
//        public override string ModIdentifier { get { return "Outfitter"; } }
//    }

using System.Linq;
using System.Reflection;
using Harmony;
using Outfitter.TabPatch;
using RimWorld;
using Verse;

// ReSharper disable InconsistentNaming

namespace Outfitter
{
    [StaticConstructorOnStartup]
    internal static class HarmonyPatches
    {

        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("com.outfitter.rimworld.mod");

            harmony.Patch(
                          AccessTools.Method(typeof(PawnGraphicSet), nameof(PawnGraphicSet.ResolveApparelGraphics)),
                          null,
                          new HarmonyMethod(typeof(HarmonyPatches), nameof(ResolveApparelGraphics_Postfix)));

            // harmony.Patch(
            // AccessTools.Method(typeof(InspectPaneUtility), "DoTabs"),
            // new HarmonyMethod(typeof(TabsPatch), nameof(TabsPatch.DoTabs_Prefix)),
            // null);

            harmony.Patch(
                          AccessTools.Method(typeof(JobGiver_OptimizeApparel), "TryGiveJob"),
                          new HarmonyMethod(
                                            typeof(JobGiver_OutfitterOptimizeApparel),
                                            nameof(JobGiver_OutfitterOptimizeApparel.TryGiveJob_Prefix)),
                          null);

            harmony.Patch(
                          AccessTools.Method(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.SetPriority)),
                          null,
                          new HarmonyMethod(typeof(HarmonyPatches), nameof(UpdatePriorities)));

            harmony.Patch(
                          AccessTools.Method(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.Notify_UseWorkPrioritiesChanged)),
                          null,
                          new HarmonyMethod(typeof(HarmonyPatches), nameof(UpdatePriorities)));

            harmony.Patch(
                          AccessTools.Method(typeof(ITab_Bills), "FillTab"),
                          new HarmonyMethod(typeof(Tab_Bills_Patch), nameof(Tab_Bills_Patch.FillTab_Prefix)),
                          null);

            harmony.Patch(
                          AccessTools.Method(typeof(ITab_Bills), "TabUpdate"),
                          new HarmonyMethod(typeof(Tab_Bills_Patch), nameof(Tab_Bills_Patch.TabUpdate_Prefix)),
                          null);

            // harmony.Patch(
            // AccessTools.Method(typeof(ThinkNode_JobGiver), nameof(ThinkNode_JobGiver.TryIssueJobPackage)),
            // null,
            // new HarmonyMethod(typeof(HarmonyPatches), nameof(LogJobActivities)));

            // harmony.Patch(
            // AccessTools.Method(typeof(ITab_Bills), "FillTab"),
            // new HarmonyMethod(
            // typeof(ITab_Bills_Patch),
            // nameof(ITab_Bills_Patch.FillTab_Prefix)),
            // null);
            Log.Message(
                        "Outfitter successfully completed " + harmony.GetPatchedMethods().Count() + " patches with harmony.");
        }

        private static void ResolveApparelGraphics_Postfix(PawnGraphicSet __instance)
        {
            __instance.apparelGraphics = __instance.apparelGraphics
                                                   .OrderBy(x => x.sourceApparel.def.apparel.bodyPartGroups[0].listOrder).ToList();
        }

        private static void UpdatePriorities(Pawn_WorkSettings __instance)
        {
            FieldInfo fieldInfo =
            typeof(Pawn_WorkSettings).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);
            Pawn pawn = (Pawn)fieldInfo?.GetValue(__instance);
            if (pawn != null)
            {
                pawn.GetSaveablePawn().ForceStatUpdate = true;
            }
        }
    }
}
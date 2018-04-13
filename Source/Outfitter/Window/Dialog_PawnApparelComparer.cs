using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitter.Window
{
    public class Dialog_PawnApparelComparer : Verse.Window
    {
        [NotNull]
        private readonly Apparel _apparel;

        [NotNull]
        private readonly Pawn _pawn;

        private Vector2 _scrollPosition;

        public Dialog_PawnApparelComparer(Pawn p, Apparel apparel)
        {
            this.doCloseX = true;
            this.closeOnEscapeKey = true;
            this.doCloseButton = true;

            this._pawn = p;
            this._apparel = apparel;
        }

        public override Vector2 InitialSize => new Vector2(500f, 700f);

        private Dictionary<Apparel, float> _dict;

        private const float ScoreWidth = 100f;

        public override void DoWindowContents(Rect inRect)
        {
            ApparelStatCache apparelStatCache = this._pawn.GetApparelStatCache();
            Outfit currentOutfit = this._pawn.outfits.CurrentOutfit;

            if (this._dict == null || Find.TickManager.TicksGame % 60 == 0 || GUI.changed)
            {
                List<Apparel> ap = new List<Apparel>(this._pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel).OfType<Apparel>().Where(
                        x => x.Map.slotGroupManager.SlotGroupAt(x.Position) != null));

                foreach (Pawn otherPawn in PawnsFinder.AllMaps_FreeColonists.Where(x => x.Map == this._pawn.Map))
                {
                    foreach (Apparel pawnApparel in otherPawn.apparel.WornApparel)
                    {
                        if (otherPawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(pawnApparel))
                        {
                            ap.Add(pawnApparel);
                        }
                    }
                }

                ap = ap.Where(
                    i => !ApparelUtility.CanWearTogether(this._apparel.def, i.def, this._pawn.RaceProps.body)
                         && currentOutfit.filter.Allows(i)).ToList();


                ap = ap.OrderByDescending(
                    i =>
                        {
                            float g = this._pawn.ApparelScoreGain(i);
                            return g;
                        }).ToList();

                this._dict = new Dictionary<Apparel, float>();
                foreach (Apparel currentAppel in ap)
                {
                    float gain = this._pawn.ApparelScoreGain(currentAppel);
                    this._dict.Add(currentAppel, gain);
                }
            }

            Rect groupRect = inRect.ContractedBy(10f);
            groupRect.height -= 100;
            GUI.BeginGroup(groupRect);


            float apparelLabelWidth = (groupRect.width - 2 * ScoreWidth) / 3 - 8f - 8f;
            float apparelEquippedWidth = apparelLabelWidth;
            float apparelOwnerWidth = apparelLabelWidth;

            Rect itemRect = new Rect(groupRect.xMin + 4f, groupRect.yMin, groupRect.width - 8f, 28f);

            this.DrawLine(
                ref itemRect,
                null,
                "Apparel",
                apparelLabelWidth,
                null,
                "Equiped",
                apparelEquippedWidth,
                null,
                "Target",
                apparelOwnerWidth,
                "Score",
                "Gain");

            groupRect.yMin += itemRect.height;
            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMin, groupRect.width);
            groupRect.yMin += 4f;
            groupRect.height -= 4f;
            groupRect.height -= Text.LineHeight * 1.2f * 3f;

            Rect viewRect = new Rect(
                groupRect.xMin,
                groupRect.yMin,
                groupRect.width - 16f, this._dict.Count * 28f + 16f);
            if (viewRect.height < groupRect.height)
            {
                groupRect.height = viewRect.height;
            }

            Rect listRect = viewRect.ContractedBy(4f);

            Widgets.BeginScrollView(groupRect, ref this._scrollPosition, viewRect);


            foreach (KeyValuePair<Apparel, float> kvp in this._dict)
            {
                Apparel currentAppel = kvp.Key;
                float gain = kvp.Value;

                itemRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, 28f);
                if (Mouse.IsOver(itemRect))
                {
                    GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                    GUI.color = Color.white;
                }

                Pawn equipped = currentAppel.Wearer;

                string gainString = this._pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(currentAppel)
                                        ? gain.ToString("N3")
                                        : "No Allow";

                this.DrawLine(
                    ref itemRect,
                    currentAppel,
                    currentAppel.LabelCap,
                    apparelLabelWidth,
                    equipped,
                    equipped?.LabelCap,
                    apparelEquippedWidth,
                    null,
                    null,
                    apparelOwnerWidth,
                    apparelStatCache.ApparelScoreRaw(currentAppel).ToString("N3"),
                    gainString
                    );

                listRect.yMin = itemRect.yMax;
            }

            Widgets.EndScrollView();

            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMax, groupRect.width);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        private void DrawLine(
            ref Rect itemRect,
            [CanBeNull] Apparel apparelThing,
            string apparelText,
            float textureWidth,
            [CanBeNull] Pawn equippedPawn,
            string apparelEquippedText,
            float apparelEquippedWidth,
            [CanBeNull] Pawn apparelOwnerThing,
            string apparelOwnerText,
            float apparelOwnerWidth,
            string apparelScoreText,
            string apparelGainText)
        {
            Rect fieldRect;
            bool isCurrentlyWorn = equippedPawn != null;

            if (apparelThing != null)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!apparelText.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(fieldRect, apparelText);
                }

                if (apparelThing.def.DrawMatSingle != null && apparelThing.def.DrawMatSingle.mainTexture != null)
                {
                    Widgets.ThingIcon(fieldRect, apparelThing);
                }

                if (Widgets.ButtonInvisible(fieldRect))
                {
                    this.Close();
                    Find.MainTabsRoot.EscapeCurrentTab();
                    if (isCurrentlyWorn)
                    {
                        Find.CameraDriver.JumpToVisibleMapLoc(equippedPawn.PositionHeld);
                        Find.Selector.ClearSelection();
                        if (equippedPawn.Spawned)
                        {
                            Find.Selector.Select(equippedPawn);
                        }
                    }
                    else
                    {
                        Find.CameraDriver.JumpToVisibleMapLoc(apparelThing.PositionHeld);
                        Find.Selector.ClearSelection();
                        if (apparelThing.Spawned)
                        {
                            Find.Selector.Select(apparelThing);
                        }
                    }

                    return;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(apparelText))
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, textureWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelText);
                }
            }

            itemRect.xMin += textureWidth;

            if (isCurrentlyWorn)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!apparelEquippedText.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(fieldRect, apparelEquippedText);
                }

                if (equippedPawn.def.DrawMatSingle != null
                    && equippedPawn.def.DrawMatSingle.mainTexture != null)
                {
                    Widgets.ThingIcon(fieldRect, equippedPawn);
                }

                if (Widgets.ButtonInvisible(fieldRect))
                {
                    this.Close();
                    Find.MainTabsRoot.EscapeCurrentTab();
                    Find.CameraDriver.JumpToVisibleMapLoc(equippedPawn.PositionHeld);
                    Find.Selector.ClearSelection();
                    if (equippedPawn.Spawned)
                    {
                        Find.Selector.Select(equippedPawn);
                    }

                    return;
                }
            }
            else
            {
                if (!apparelEquippedText.NullOrEmpty())
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, apparelEquippedWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelText);
                }
            }

            itemRect.xMin += apparelEquippedWidth;

            if (apparelOwnerThing != null)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!string.IsNullOrEmpty(apparelOwnerText))
                {
                    TooltipHandler.TipRegion(fieldRect, apparelOwnerText);
                }

                if (apparelOwnerThing.def.DrawMatSingle != null
                    && apparelOwnerThing.def.DrawMatSingle.mainTexture != null)
                {
                    Widgets.ThingIcon(fieldRect, apparelOwnerThing);
                }
            }
            else
            {
                if (!apparelOwnerText.NullOrEmpty())
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, apparelOwnerWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelOwnerText);
                }
            }

            itemRect.xMin += apparelOwnerWidth;

            fieldRect = new Rect(itemRect.xMin, itemRect.yMin, ScoreWidth, itemRect.height);
            if (isCurrentlyWorn)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
            }
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(fieldRect, apparelScoreText);

            itemRect.xMin += ScoreWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, ScoreWidth, itemRect.height), apparelGainText);
            GUI.color = Color.white;
            if (apparelThing != null)
            {
                Text.Anchor = TextAnchor.UpperLeft;
                if (Widgets.ButtonInvisible(fieldRect))
                {
                    Find.WindowStack.Add(new Window_Pawn_ApparelDetail(this._pawn, apparelThing));
                }
            }
        }
    }
}
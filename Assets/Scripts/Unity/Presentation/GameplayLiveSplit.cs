using System;
using System.Globalization;
using Biathlon.Presentation;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class GameplayHud
    {
        private VisualElement checkpointNotice, liveSplitTrackFill, liveSplitFeatured;
        private Label liveSplitHeading, liveSplitStage, liveSplitAthlete, liveSplitNation, liveSplitTime,
            liveSplitDifference, liveSplitReference;
        private NationFlagGraphic liveSplitFlag;
        private TimingAthleteRow liveSplitCard;
        private readonly CheckpointStanding[] liveSplitRivals = new CheckpointStanding[3];
        private CheckpointStandings liveSplitStandings;
        private int liveSplitOrdinal, liveSplitRivalCount, liveSplitShownSlot;
        private bool liveSplitCrossed;
        private long liveSplitTick, liveSplitRotationStart;

        private void BuildLiveSplit()
        {
            var mount = new VisualElement { pickingMode = PickingMode.Ignore };
            mount.style.position = Position.Absolute;
            // Reserve the two side columns even in narrow windows. The bottom
            // strip never shares screen space with speed, pulse or breathing.
            mount.style.left = settings.edge + settings.speed_width * Scale + 12;
            mount.style.right = settings.edge + settings.right_width * Scale + 12;
            mount.style.bottom = settings.edge;
            mount.style.alignItems = Align.Center;
            Root.Add(mount);

            checkpointNotice = new VisualElement { name = "checkpoint_notice", pickingMode = PickingMode.Ignore };
            FrostlineStyle.SurfaceCard(checkpointNotice, true);
            checkpointNotice.style.width = 760 * Scale;
            checkpointNotice.style.maxWidth = Length.Percent(100);
            checkpointNotice.style.flexDirection = FlexDirection.Row;
            checkpointNotice.style.height = 61 * Scale;
            checkpointNotice.style.overflow = Overflow.Hidden;
            if (HighContrast) checkpointNotice.style.backgroundColor = Color.black;
            checkpointNotice.style.display = DisplayStyle.None;
            mount.Add(checkpointNotice);

            var header = new VisualElement();
            header.style.width = 162 * Scale;
            header.style.flexShrink = 0;
            header.style.paddingLeft = 12 * Scale;
            header.style.paddingRight = 7 * Scale;
            header.style.justifyContent = Justify.Center;
            checkpointNotice.Add(header);
            liveSplitHeading = SplitLabel(header, "live_split_heading", "", settings.body_size, true);
            liveSplitStage = SplitLabel(header, "live_split_stage", "", settings.caption_size, true);
            liveSplitStage.style.color = accent;

            var featured = liveSplitFeatured = Row(checkpointNotice);
            featured.name = "live_split_player";
            featured.style.backgroundColor = HighContrast ? Color.black : FrostlineStyle.Surface;
            featured.style.borderLeftWidth = 3;
            featured.style.borderLeftColor = accent;
            featured.style.paddingLeft = 9 * Scale;
            featured.style.paddingRight = 12 * Scale;
            featured.style.flexGrow = 1.25f;
            featured.style.flexBasis = 0;
            featured.style.minWidth = 0;
            liveSplitFlag = new NationFlagGraphic("") { name = "live_split_player_flag" };
            liveSplitFlag.style.width = 30 * Scale;
            liveSplitFlag.style.height = 20 * Scale;
            featured.Add(liveSplitFlag);
            var identity = new VisualElement();
            identity.style.flexGrow = 1;
            identity.style.flexBasis = 0;
            identity.style.minWidth = 0;
            identity.style.marginRight = 8 * Scale;
            featured.Add(identity);
            liveSplitAthlete = SplitLabel(identity, "live_split_player_name", "", settings.body_size);
            liveSplitAthlete.style.unityFontStyleAndWeight = FontStyle.Bold;
            liveSplitNation = SplitLabel(identity, "live_split_player_nation", "", settings.caption_size, true);
            liveSplitNation.style.color = accent;

            var clockColumn = new VisualElement();
            clockColumn.style.alignItems = Align.FlexEnd;
            clockColumn.style.flexShrink = 0;
            featured.Add(clockColumn);
            liveSplitTime = SplitLabel(clockColumn, "live_split_time", "", settings.number_size, true);
            liveSplitDifference = SplitLabel(clockColumn, "live_split_difference", "", settings.body_size, true);

            var miniWindow = new VisualElement { name = "live_split_window" };
            miniWindow.style.flexGrow = 1;
            miniWindow.style.flexBasis = 0;
            miniWindow.style.minWidth = 0;
            miniWindow.style.justifyContent = Justify.Center;
            checkpointNotice.Add(miniWindow);
            liveSplitReference = SplitLabel(miniWindow, "live_split_reference",
                L("Ещё нет времени лидера", "No time to beat yet"), settings.caption_size);
            liveSplitReference.style.color = muted;
            liveSplitReference.style.unityTextAlign = TextAnchor.MiddleCenter;
            liveSplitReference.style.whiteSpace = WhiteSpace.Normal;
            liveSplitReference.style.paddingLeft = liveSplitReference.style.paddingRight = 12 * Scale;
            liveSplitCard = new TimingAthleteRow(Scale, broadcast: true) { name = "live_split_card" };
            liveSplitCard.style.height = StyleKeyword.Auto;
            liveSplitCard.style.flexGrow = 1;
            liveSplitCard.style.minWidth = 0;
            liveSplitCard.style.marginBottom = 0;
            liveSplitCard.style.display = DisplayStyle.None;
            liveSplitCard.style.paddingLeft = liveSplitCard.style.paddingRight = 12 * Scale;
            miniWindow.Add(liveSplitCard);

            var track = new VisualElement();
            track.style.position = Position.Absolute;
            track.style.left = track.style.right = track.style.bottom = 0;
            track.style.height = 2;
            track.style.backgroundColor = new Color(accent.r, accent.g, accent.b, .18f);
            checkpointNotice.Add(track);
            liveSplitTrackFill = new VisualElement { name = "live_split_progress" };
            liveSplitTrackFill.style.height = 2;
            liveSplitTrackFill.style.backgroundColor = accent;
            track.Add(liveSplitTrackFill);
            liveSplitStandings = null;
            liveSplitOrdinal = -1;
        }

        private Label SplitLabel(VisualElement parent, string name, string value, int size, bool display = false)
        {
            var label = Text(parent, name, value, size, display);
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.marginBottom = 0;
            label.style.minWidth = 0;
            return label;
        }

        private void RefreshLiveSplit(AthletePresentationState athlete)
        {
            var field = host?.Runner?.Field;
            var split = field == null || CheckpointTableVisible || helpVisible ? default :
                LiveSplit.Capture(field, checkpointHistoryLayout, selectedIndex, athlete,
                    reviewSettings.checkpoint_approach_m, reviewSettings.checkpoint_notice_seconds);
            checkpointNotice.style.display = split.Visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!split.Visible)
            {
                liveSplitOrdinal = -1;
                return;
            }

            long tick = field.Tick.Value;
            if (liveSplitOrdinal != split.Ordinal || liveSplitCrossed != split.Crossed || tick < liveSplitTick)
            {
                liveSplitOrdinal = split.Ordinal;
                liveSplitCrossed = split.Crossed;
                liveSplitStandings = null;
                liveSplitRivalCount = 0;
                liveSplitRotationStart = tick;
                liveSplitFeatured.style.display = split.Crossed ? DisplayStyle.None : DisplayStyle.Flex;
                checkpointNotice.style.width = (split.Crossed ? 500 : 760) * Scale;
                var culture = CultureInfo.GetCultureInfo(Locale == "ru" ? "ru-RU" : "en-US");
                liveSplitHeading.text = L("ОТСЕЧКА ", "SPLIT ") + host.DistanceView.Kilometers(split.RouteMeters, culture) + L(" КМ", " KM");
            }
            liveSplitTick = tick;

            if (split.Crossed)
            {
                if (liveSplitStandings != null) return;
                var own = field.GetAthlete(selectedIndex).Checkpoints.Get(split.Ordinal);
                liveSplitStandings = CheckpointStandings.Capture(field, split.Ordinal, own.GlobalTime);
                for (int i = 0; i < liveSplitStandings.Count; i++)
                {
                    var result = liveSplitStandings.Get(i);
                    if (result.AthleteId != athleteId) continue;
                    liveSplitStage.text = L("МЕСТО ", "RANK ") + result.Rank + " / " + liveSplitStandings.Count;
                    BindLiveSplitCard(result, true);
                    break;
                }
                liveSplitTrackFill.style.width = Length.Percent(100);
                liveSplitCard.style.display = DisplayStyle.Flex;
                liveSplitReference.style.display = DisplayStyle.None;
                return;
            }

            liveSplitStage.text = L("ДО ЛИНИИ ", "TO LINE ") + Math.Ceiling(split.RemainingMeters) + L(" М", " M");
            liveSplitTime.text = GameplayHudModel.FormatClock(split.Seconds);
            liveSplitTrackFill.style.width = Length.Percent(100 * (1 - (float)(split.RemainingMeters / reviewSettings.checkpoint_approach_m)));

            var difference = split.DifferenceSeconds;
            var gapCulture = CultureInfo.GetCultureInfo(Locale == "ru" ? "ru-RU" : "en-US");
            string gap = difference.HasValue ? GameplayHudModel.FormatGap(Math.Abs(difference.Value), gapCulture) : "—";
            liveSplitDifference.text = difference < 0 ? "−" + gap.Substring(1) : gap;
            liveSplitDifference.style.color = difference > 0 && !HighContrast ? FrostlineStyle.Danger : accent;
            if (liveSplitStandings == null || liveSplitStandings.Count != split.Arrivals)
            {
                liveSplitStandings = CheckpointStandings.Capture(field, split.Ordinal);
                if (liveSplitRivalCount == 0) liveSplitRotationStart = tick;
                liveSplitRivalCount = liveSplitStandings.CopyBroadcastRivals(athleteId, liveSplitRivals);
                liveSplitShownSlot = -1;
                BindLiveSplitIdentity();
                liveSplitCard.style.display = liveSplitRivalCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                liveSplitReference.style.display = liveSplitRivalCount == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (liveSplitRivalCount == 0) return;
            int cardTicks = Math.Max(1, Mathf.RoundToInt(reviewSettings.checkpoint_card_seconds * field.TickFrequency));
            int slot = (int)((tick - liveSplitRotationStart) / cardTicks % liveSplitRivalCount);
            if (slot == liveSplitShownSlot) return;
            liveSplitShownSlot = slot;
            BindLiveSplitCard(liveSplitRivals[slot], false);
        }

        private string LiveSplitPlayerName() => broadcastNames.TryGetValue(athleteId, out var name) ? name :
            displayName;

        private void BindLiveSplitIdentity()
        {
            string name = LiveSplitPlayerName();
            liveSplitAthlete.text = liveSplitAthlete.tooltip = string.IsNullOrEmpty(name) ? L("Вы", "You") : name;
            string playerNation = broadcastNations.TryGetValue(athleteId, out var nation) ? nation : "";
            liveSplitFlag.SetNation(playerNation);
            liveSplitNation.text = L("ВЫ", "YOU") + (playerNation.Length == 0 ? "" : "  ·  " + playerNation);
        }

        private void BindLiveSplitCard(CheckpointStanding standing, bool player)
        {
            string name = player ? LiveSplitPlayerName() :
                broadcastNames.TryGetValue(standing.AthleteId, out var stored) ? stored : L("Спортсмен", "Athlete") + " " + standing.Rank;
            if (player) name = L("ВЫ", "YOU") + (string.IsNullOrEmpty(name) ? "" : " · " + name);
            string nation = broadcastNations.TryGetValue(standing.AthleteId, out var code) ? code : "";
            liveSplitCard.BindCheckpoint(standing.AthleteId, standing.Rank, name, nation, standing.Seconds,
                standing.GapSeconds, Locale, player);
        }
    }
}

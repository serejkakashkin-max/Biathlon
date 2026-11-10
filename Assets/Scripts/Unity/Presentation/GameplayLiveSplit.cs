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
        private VisualElement checkpointNotice, liveSplitTrackFill, liveSplitClock;
        private Label liveSplitHeading, liveSplitStage, liveSplitTime, liveSplitDifference, liveSplitReference;
        private readonly TimingAthleteRow[] liveSplitCards = new TimingAthleteRow[CheckpointStandings.BroadcastSlots];
        private CheckpointStandings liveSplitStandings;
        private int liveSplitOrdinal, liveSplitShownStart;
        private bool liveSplitCrossed;
        private long liveSplitTick, liveSplitRotationStart;
        private double liveSplitRotationDistance;

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
            checkpointNotice.style.width = 930 * Scale;
            checkpointNotice.style.maxWidth = Length.Percent(100);
            checkpointNotice.style.flexDirection = FlexDirection.Column;
            checkpointNotice.style.height = 84 * Scale;
            checkpointNotice.style.overflow = Overflow.Hidden;
            if (HighContrast) checkpointNotice.style.backgroundColor = Color.black;
            checkpointNotice.style.display = DisplayStyle.None;
            mount.Add(checkpointNotice);

            var header = Row(checkpointNotice);
            header.style.height = 27 * Scale;
            header.style.flexShrink = 0;
            header.style.paddingLeft = header.style.paddingRight = 12 * Scale;
            header.style.backgroundColor = HighContrast ? Color.black : FrostlineStyle.Surface;
            liveSplitHeading = SplitLabel(header, "live_split_heading", "", settings.body_size, true);
            liveSplitStage = SplitLabel(header, "live_split_stage", "", settings.caption_size, true);
            liveSplitStage.style.marginLeft = 12 * Scale;
            liveSplitStage.style.flexGrow = 1;
            liveSplitStage.style.color = accent;

            liveSplitClock = Row(header);
            liveSplitClock.name = "live_split_clock";
            liveSplitClock.style.flexShrink = 0;
            liveSplitTime = SplitLabel(liveSplitClock, "live_split_time", "", settings.body_size, true);
            liveSplitDifference = SplitLabel(liveSplitClock, "live_split_difference", "", settings.body_size, true);
            liveSplitDifference.style.marginLeft = 8 * Scale;

            var miniWindow = Row(checkpointNotice);
            miniWindow.name = "live_split_window";
            miniWindow.style.flexGrow = 1;
            miniWindow.style.alignItems = Align.Stretch;
            liveSplitReference = SplitLabel(miniWindow, "live_split_reference",
                L("Ещё нет времени лидера", "No time to beat yet"), settings.caption_size);
            liveSplitReference.style.flexGrow = 1;
            liveSplitReference.style.color = muted;
            liveSplitReference.style.unityTextAlign = TextAnchor.MiddleCenter;

            for (int i = 0; i < liveSplitCards.Length; i++)
            {
                var card = liveSplitCards[i] = new TimingAthleteRow(Scale, broadcast: true) { name = "live_split_card_" + i };
                card.style.height = StyleKeyword.Auto;
                card.style.flexGrow = 1;
                card.style.flexBasis = 0;
                card.style.minWidth = 0;
                card.style.marginBottom = 0;
                card.style.display = DisplayStyle.None;
                card.style.paddingLeft = card.style.paddingRight = 10 * Scale;
                card.style.borderRightWidth = i + 1 < liveSplitCards.Length ? 1 : 0;
                card.style.borderRightColor = new Color(accent.r, accent.g, accent.b, .2f);
                miniWindow.Add(card);
            }

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
                liveSplitShownStart = -1;
                liveSplitClock.style.display = split.Crossed ? DisplayStyle.None : DisplayStyle.Flex;
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
                    BindLiveSplitPage(liveSplitStandings.BroadcastResultStart(i), result.AthleteId);
                    break;
                }
                liveSplitTrackFill.style.width = Length.Percent(100);
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
                if (liveSplitStandings == null || liveSplitStandings.Count == 0)
                {
                    liveSplitRotationStart = tick;
                    liveSplitRotationDistance = split.RemainingMeters;
                }
                liveSplitStandings = CheckpointStandings.Capture(field, split.Ordinal);
                liveSplitShownStart = -1;
            }

            int pageTicks = Math.Max(1, Mathf.RoundToInt(reviewSettings.checkpoint_page_seconds * field.TickFrequency));
            double progress = liveSplitRotationDistance > 0 ? 1 - split.RemainingMeters / liveSplitRotationDistance : 0;
            int pageStart = liveSplitStandings.BroadcastPageStart((tick - liveSplitRotationStart) / pageTicks, progress);
            if (pageStart != liveSplitShownStart) BindLiveSplitPage(pageStart);
        }

        private void BindLiveSplitPage(int start, string featuredId = null)
        {
            liveSplitShownStart = start;
            liveSplitReference.style.display = liveSplitStandings.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            for (int i = 0; i < liveSplitCards.Length; i++)
            {
                var card = liveSplitCards[i];
                int index = start + i;
                bool visible = index < liveSplitStandings.Count;
                card.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible) continue;

                var standing = liveSplitStandings.Get(index);
                bool player = standing.AthleteId == featuredId;
                string name = broadcastNames.TryGetValue(standing.AthleteId, out var stored) ? stored :
                    player ? displayName : L("Спортсмен", "Athlete") + " " + standing.Rank;
                if (player) name = L("ВЫ", "YOU") + (string.IsNullOrEmpty(name) ? "" : " · " + name);
                string nation = broadcastNations.TryGetValue(standing.AthleteId, out var code) ? code : "";
                card.BindCheckpoint(standing.AthleteId, standing.Rank, name, nation, standing.Seconds,
                    standing.GapSeconds, Locale, player);
            }
        }
    }
}

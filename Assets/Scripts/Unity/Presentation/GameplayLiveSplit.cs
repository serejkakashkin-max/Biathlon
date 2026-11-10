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
        private readonly TimingAthleteRow[] liveSplitRows = new TimingAthleteRow[2];
        private readonly CheckpointStanding[] liveSplitRivals = new CheckpointStanding[2];
        private CheckpointStandings liveSplitStandings;
        private int liveSplitOrdinal, liveSplitRivalCount;
        private bool liveSplitCrossed, liveSplitCompact;
        private long liveSplitTick;

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
            checkpointNotice.style.width = 1040 * Scale;
            checkpointNotice.style.maxWidth = Length.Percent(100);
            checkpointNotice.style.flexDirection = FlexDirection.Row;
            checkpointNotice.style.height = 61 * Scale;
            checkpointNotice.style.overflow = Overflow.Hidden;
            if (HighContrast) checkpointNotice.style.backgroundColor = Color.black;
            checkpointNotice.style.display = DisplayStyle.None;
            mount.Add(checkpointNotice);
            liveSplitCrossed = liveSplitCompact = false;
            liveSplitRivalCount = 0;
            mount.RegisterCallback<GeometryChangedEvent>(change =>
            {
                liveSplitCompact = change.newRect.width < 850 * Scale;
                UpdateLiveSplitSlots();
            });

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
            featured.style.flexGrow = 1.8f;
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

            liveSplitReference = SplitLabel(checkpointNotice, "live_split_reference", "", settings.caption_size);
            liveSplitReference.style.color = muted;
            liveSplitReference.style.unityTextAlign = TextAnchor.MiddleCenter;
            liveSplitReference.style.paddingLeft = liveSplitReference.style.paddingRight = 12 * Scale;
            for (int i = 0; i < liveSplitRows.Length; i++)
            {
                var row = new TimingAthleteRow(Scale, broadcast: true) { name = "live_split_rival_" + i };
                row.style.height = StyleKeyword.Auto;
                row.style.flexGrow = 1;
                row.style.flexBasis = 0;
                row.style.minWidth = 0;
                row.style.marginBottom = 0;
                row.style.display = DisplayStyle.None;
                row.style.paddingLeft = row.style.paddingRight = 12 * Scale;
                liveSplitRows[i] = row;
                checkpointNotice.Add(row);
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
            if (!split.Visible) return;

            var culture = CultureInfo.GetCultureInfo(Locale == "ru" ? "ru-RU" : "en-US");
            liveSplitHeading.text = L("ОТСЕЧКА ", "SPLIT ") + host.DistanceView.Kilometers(split.RouteMeters, culture) + L(" КМ", " KM");
            liveSplitStage.text = split.Crossed
                ? L("МЕСТО ", "RANK ") + split.Rank + " / " + split.Arrivals
                : L("ДО ЛИНИИ ", "TO LINE ") + Math.Ceiling(split.RemainingMeters) + L(" М", " M");
            liveSplitTime.text = GameplayHudModel.FormatClock(split.Seconds);
            liveSplitTrackFill.style.width = Length.Percent(100 * (1 - (float)(split.RemainingMeters / reviewSettings.checkpoint_approach_m)));

            var difference = split.DifferenceSeconds;
            string gap = difference.HasValue ? GameplayHudModel.FormatGap(Math.Abs(difference.Value), culture) : "—";
            liveSplitDifference.text = split.Crossed && split.Rank == 1 ? L("ЛИДЕР", "LEADER") : difference < 0 ? "−" + gap.Substring(1) : gap;
            liveSplitDifference.style.color = difference > 0 && !HighContrast ? FrostlineStyle.Danger : accent;
            if (liveSplitStandings == null || liveSplitOrdinal != split.Ordinal || liveSplitStandings.Count != split.Arrivals || liveSplitTick > field.Tick.Value)
            {
                liveSplitStandings = CheckpointStandings.Capture(field, split.Ordinal);
                liveSplitOrdinal = split.Ordinal;
                BindLiveSplitRoster(split);
            }
            else if (liveSplitCrossed != split.Crossed)
                BindLiveSplitRoster(split);
            liveSplitTick = field.Tick.Value;
        }

        private void BindLiveSplitRoster(LiveSplit split)
        {
            if (liveSplitCrossed != split.Crossed)
            {
                liveSplitCrossed = split.Crossed;
                if (split.Crossed) liveSplitFeatured.PlaceInFront(liveSplitRows[0]);
                else liveSplitFeatured.PlaceBehind(liveSplitReference);
                liveSplitFeatured.style.flexGrow = split.Crossed ? 1 : 1.8f;
            }

            liveSplitAthlete.text = liveSplitAthlete.tooltip = broadcastNames.TryGetValue(athleteId, out var name) ? name :
                string.IsNullOrEmpty(displayName) ? L("Вы", "You") : displayName;
            string playerNation = broadcastNations.TryGetValue(athleteId, out var nation) ? nation : "";
            liveSplitFlag.SetNation(playerNation);
            liveSplitNation.text = L("ВЫ", "YOU") + (playerNation.Length == 0 ? "" : "  ·  " + playerNation);

            if (split.Crossed)
            {
                liveSplitRivalCount = split.Rank > 1 && liveSplitStandings.Count > 0 ? 1 : 0;
                if (liveSplitRivalCount != 0) liveSplitRivals[0] = liveSplitStandings.Get(0);
                checkpointNotice.style.width = (liveSplitRivalCount == 0 ? 550 : 850) * Scale;
            }
            else
            {
                liveSplitRivalCount = liveSplitStandings.CopyBroadcastRivals(athleteId, liveSplitRivals);
                checkpointNotice.style.width = 1040 * Scale;
            }
            UpdateLiveSplitSlots();
            for (int i = 0; i < liveSplitRivalCount; i++)
            {
                var row = liveSplitRows[i];
                var rival = liveSplitRivals[i];
                string rivalName = broadcastNames.TryGetValue(rival.AthleteId, out var stored) ? stored : L("Спортсмен", "Athlete") + " " + rival.Rank;
                string rivalNation = broadcastNations.TryGetValue(rival.AthleteId, out var code) ? code : "";
                row.BindCheckpoint(rival.AthleteId, rival.Rank, rivalName, rivalNation, rival.Seconds, rival.GapSeconds, Locale, false);
            }
        }

        private void UpdateLiveSplitSlots()
        {
            liveSplitReference.style.display = !liveSplitCrossed && liveSplitRivalCount == 0
                ? DisplayStyle.Flex : DisplayStyle.None;
            liveSplitReference.text = L("Ещё нет времени лидера", "No time to beat yet");
            for (int i = 0; i < liveSplitRows.Length; i++)
                liveSplitRows[i].style.display = i < liveSplitRivalCount && (i == 0 || !liveSplitCompact)
                    ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

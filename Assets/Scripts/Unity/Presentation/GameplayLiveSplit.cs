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
        private VisualElement checkpointNotice, liveSplitTrackFill;
        private Label liveSplitHeading, liveSplitStage, liveSplitAthlete, liveSplitNation, liveSplitTime,
            liveSplitDifference, liveSplitClockCaption, liveSplitArrivalCount, liveSplitReference;
        private NationFlagGraphic liveSplitFlag;
        private readonly TimingAthleteRow[] liveSplitRows = new TimingAthleteRow[3];
        private readonly CheckpointStanding[] liveSplitRivals = new CheckpointStanding[3];
        private CheckpointStandings liveSplitStandings;
        private int liveSplitOrdinal;
        private long liveSplitTick;

        private void BuildLiveSplit()
        {
            // A centered mount lets the panel fit between the side HUDs and drop below them on narrower screens.
            var mount = new VisualElement { pickingMode = PickingMode.Ignore };
            mount.style.position = Position.Absolute;
            mount.style.left = mount.style.right = 0;
            mount.style.top = settings.edge;
            mount.style.alignItems = Align.Center;
            Root.Add(mount);

            checkpointNotice = new VisualElement { name = "checkpoint_notice", pickingMode = PickingMode.Ignore };
            FrostlineStyle.SurfaceCard(checkpointNotice, true);
            checkpointNotice.style.width = 464 * Scale;
            checkpointNotice.style.maxWidth = Length.Percent(48);
            checkpointNotice.style.overflow = Overflow.Hidden;
            if (HighContrast) checkpointNotice.style.backgroundColor = Color.black;
            checkpointNotice.style.display = DisplayStyle.None;
            mount.Add(checkpointNotice);
            checkpointNotice.RegisterCallback<GeometryChangedEvent>(change =>
            {
                float space = (mount.layout.width - change.newRect.width) / 2;
                float top = settings.edge + (space < settings.edge + settings.left_width * Scale ? 92 * Scale : 0);
                if (mount.style.top.value.value != top) mount.style.top = top;
            });

            var header = Row(checkpointNotice);
            header.style.paddingLeft = header.style.paddingRight = 12 * Scale;
            header.style.height = 34 * Scale;
            var live = SplitLabel(header, "live_split_live", "● LIVE", settings.caption_size, true);
            live.style.color = accent;
            live.style.marginRight = 12 * Scale;
            live.style.flexShrink = 0;
            liveSplitHeading = SplitLabel(header, "live_split_heading", "", settings.body_size, true);
            liveSplitHeading.style.flexGrow = 1;
            liveSplitStage = SplitLabel(header, "live_split_stage", "", settings.caption_size, true);
            liveSplitStage.style.color = accent;
            liveSplitStage.style.marginLeft = 6 * Scale;
            liveSplitStage.style.flexShrink = 0;

            var featured = Row(checkpointNotice);
            featured.style.backgroundColor = HighContrast ? Color.black : FrostlineStyle.Surface;
            featured.style.borderLeftWidth = 3;
            featured.style.borderLeftColor = accent;
            featured.style.paddingLeft = 9 * Scale;
            featured.style.paddingRight = 12 * Scale;
            featured.style.height = 56 * Scale;
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
            liveSplitClockCaption = SplitLabel(clockColumn, "live_split_clock_caption", "", settings.caption_size, true);
            liveSplitClockCaption.style.color = muted;
            liveSplitDifference = SplitLabel(featured, "live_split_difference", "", settings.body_size, true);
            liveSplitDifference.style.width = 77 * Scale;
            liveSplitDifference.style.flexShrink = 0;
            liveSplitDifference.style.unityTextAlign = TextAnchor.MiddleRight;

            var track = new VisualElement();
            track.style.height = 2;
            track.style.backgroundColor = new Color(accent.r, accent.g, accent.b, .18f);
            checkpointNotice.Add(track);
            liveSplitTrackFill = new VisualElement { name = "live_split_progress" };
            liveSplitTrackFill.style.height = 2;
            liveSplitTrackFill.style.backgroundColor = accent;
            track.Add(liveSplitTrackFill);

            var tableHeader = Row(checkpointNotice);
            tableHeader.style.paddingLeft = tableHeader.style.paddingRight = 12 * Scale;
            tableHeader.style.height = 26 * Scale;
            SplitLabel(tableHeader, "live_split_rivals_title", L("СОПЕРНИКИ НА ОТСЕЧКЕ", "RIVALS AT THE SPLIT"), settings.caption_size, true).style.color = muted;
            liveSplitArrivalCount = SplitLabel(tableHeader, "live_split_arrivals", "", settings.caption_size, true);
            liveSplitArrivalCount.style.color = muted;
            liveSplitReference = SplitLabel(checkpointNotice, "live_split_reference", "", settings.caption_size);
            liveSplitReference.style.color = muted;
            liveSplitReference.style.paddingLeft = 12 * Scale;
            liveSplitReference.style.paddingBottom = 10 * Scale;
            for (int i = 0; i < liveSplitRows.Length; i++)
            {
                var row = new TimingAthleteRow(Scale, broadcast: true) { name = "live_split_rival_" + i };
                row.style.height = 28 * Scale;
                row.style.marginLeft = row.style.marginRight = 6 * Scale;
                row.style.marginBottom = 1;
                liveSplitRows[i] = row;
                checkpointNotice.Add(row);
            }
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
            liveSplitClockCaption.text = split.Crossed ? L("НА ОТСЕЧКЕ", "SPLIT TIME") : L("ТЕКУЩЕЕ", "RUNNING TIME");
            liveSplitTrackFill.style.width = Length.Percent(100 * (1 - (float)(split.RemainingMeters / reviewSettings.checkpoint_approach_m)));

            var difference = split.DifferenceSeconds;
            string gap = difference.HasValue ? GameplayHudModel.FormatGap(Math.Abs(difference.Value), culture) : "—";
            liveSplitDifference.text = split.Crossed && split.Rank == 1 ? L("ЛИДЕР", "LEADER") : difference < 0 ? "−" + gap.Substring(1) : gap;
            liveSplitDifference.style.color = difference > 0 && !HighContrast ? FrostlineStyle.Danger : accent;
            if (liveSplitStandings == null || liveSplitOrdinal != split.Ordinal || liveSplitStandings.Count != split.Arrivals || liveSplitTick > field.Tick.Value)
            {
                liveSplitStandings = CheckpointStandings.Capture(field, split.Ordinal);
                liveSplitOrdinal = split.Ordinal;
                BindLiveSplitRoster(split.Arrivals, field.Count);
            }
            liveSplitTick = field.Tick.Value;
        }

        private void BindLiveSplitRoster(int arrivals, int fieldCount)
        {
            liveSplitAthlete.text = liveSplitAthlete.tooltip = broadcastNames.TryGetValue(athleteId, out var name) ? name :
                string.IsNullOrEmpty(displayName) ? L("Вы", "You") : displayName;
            string playerNation = broadcastNations.TryGetValue(athleteId, out var nation) ? nation : "";
            liveSplitFlag.SetNation(playerNation);
            liveSplitNation.text = L("ВЫ", "YOU") + (playerNation.Length == 0 ? "" : "  ·  " + playerNation);

            int count = liveSplitStandings.CopyBroadcastRivals(athleteId, liveSplitRivals);
            liveSplitArrivalCount.text = arrivals + " / " + fieldCount + L(" ПРОШЛИ", " THROUGH");
            liveSplitReference.style.display = count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            liveSplitReference.text = arrivals == 0 ? L("Ещё нет времени лидера", "No time to beat yet") :
                L("Ожидаем результаты соперников", "Waiting for rivals' splits");
            for (int i = 0; i < liveSplitRows.Length; i++)
            {
                var row = liveSplitRows[i];
                row.style.display = i < count ? DisplayStyle.Flex : DisplayStyle.None;
                if (i >= count) continue;
                var rival = liveSplitRivals[i];
                string rivalName = broadcastNames.TryGetValue(rival.AthleteId, out var stored) ? stored : L("Спортсмен", "Athlete") + " " + rival.Rank;
                string rivalNation = broadcastNations.TryGetValue(rival.AthleteId, out var code) ? code : "";
                row.BindCheckpoint(rival.AthleteId, rival.Rank, rivalName, rivalNation, rival.Seconds, rival.GapSeconds, Locale, false);
            }
        }
    }
}

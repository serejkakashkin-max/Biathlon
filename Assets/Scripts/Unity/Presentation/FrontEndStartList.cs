using System;
using System.Globalization;
using Biathlon.Domain;
using Biathlon.Presentation;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private static readonly float[] StartListColumns = { 10, 62, 28 };
        private RaceStartList shownStartList;
        private int startListPage;

        private void RenderRaceStartList(VisualElement actions, VisualElement card, RaceStartList lineup)
        {
            EnsureResultsLayout();
            int pageSize = resultsLayout.rows_per_page;
            if (!ReferenceEquals(shownStartList, lineup))
            {
                shownStartList = lineup;
                startListPage = 0;
            }

            var player = lineup.Row(lineup.PlayerIndex);
            bool simultaneous = lineup.Profile.StartType == CompetitionStartType.Simultaneous;
            string discipline = T("format." + (int)lineup.Profile.Format);
            AddLabel(actions, L("ВАШ НОМЕР", "YOUR BIB"), theme.caption_size);
            AddLabel(actions, player.Bib.ToString("00"), theme.title_size, true).name = "start_list_player_bib";
            AddLabel(actions, player.Name, theme.body_size, true);
            AddLabel(actions, StartListTiming(player, simultaneous), theme.caption_size).name = "start_list_player_time";
            AddButton(actions, "confirm_race_start", L("Старт", "Start"), Race.StartRace, true);
            if (lineup.Count > pageSize)
                AddButton(actions, "start_list_my_bib", L("Найти свой номер", "Find my bib"), () =>
                {
                    startListPage = lineup.PlayerIndex / pageSize;
                    Render();
                });
            AddButton(actions, "back", T("back"), Back);

            var masthead = Tile(card, "start_list_masthead", true);
            masthead.style.borderLeftWidth = 4;
            masthead.style.borderLeftColor = FrostlineStyle.Accent;
            masthead.style.paddingTop = masthead.style.paddingBottom = 9;
            AddLabel(masthead, L("Стартовый протокол", "Start list"), theme.section_size, true).name = "start_list_title";
            var culture = CultureInfo.GetCultureInfo(Locale == "ru" ? "ru-RU" : "en-US");
            string distance = (RaceDistanceBinding.Create(lineup.Profile).TotalMeters / 1000).ToString("0.##", culture);
            AddLabel(masthead, $"{T("woodland")}  /  {discipline}  ·  {distance}{L(" км", " km")}  ·  {lineup.Count} {L("на старте", "starters")}",
                theme.caption_size).style.marginBottom = 0;
            ResultTableRow(card, "start_list_header", new[] { L("Номер", "Bib"), T("results.athlete"),
                simultaneous ? L("Ряд", "Grid row") : L("После первого старта", "After first start") }, true, false, columnWidths: StartListColumns);
            var rows = new VisualElement { name = "start_list_rows" };
            card.Add(rows);
            for (int i = 0; i < lineup.Count; i++)
            {
                var row = lineup.Row(i);
                ResultTableRow(rows, "start_list_row_" + i, new[] { row.Bib.ToString("00"), row.Name,
                    simultaneous ? row.GridRow?.ToString() ?? "—" : StartListTiming(row, false) }, false, row.IsPlayer,
                    row.Nation, columnWidths: StartListColumns);
            }
            PageRows(card, rows, startListPage, pageSize, page => startListPage = page);
        }

        private string StartListTiming(RaceStartRow row, bool simultaneous)
        {
            if (simultaneous) return row.GridRow.HasValue ? L("Ряд ", "Grid row ") + row.GridRow.Value : L("Общий старт", "Simultaneous start");
            if (row.StartOffsetSeconds == 0) return L("Первый старт", "First start");
            var culture = CultureInfo.GetCultureInfo(Locale == "ru" ? "ru-RU" : "en-US");
            return GameplayHudModel.FormatGap(row.StartOffsetSeconds, culture);
        }
    }
}

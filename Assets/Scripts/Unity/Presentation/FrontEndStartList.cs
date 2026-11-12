using System;
using System.Globalization;
using Biathlon.Domain;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private const int StartListPageSize = 15;
        private static readonly float[] StartListColumns = { 13, 55, 32 };
        private RaceStartList shownStartList;
        private int startListPage;

        private void RenderRaceStartList(RaceStartList lineup)
        {
            EnsureResultsLayout();
            menuCamera.enabled = false;
            Root.style.backgroundColor = Color.clear;
            Root.style.paddingTop = Root.style.paddingBottom = Root.style.paddingLeft = Root.style.paddingRight = theme.page_padding;
            if (!ReferenceEquals(shownStartList, lineup))
            {
                shownStartList = lineup;
                startListPage = 0;
                rememberedFocus[Session.Screen] = "confirm_race_start";
            }

            var player = lineup.Row(lineup.PlayerIndex);
            bool simultaneous = lineup.Profile.StartType == CompetitionStartType.Simultaneous;
            string discipline = T("format." + (int)lineup.Profile.Format);
            var card = new VisualElement { name = "start_list_panel" };
            card.style.width = Length.Percent(compactLayout ? 50 : 38);
            card.style.maxWidth = 620;
            card.style.paddingLeft = card.style.paddingRight = 16;
            card.style.paddingTop = card.style.paddingBottom = 12;
            card.style.borderTopWidth = 2;
            card.style.borderTopColor = FrostlineStyle.Accent;
            card.style.backgroundColor = HighContrast ? Color.black : new Color(.036f, .085f, .13f, .88f);
            Root.Add(card);

            AddLabel(card, L("Стартовый состав", "Start list"), theme.section_size, true).name = "start_list_title";
            var culture = CultureInfo.GetCultureInfo(Locale == "ru" ? "ru-RU" : "en-US");
            string distance = (RaceDistanceBinding.Create(lineup.Profile).TotalMeters / 1000).ToString("0.##", culture);
            var subtitle = AddLabel(card, $"{T("woodland")}  /  {discipline}  ·  {distance}{L(" км", " km")}  ·  {lineup.Count} {L("на старте", "starters")}",
                theme.caption_size);
            subtitle.style.color = FrostlineStyle.Muted;
            ResultTableRow(card, "start_list_header", new[] { L("Номер", "Bib"), T("results.athlete"),
                simultaneous ? L("Ряд", "Grid row") : L("Время старта", "Start offset") }, true, false, columnWidths: StartListColumns);
            var rows = new VisualElement { name = "start_list_rows" };
            rows.style.minHeight = Math.Min(lineup.Count, StartListPageSize) * resultsLayout.row_height;
            card.Add(rows);
            int first = startListPage * StartListPageSize;
            int end = Math.Min(first + StartListPageSize, lineup.Count);
            for (int i = first; i < end; i++)
            {
                var row = lineup.Row(i);
                var line = ResultTableRow(rows, "start_list_row_" + i, new[] { row.Bib.ToString("00"), row.Name,
                    simultaneous ? row.GridRow?.ToString() ?? "—" : StartListTiming(row, culture) }, false, row.IsPlayer,
                    row.Nation, columnWidths: StartListColumns);
                line.style.paddingTop = line.style.paddingBottom = 0;
                var bib = (Label)line[0];
                bib.style.unityTextAlign = TextAnchor.MiddleLeft;
                bib.style.color = row.IsPlayer ? FrostlineStyle.Accent : FrostlineStyle.Muted;
            }
            int pages = (lineup.Count + StartListPageSize - 1) / StartListPageSize;
            if (pages > 1)
            {
                var pager = new VisualElement { name = "start_list_pager" };
                pager.style.flexDirection = FlexDirection.Row;
                pager.style.alignItems = Align.Center;
                pager.style.justifyContent = Justify.SpaceBetween;
                card.Add(pager);
                AddButton(pager, "start_list_rows_prev", "←", () => SetStartListPage(startListPage - 1), enabled: startListPage > 0);
                var page = AddLabel(pager, $"{startListPage + 1} / {pages}", theme.caption_size, true);
                page.style.marginBottom = 0;
                AddButton(pager, "start_list_rows_next", "→", () => SetStartListPage(startListPage + 1), enabled: startListPage + 1 < pages);
            }

            var actions = new VisualElement { name = "start_list_actions" };
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.alignItems = Align.Center;
            actions.style.marginTop = 8;
            card.Add(actions);
            AddButton(actions, "confirm_race_start", L("СТАРТ", "START"), Race.StartRace, true);
            var start = actions.Q<Button>("confirm_race_start");
            start.style.flexGrow = 1;
            start.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (pages > 1)
                AddButton(actions, "start_list_my_bib", $"{L("Ваш №", "Your bib")} {player.Bib:00}",
                    () => SetStartListPage(lineup.PlayerIndex / StartListPageSize));
            AddButton(actions, "back", T("back"), Back);

            message = AddLabel(card, LastMessage ?? "", theme.caption_size);
            message.style.color = FrostlineStyle.Danger;
            message.style.marginBottom = 0;
            message.style.display = string.IsNullOrEmpty(LastMessage) ? DisplayStyle.None : DisplayStyle.Flex;
            Root.schedule.Execute(() =>
            {
                if (start.panel == null) return;
                if (rememberedFocus.TryGetValue(Session.Screen, out string id))
                    foreach (var control in controls)
                        if (control.name == id && IsShown(control)) { control.Focus(); return; }
                start.Focus();
            });
        }

        private void SetStartListPage(int page)
        {
            startListPage = page;
            Render();
        }

        private string StartListTiming(RaceStartRow row, CultureInfo culture)
        {
            if (row.StartOffsetSeconds == 0) return L("Первый старт", "First start");
            return GameplayHudModel.FormatGap(row.StartOffsetSeconds, culture);
        }
    }
}

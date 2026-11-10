#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Biathlon.Unity
{
    public static partial class PlayerBuildPipeline
    {
        public static void BuildM167IntegratedAthleteReview() => BuildM167Review("Builds/M16_7_New_Athlete_Review",
            "m167_integrated_athlete_review", "BIATHLON_PULSE_Before_After_1440p.mp4", "");

        public static void BuildM167PaceReview() => BuildM167Review("Builds/M16_7_Pace_Review",
            "m167_pace_review", "BIATHLON_PULSE_Before_After_1440p60.mp4", "Темп рук зависит от скорости. Предел отрисовки 100 FPS.\r\n");

        public static void BuildM167MatReview() => BuildM167Review("Builds/M16_7_Mat_Review",
            "m167_mat_review", "BIATHLON_PULSE_Before_After_Mat_1440p60.mp4",
            "На рубеже спортсмены собирают обе палки в одну руку, кладут их вместе и переставляют лыжи при выходе в стойку.\r\n");

        public static void BuildM167ContinuityReview() => BuildM167Review("Builds/M16_7_Continuity_Review",
            "m167_continuity_review", null,
            "Исправлены рывки при поворотах A/D и при остановке у коврика. Проверьте короткие нажатия, смену направления и обе стойки стрельбы.\r\n");

        public static void BuildM168LiveTimingReview() => BuildM167Review("Builds/M16_8_Live_Timing_Review",
            "m168_live_timing_review", null,
            "На трассе Woodland Basin появились две новые отсечки. Табло показывает время лидера на ближайшей отсечке и ваше место после её пересечения. История доступна по TAB.\r\n",
            "M16.8");

        public static void BuildM168SplitBroadcastReview() => BuildM167Review("Builds/M16_8_Split_Broadcast_Review",
            "m168_split_broadcast_review", null,
            "Эфирное табло отсечки показывает ваш флаг, имя, текущее время и до трёх соперников с результатами. После пересечения линии показано промежуточное место. История доступна по TAB.\r\n",
            "M16.8");

        public static void BuildM168RaceReadabilityReview() => BuildM167Review("Builds/M16_8_Race_Readability_Review",
            "m168_race_readability_review", null,
            "Табло отсечки теперь внизу экрана. Обновлены вид обледеневшего снега и отрисовка Woodland Basin. История отсечек доступна по TAB.\r\n",
            "M16.8");

        public static void BuildM168PerformanceReview() => BuildM167Review("Builds/M16_8_Performance_Review",
            "m168_performance_review", null,
            "Ускорены подготовка экипировки и обновление поз спортсменов. Невидимые детали вне зоны теней пропускаются при отрисовке.\r\n",
            "M16.8");

        public static void BuildM168HitchProbe()
        {
            bool timing=PlayerSettings.enableFrameTimingStats;
            try
            {
                PlayerSettings.enableFrameTimingStats=true;
                BuildM167Review("Builds/M16_8_Hitch_Probe","m168_hitch_probe",null,"Диагностическая сборка полного карьерного заезда.\r\n","M16.8");
            }
            finally{PlayerSettings.enableFrameTimingStats=timing;AssetDatabase.SaveAssets();}
        }

        public static void BuildM168HitchReview() => BuildM167Review("Builds/M16_8_Hitch_Review",
            "m168_hitch_review", null,
            "Устранён лишний пересчёт всего поля при независимом торможении и контакте с краем трассы. Старые повторы используют правила своей записи.\r\n",
            "M16.8", "Коэффициенты движения, баланс, геометрия попаданий и формат сохранений прежние. Новые гонки используют уточнённый расчёт контакта со снегом.\r\n");

        public static void BuildM168SplitHudReview() => BuildM167Review("Builds/M16_8_Split_HUD_Review",
            "m168_split_hud_review", null,
            "После отсечки нижняя полоса показывает лидера и ваш результат. Подсказка дыхания находится над пульсом справа.\r\n",
            "M16.8");

        public static void BuildM168SplitTickerReview() => BuildM167Review("Builds/M16_8_Split_Ticker_Review",
            "m168_split_ticker_review", null,
            "До отсечки мини-окно по очереди показывает три лучших результата. После пересечения оно фиксирует ваш результат и место на момент пересечения.\r\n",
            "M16.8");

        public static void BuildM168SplitScrollReview() => BuildM167Review("Builds/M16_8_Split_Scroll_Review",
            "m168_split_scroll_review", null,
            "До отсечки нижняя полоса листает весь протокол по три спортсмена. При пересечении она фиксирует тройку с вашим выделенным результатом.\r\n",
            "M16.8");

        private static void BuildM167Review(string folder, string id, string comparisonFile, string reviewNote, string stage="M16.7",
            string simulationNote="Физика гонки, баланс, геометрия попаданий и формат сохранений не изменены.\r\n")
        {
            // These names own the existing career directory; branding belongs in the UI.
            if(PlayerSettings.companyName!="DefaultCompany"||PlayerSettings.productName!="Biathlon")
                throw new InvalidOperationException("Preserve the existing career storage identity.");
            Biathlete167RaceAuthoring.EnsureRuntimeResources();
            BuildVisualUpgrade(folder);
            var receipt=JsonUtility.FromJson<VisualBuildReceipt>(File.ReadAllText(Path.Combine(folder,"build-summary.json")));
            receipt.id=id;
            File.WriteAllText(Path.Combine(folder,"build-summary.json"),JsonUtility.ToJson(receipt,true));
            const string credits="Assets/Biathlon/Characters/Biathlete_16_7/Documentation/EQUIPMENT_ATTRIBUTION.md";
            File.Copy(credits,Path.Combine(folder,"M167_ASSET_CREDITS.md"),true);
            File.Copy("Assets/Biathlon/Characters/Biathlete_16_7/Documentation/LICENSE_AND_PROVENANCE.md",Path.Combine(folder,"M167_PROVENANCE.md"),true);
            File.WriteAllText(Path.Combine(folder,"START_TEST.cmd"),
                "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"BIATHLON PULSE "+stage+"\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            // The inherited launcher compares an older motion fallback, not these two assets.
            File.Delete(Path.Combine(folder,"COMPARE_MOTION.cmd"));
            File.WriteAllText(Path.Combine(folder,"milestone.json"),
                "{\"schema_version\":1,\"id\":\""+id+"\",\"stage\":\""+stage+"\",\"status\":\"owner_review\",\"unity\":\""+Application.unityVersion+"\"}");
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),
                "BIATHLON: PULSE / Биатлон: Пульс гонки — "+stage+"\r\n\r\n"+
                "START_TEST.cmd — игра на весь экран. Можно продолжить существующую карьеру.\r\n"+
                "Новый спортсмен внедрён в гонки и редактор внешности. Сохранены индивидуальные лица, цвета сборных, головные уборы, очки, перчатки, ботинки и выбранные лыжи.\r\n"+
                "Периодическое боковое раскачивание убрано. Наклон в поворотах и реакция на столкновения сохранены.\r\n"+
                simulationNote+
                "Проверьте лыжный ход, разгон, подъём, стрельбу лёжа/стоя и внешность соперников.\r\n"+
                reviewNote+
                (string.IsNullOrEmpty(comparisonFile) ? "" : "Видео сравнения: Media/"+comparisonFile+" (WATCH_COMPARISON.cmd).\r\n")+
                (stage=="M16.8"?"М16.7 закрыт владельцем. М16.8 открыт для проверки.\r\n":"М16.6 закрыт владельцем. М16.7 открыт до вашего тестирования.\r\n")+
                "Лицензии ассетов: THIRD_PARTY_CREDITS.txt, M167_ASSET_CREDITS.md и OFL-файлы.\r\n");
            AssetDatabase.SaveAssets();
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Biathlon.Unity
{
    public static partial class PlayerBuildPipeline
    {
        public static void BuildM167IntegratedAthleteReview() => BuildM167Review("Builds/M16_7_New_Athlete_Review", false);

        public static void BuildM167PaceReview() => BuildM167Review("Builds/M16_7_Pace_Review", true);

        private static void BuildM167Review(string folder, bool paceReview)
        {
            // These names own the existing career directory; branding belongs in the UI.
            if(PlayerSettings.companyName!="DefaultCompany"||PlayerSettings.productName!="Biathlon")
                throw new InvalidOperationException("Preserve the existing career storage identity.");
            Biathlete167RaceAuthoring.EnsureRuntimeResources();
            BuildVisualUpgrade(folder);
            var receipt=JsonUtility.FromJson<VisualBuildReceipt>(File.ReadAllText(Path.Combine(folder,"build-summary.json")));
            receipt.id=paceReview ? "m167_pace_review" : "m167_integrated_athlete_review";
            File.WriteAllText(Path.Combine(folder,"build-summary.json"),JsonUtility.ToJson(receipt,true));
            const string credits="Assets/Biathlon/Characters/Biathlete_16_7/Documentation/EQUIPMENT_ATTRIBUTION.md";
            File.Copy(credits,Path.Combine(folder,"M167_ASSET_CREDITS.md"),true);
            File.Copy("Assets/Biathlon/Characters/Biathlete_16_7/Documentation/LICENSE_AND_PROVENANCE.md",Path.Combine(folder,"M167_PROVENANCE.md"),true);
            File.WriteAllText(Path.Combine(folder,"START_TEST.cmd"),
                "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"BIATHLON PULSE M16.7\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            // The inherited launcher compares an older motion fallback, not these two assets.
            File.Delete(Path.Combine(folder,"COMPARE_MOTION.cmd"));
            File.WriteAllText(Path.Combine(folder,"milestone.json"),
                "{\"schema_version\":1,\"id\":\"m167_integrated_athlete_review\",\"stage\":\"M16.7\",\"status\":\"owner_review\",\"unity\":\""+Application.unityVersion+"\"}");
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),
                "BIATHLON: PULSE / Биатлон: Пульс гонки — М16.7\r\n\r\n"+
                "START_TEST.cmd — игра на весь экран. Можно продолжить существующую карьеру.\r\n"+
                "Новый спортсмен внедрён в гонки и редактор внешности. Сохранены индивидуальные лица, цвета сборных, головные уборы, очки, перчатки, ботинки и выбранные лыжи.\r\n"+
                "Периодическое боковое раскачивание убрано. Наклон в поворотах и реакция на столкновения сохранены.\r\n"+
                "Физика гонки, баланс, геометрия попаданий и формат сохранений не изменены.\r\n"+
                "Проверьте лыжный ход, разгон, подъём, стрельбу лёжа/стоя и внешность соперников.\r\n"+
                (paceReview ? "Темп рук зависит от скорости. Предел отрисовки 100 FPS.\r\nВидео сравнения: Media/BIATHLON_PULSE_Before_After_1440p60.mp4 (WATCH_COMPARISON.cmd).\r\n" :
                    "Видео сравнения: Media/BIATHLON_PULSE_Before_After_1440p.mp4 (WATCH_COMPARISON.cmd).\r\n")+
                "М16.6 закрыт владельцем. М16.7 открыт до вашего тестирования.\r\n"+
                "Лицензии ассетов: THIRD_PARTY_CREDITS.txt, M167_ASSET_CREDITS.md и OFL-файлы.\r\n");
            AssetDatabase.SaveAssets();
        }
    }
}
#endif

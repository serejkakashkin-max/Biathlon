#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Biathlon.Unity
{
    public static partial class PlayerBuildPipeline
    {
        public static void BuildVisualUpgradeBaseline()=>BuildVisualUpgrade("Builds/M15_5_Baseline_Review");
        public static void BuildVisualUpgradeFinal()=>BuildVisualUpgrade("Builds/M15_5_Visual_Review");
        public static void BuildElbowBaseline()=>BuildVisualUpgrade("Builds/M15_5_Elbow_Baseline");
        public static void BuildSprintCarryReview()
        {
            const string folder="Builds/M15_5_Sprint_Rifle_Review";
            BuildVisualUpgrade(folder);
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),
                "BIATHLON M15.5 — темп рук при ускорении и винтовка на спине\r\n\r\n"+
                "START_TEST.cmd — обычная игра, полный экран; существующая карьера совместима.\r\n"+
                "Частота толчков при Shift снижена на 28%; ритм шагов и скорость гонки сохранены.\r\n"+
                "Винтовка лежит широкой стороной ложи вдоль спины, ствол направлен вверх.\r\n"+
                "Проверьте разгон, включение/отпускание Shift и перенос винтовки к стрельбе/назад.\r\n"+
                "Видео до/после: docs/quality/m15-5/sprint-carry-refinement/review.html в проекте.\r\n"+
                "COMPARE_MOTION.cmd использует старый procedural fallback, а не предыдущий тестовый билд.\r\n"+
                "Баланс, управление и качество графики не перенастраивались. M16 пока не начат.\r\n");
        }
        public static void BuildArmReview()
        {
            const string folder="Builds/M15_5_Arm_Review";
            BuildVisualUpgrade(folder);
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),
                "BIATHLON M15.5 — плавность рук и зазор от корпуса\r\n\r\n"+
                "START_TEST.cmd — обычная игра, полный экран; можно продолжить существующую карьеру.\r\n"+
                "Руки отведены от корпуса, толчок и возврат палок сглажены, особенно при ускорении.\r\n"+
                "Одобренные движения ног и туловища сохранены точно. Баланс и управление не изменены.\r\n"+
                "Проверьте руки на обычном ходу, при Shift, на подъёмах и поворотах.\r\n"+
                "Крупные планы до/после: docs/quality/m15-5/arm-refinement/review.html в проекте.\r\n"+
                "COMPARE_MOTION.cmd показывает старый procedural fallback; это не предыдущий тестовый билд.\r\n"+
                "VU-B ожидает вашей визуальной проверки. M16 не начат.\r\n");
        }
        public static void BuildElbowReview()
        {
            const string folder="Builds/M15_5_Elbow_Review";
            BuildVisualUpgrade(folder);
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),
                "BIATHLON M15.5 — исправление локтей\r\n\r\n"+
                "START_TEST.cmd — обычная игра и существующая карьера.\r\n"+
                "Проверьте локти при разгоне, толчке и возврате палок, ускорении, на поворотах и подъёмах.\r\n"+
                "Предплечье сгибается вокруг одной оси; кисти идут относительно плеч, без выворачивания локтей.\r\n"+
                "COMPARE_MOTION.cmd — лаборатория: слева старый procedural fallback, справа новое движение.\r\n"+
                "Крупные планы до/после: docs/quality/m15-5/elbow-refinement/review.html в проекте.\r\n"+
                "Одобренные материалы, модель, графика, интерфейс, звук и баланс сохранены.\r\n"+
                "M16 не начат; визуальное подтверждение анимации ожидается от владельца.\r\n");
        }
        public static void BuildAnimationRefinement()
        {
            const string folder="Builds/M15_5_Animation_Review";
            BuildVisualUpgrade(folder);
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),
                "BIATHLON M15.5 — повторная проверка анимации\r\n\r\n"+
                "START_TEST.cmd — обычная игра; можно продолжить существующую карьеру.\r\n"+
                "Проверьте руки и палки в ожидании старта масстарта, при разгоне, на поворотах, перестроениях и подъёмах.\r\n"+
                "Палки теперь работают в поворотах и на подъёмах; руки проходят постановку, толчок и возврат.\r\n"+
                "COMPARE_MOTION.cmd — лаборатория: слева прежний процедурный fallback, справа доработанная анимация.\r\n"+
                "Одобренные модель, материалы, снег, погода, интерфейс, звук и баланс сохранены.\r\n"+
                "Анимация авторская и процедурная, без motion capture. M16 начнётся после вашей проверки анимации.\r\n");
        }
        private static void BuildVisualUpgrade(string folder)
        {
            ValidateProductionBuildDirectory(folder);if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Idle editor required.");
            FrontEndAuthoring.EnsurePanelTemplate();M15AudioAuthoring.EnsureSoundBank();Directory.CreateDirectory(folder);
            WithProductionBuildProfiles(()=>
            {
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FrontEndAuthoring.ScenePath,"Assets/Scenes/SkiMotionComparison.unity"},locationPathName=Path.Combine(folder,"Biathlon.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                var s=report.summary;File.WriteAllText(Path.Combine(folder,"build-summary.json"),JsonUtility.ToJson(new VisualBuildReceipt{result=s.result.ToString(),errors=s.totalErrors,warnings=s.totalWarnings,bytes=s.totalSize,seconds=s.totalTime.TotalSeconds,unity=Application.unityVersion},true));
                if(s.result!=BuildResult.Succeeded)throw new InvalidOperationException("Visual build failed.");
            });
            File.Copy("Assets/Resources/Biathlon/G2ThirdPartyCredits.txt",Path.Combine(folder,"THIRD_PARTY_CREDITS.txt"),true);
            foreach(string f in Directory.GetFiles("Assets/Resources/Biathlon/UI/Fonts","*OFL*.txt"))File.Copy(f,Path.Combine(folder,Path.GetFileName(f)),true);
            File.WriteAllText(Path.Combine(folder,"START_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon visual review\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            File.WriteAllText(Path.Combine(folder,"COMPARE_MOTION.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon motion comparison\" \"%~dp0Biathlon.exe\" -biathlonMotionLab -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),
                "BIATHLON M15.5 — визуал и анимация, тестовая сборка\r\n\r\n"+
                "START_TEST.cmd — обычная игра и существующая карьера.\r\n"+
                "COMPARE_MOTION.cmd — сравнение движений: слева прежние, справа новые; 12 состояний.\r\n"+
                "В настройках графики доступны Performance / High / Ultra. Текстуры и модель спортсмена общие.\r\n\r\n"+
                "Проверьте коньковый ход, подъём, ускорение, лыжи/палки, стрельбу лёжа и стоя, снег и погоду.\r\n"+
                "Существующие механики, баланс, управление, сохранения и звук в этом этапе не перенастраивались.\r\n"+
                "Новые движения авторские и процедурные, не motion capture. Переход к M16 после вашей проверки.\r\n");
        }
        [Serializable]private sealed class VisualBuildReceipt
        {public int schema_version=1;public string id="m15_5_visual_review",result,unity;public int errors,warnings;public ulong bytes;public double seconds;}
    }
}
#endif

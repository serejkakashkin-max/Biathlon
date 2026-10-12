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
        public const string M14ReviewDirectory="Builds/M14Revision20261012";
        public static void BuildWindowsM14Review()
        {
            ValidateProductionBuildDirectory(M14ReviewDirectory);
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Build requires idle Edit Mode.");
            FrontEndAuthoring.EnsurePanelTemplate();
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new InvalidOperationException("Windows build support required.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(FrontEndAuthoring.ScenePath)==null)throw new FileNotFoundException("Missing front-end scene.");
            Directory.CreateDirectory(M14ReviewDirectory);
            WithProductionBuildProfiles(()=>
            {
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FrontEndAuthoring.ScenePath},locationPathName=Path.Combine(M14ReviewDirectory,"Biathlon.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                var summary=report.summary;
                File.WriteAllText(Path.Combine(M14ReviewDirectory,"build-summary.json"),JsonUtility.ToJson(new M14BuildEvidence{result=summary.result.ToString(),errors=summary.totalErrors,warnings=summary.totalWarnings,bytes=summary.totalSize,seconds=summary.totalTime.TotalSeconds,unity=Application.unityVersion},true));
                if(summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("M14 build failed: "+summary.result);
            });
            File.Copy("Assets/Resources/Biathlon/G2ThirdPartyCredits.txt",Path.Combine(M14ReviewDirectory,"THIRD_PARTY_CREDITS.txt"),true);
            File.WriteAllText(Path.Combine(M14ReviewDirectory,"START_M14_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon M14\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080\r\n");
            File.WriteAllText(Path.Combine(M14ReviewDirectory,"READ_ME_RU.txt"),
                "BIATHLON — M14, тестовая сборка 12.10.2026\r\n\r\nЗапуск: START_M14_TEST.cmd. M14 готов к вашей проверке; окончательное закрытие после вашего решения.\r\n"+
                "Меню: новая карьера, продолжить, настройки. Стрелки/геймпад — выбор, Enter/A — открыть, Esc/B — назад. Настройки: RU/EN, размер текста, контраст.\r\n"+
                "Карьера: календарь, тренировки, экипировка, таблица, история, соперничество, повторы. Первый спринт на 6-й неделе: выберите тренировки/восстановление или пять недель восстановления. Перед гонкой показаны погода, состояние, лыжи и правила.\r\n"+
                "Лыжи: W увеличить темп, S тормоз, Shift атака, Ctrl восстановление, A/D быстрое ручное руление, удерживать Space для низкой стойки. F1 помощь, F2 компактная ТВ-таблица, F3 телеметрия, Esc пауза. На малой скорости A/D разворачивает спортсмена шагами с расходом мощности.\r\n"+
                "Отсечки: 6 линий на круге, место и отставание при пересечении; Tab таблица последней пройденной отсечки. Перед рубежом полоса пульса объясняет спокойное/повышенное/сильное колебание прицела.\r\n"+
                "Стрельба: ПКМ затвор до первого выстрела и после каждого кроме пятого, ЛКМ выстрел, Space дыхание, F точный прицел, Tab обзор. Подъезд по стрелкам и короткий выезд с коврика автоматические.\r\n"+
                "После личного финиша: [ / ] выбор соперника, C камера (сзади/сверху/сбоку), Enter или кнопка Завершить гонку — ускоренная симуляция оставшихся участников. После финиша всего поля сохраняются протокол, очки/место и повтор. Повторы: пауза Space, перемотка стрелками или шкалой, скорость 1/2/4, камера C или список, выбор спортсмена. Esc возврат. Новые повторы требуют совместимых определений трассы/симуляции.\r\n"+
                "Профили и повторы: %USERPROFILE%\\AppData\\LocalLow\\DefaultCompany\\Biathlon (точный путь указан в startup-smoke.json). Записи старых билдов не создаются задним числом.\r\n");
        }
        [Serializable]private sealed class M14BuildEvidence
        {public int schema_version=1;public string id="m14_windows_review_20261012",result,unity,startup_scene=FrontEndAuthoring.ScenePath;public int errors,warnings;public ulong bytes;public double seconds;}
    }
}
#endif

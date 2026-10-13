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
        public const string M15ReviewDirectory="Builds/M15_Audio_Review";
        public static void BuildWindowsM15Review()
        {
            ValidateProductionBuildDirectory(M15ReviewDirectory);
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Build requires idle Edit Mode.");
            FrontEndAuthoring.EnsurePanelTemplate();M15AudioAuthoring.EnsureSoundBank();
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new InvalidOperationException("Windows build support required.");
            Directory.CreateDirectory(M15ReviewDirectory);
            WithProductionBuildProfiles(()=>
            {
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FrontEndAuthoring.ScenePath},locationPathName=Path.Combine(M15ReviewDirectory,"Biathlon.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                var s=report.summary;
                File.WriteAllText(Path.Combine(M15ReviewDirectory,"build-summary.json"),JsonUtility.ToJson(new M15Receipt{result=s.result.ToString(),errors=s.totalErrors,warnings=s.totalWarnings,bytes=s.totalSize,seconds=s.totalTime.TotalSeconds,unity=Application.unityVersion},true));
                if(s.result!=BuildResult.Succeeded)throw new InvalidOperationException("M15 build failed: "+s.result);
            });
            File.Copy("Assets/Resources/Biathlon/G2ThirdPartyCredits.txt",Path.Combine(M15ReviewDirectory,"THIRD_PARTY_CREDITS.txt"),true);
            foreach(string notice in Directory.GetFiles("Assets/Resources/Biathlon/UI/Fonts","*OFL*.txt"))File.Copy(notice,Path.Combine(M15ReviewDirectory,Path.GetFileName(notice)),true);
            File.Copy("Assets/Resources/Biathlon/Audio/sources.json",Path.Combine(M15ReviewDirectory,"AUDIO_SOURCES.json"),true);
            File.WriteAllText(Path.Combine(M15ReviewDirectory,"START_M15_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon M15\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080\r\n");
            File.WriteAllText(Path.Combine(M15ReviewDirectory,"READ_ME_RU.txt"),
                "BIATHLON — M15 AUDIO, тестовая сборка 13.10.2026\r\n\r\nЗапуск: START_M15_TEST.cmd\r\n"+
                "Добавлены звуки лыж и палок по скорости/снегу, дыхание, пульс, выстрелы с отражением, затвор, мишени, ветер, снегопад, трибуны и меню.\r\n"+
                "Настройки → Звук: общая громкость, эффекты, дыхание/пульс, окружение, интерфейс, атмосфера меню. Мягкий микс смягчает резкие звуки. По умолчанию игра замолкает при переключении в другое окно.\r\n"+
                "На рубеже пульс связан с ритмом симуляции. Space задерживает дыхание; пульс продолжается. ПКМ затвор, ЛКМ выстрел. Соседние выстрелы пространственные.\r\n"+
                "Пауза, перемотка и ускоренное завершение не проигрывают пропущенные звуки. Повтор на обычной скорости озвучен.\r\n"+
                "Управление, баланс и качество графики сохранены. Профили совместимы. Проверьте в наушниках масстарт, рубеж и повтор; сообщите о громкости/утомляемости микса.\r\n");
        }
        [Serializable]private sealed class M15Receipt
        {public int schema_version=1;public string id="m15_audio_review_20261013",result,unity;public int errors,warnings;public ulong bytes;public double seconds;}
    }
}
#endif

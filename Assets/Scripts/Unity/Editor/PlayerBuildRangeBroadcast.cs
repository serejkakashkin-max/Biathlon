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
        public const string RangeBroadcastReviewDirectory="Builds/Range_Broadcast_Review";
        public static void BuildWindowsRangeBroadcastReview()
        {
            ValidateProductionBuildDirectory(RangeBroadcastReviewDirectory);
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Idle Edit Mode required.");
            FrontEndAuthoring.EnsurePanelTemplate();M15AudioAuthoring.EnsureSoundBank();Directory.CreateDirectory(RangeBroadcastReviewDirectory);
            WithProductionBuildProfiles(()=>
            {
                var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FrontEndAuthoring.ScenePath},locationPathName=Path.Combine(RangeBroadcastReviewDirectory,"Biathlon.exe"),
                    target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});var s=r.summary;
                File.WriteAllText(Path.Combine(RangeBroadcastReviewDirectory,"build-summary.json"),JsonUtility.ToJson(new RangeBoardBuildReceipt{result=s.result.ToString(),errors=s.totalErrors,warnings=s.totalWarnings,bytes=s.totalSize,seconds=s.totalTime.TotalSeconds,unity=Application.unityVersion},true));
                if(s.result!=BuildResult.Succeeded)throw new InvalidOperationException("Range broadcast build failed: "+s.result);
            });
            File.Copy("Assets/Resources/Biathlon/G2ThirdPartyCredits.txt",Path.Combine(RangeBroadcastReviewDirectory,"THIRD_PARTY_CREDITS.txt"),true);
            foreach(string f in Directory.GetFiles("Assets/Resources/Biathlon/UI/Fonts","*OFL*.txt"))File.Copy(f,Path.Combine(RangeBroadcastReviewDirectory,Path.GetFileName(f)),true);
            File.Copy("Assets/Resources/Biathlon/Audio/sources.json",Path.Combine(RangeBroadcastReviewDirectory,"AUDIO_SOURCES.json"),true);
            File.WriteAllText(Path.Combine(RangeBroadcastReviewDirectory,"START_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon Range Broadcast\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            File.WriteAllText(Path.Combine(RangeBroadcastReviewDirectory,"READ_ME_RU.txt"),"BIATHLON — СТРЕЛЬБА СОПЕРНИКОВ, 13.10.2026\r\nЗапуск: START_TEST.cmd\r\n\r\n"+
                "Панель справа открывается за 250 метров до входа на рубеж и остаётся до 250 метров после выхода. На штрафном круге она видна. Флаги, имена и номер коврика берутся из гонки.\r\n"+
                "Белая мишень — закрыта. Точки слева от пяти мишеней — промахи. После промаха строка коротко мерцает красным. При уменьшенной анимации подсветка спокойная.\r\n"+
                "В плотном поле строки автоматически меняются каждые 5 секунд. Наш спортсмен и свежие промахи остаются на виду. TAB открывает таблицу отсечек, после закрытия панель возвращается.\r\n"+
                "Музыка меню, мягкие звуки палок и исправленный отсчёт 3–2–1 сохранены.\r\n");
        }
        [Serializable]private sealed class RangeBoardBuildReceipt
        {public int schema_version=1;public string id="range_broadcast_review_20261013",result,unity;public int errors,warnings;public ulong bytes;public double seconds;}
    }
}
#endif

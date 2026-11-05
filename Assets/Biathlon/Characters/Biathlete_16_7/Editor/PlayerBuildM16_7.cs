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
        public const string M167ReviewDirectory="Builds/M16_7_Biathlete_Studio_Review";
        public static void BuildM167Studio()
        {
            ValidateProductionBuildDirectory(M167ReviewDirectory);
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Idle compiled editor required.");
            Directory.CreateDirectory(M167ReviewDirectory);
            WithProductionBuildProfiles(()=>
            {
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Biathlete167Authoring.ScenePath,FrontEndAuthoring.ScenePath,"Assets/Scenes/SkiMotionComparison.unity"},locationPathName=Path.Combine(M167ReviewDirectory,"Biathlon.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                var s=report.summary;File.WriteAllText(Path.Combine(M167ReviewDirectory,"build-summary.json"),JsonUtility.ToJson(new M167BuildReceipt{result=s.result.ToString(),errors=s.totalErrors,warnings=s.totalWarnings,bytes=s.totalSize,seconds=s.totalTime.TotalSeconds,unity=Application.unityVersion},true));
                if(s.result!=BuildResult.Succeeded)throw new InvalidOperationException("M16.7 studio build failed.");
            });
            File.WriteAllText(Path.Combine(M167ReviewDirectory,"START_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"BIATHLON PULSE M16.7\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            File.WriteAllText(Path.Combine(M167ReviewDirectory,"READ_ME.txt"),"BIATHLON: PULSE — М16.7, мастерская спортсмена\r\n\r\nSTART_TEST.cmd — полный экран. Правая кнопка мыши вращает камеру, колесо приближает. Слева доступны шесть ракурсов и четыре позы. Кнопка «Перейти в игру» открывает обычную игру и существующие карьеры.\r\n\r\nМ16.6 закрыт владельцем. М16.7 остаётся открытым: модель и материалы проходят визуальную доработку; качество профессионального референса ещё не подтверждено. Новый Humanoid доступен в отдельной сцене и Prefab; действующий G2 в гонках сохранён до проверки полноценной анимации нового спортсмена.\r\n");
            File.Copy(Biathlete167Authoring.Root+"/Documentation/LICENSE_AND_PROVENANCE.md",Path.Combine(M167ReviewDirectory,"M16_7_ASSET_CREDITS.txt"),true);
            File.Copy("Assets/Resources/Biathlon/G2ThirdPartyCredits.txt",Path.Combine(M167ReviewDirectory,"THIRD_PARTY_CREDITS.txt"),true);
            foreach(string path in Directory.GetFiles("Assets/Resources/Biathlon/UI/Fonts","*OFL*.txt"))File.Copy(path,Path.Combine(M167ReviewDirectory,Path.GetFileName(path)),true);
        }
        [Serializable] private sealed class M167BuildReceipt
        {public int schema_version=1;public string id="biathlete_16_7_studio_build",result,unity;public int errors,warnings;public ulong bytes;public double seconds;}
    }
}
#endif

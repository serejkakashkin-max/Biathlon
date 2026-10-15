#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace Biathlon.Unity
{
    public static partial class PlayerBuildPipeline
    {
        public static void BuildCareerCreatorReview()
        {
            const string folder="Builds/M15_6_Career_Creator_Review";
            BuildVisualUpgrade(folder);
            string receipt=Path.Combine(folder,"build-summary.json");File.WriteAllText(receipt,File.ReadAllText(receipt).Replace("m15_5_visual_review","m15_6_career_creator_review"));
            File.WriteAllText(Path.Combine(folder,"START_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon M15.6\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),
                "BIATHLON — M15.6 / создание спортсмена и глубокая карьера\r\n\r\n"+
                "Запуск: START_TEST.cmd, полный экран.\r\n"+
                "Новая карьера: создать своего или выбрать готового вымышленного спортсмена.\r\n"+
                "Три уровня старта, ограниченный бюджет13 навыков, живая3D-внешность.\r\n"+
                "Индивидуальное развитие, возрастные кривые, постоянный пул соперников, историяOVR и досье.\r\n"+
                "Старые сохранения мигрируют при загрузке; история гонок и исходные характеристики сохраняются.\r\n"+
                "Перед проверкой новой системы рекомендуется создать отдельную новую карьеру.\r\n"+
                "W/S — темп; Shift — рывок; A/D — руление; Space — стойка/дыхание; ПКМ — затвор; ЛКМ — выстрел; Tab — отсечки.\r\n"+
                "Реальные спортсмены и защищённые портреты не включены. M16 не начат.\r\n"+
                "M15.6 ожидает вашего теста и решения о закрытии.\r\n");
            File.WriteAllText(Path.Combine(folder,"milestone.json"),"{\"schema_version\":1,\"id\":\"m15_6_career_creator_review\",\"status\":\"owner_review\",\"unity\":\""+Application.unityVersion+"\"}");
        }
    }
}
#endif

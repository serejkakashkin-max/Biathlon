#if UNITY_EDITOR
using System.IO;
using UnityEngine;
namespace Biathlon.Unity
{
    public static partial class PlayerBuildPipeline
    {
        public static void BuildReliabilityReview()
        {
            const string folder="Builds/M16_Reliability_Review";BuildVisualUpgrade(folder);
            string receipt=Path.Combine(folder,"build-summary.json");File.WriteAllText(receipt,File.ReadAllText(receipt).Replace("m15_5_visual_review","m16_reliability_review"));
            File.WriteAllText(Path.Combine(folder,"START_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon M16\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),"BIATHLON — M16 / Надёжность\r\n\r\nSTART_TEST.cmd — игра в полный экран.\r\n\r\nКарьеры v1/v2/v3 поддерживаются. При повреждении основного файла восстанавливается резервная копия той же карьеры или точка перед стартом.\r\nДо гонки сохраняется точная точка восстановления; незавершённая гонка не меняет сезон.\r\nНастройки → Управление: переназначение клавиатуры/мыши и геймпада, отмена и сброс. Настройки сохраняются отдельно от карьеры.\r\nНастройки → Сохранения: локальный обезличенный ZIP для диагностики. Ничего не отправляется автоматически.\r\nСогласованные механики, графика и движение M15.6 сохранены. M17 не начат.\r\nM16 ожидает вашего теста.\r\n");
            File.WriteAllText(Path.Combine(folder,"milestone.json"),"{\"schema_version\":1,\"id\":\"m16_reliability_review\",\"status\":\"owner_review\",\"unity\":\""+Application.unityVersion+"\"}");
        }
    }
}
#endif

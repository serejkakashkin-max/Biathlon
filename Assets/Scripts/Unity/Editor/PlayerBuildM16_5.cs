#if UNITY_EDITOR
using System.IO;
using UnityEngine;
namespace Biathlon.Unity
{
    public static partial class PlayerBuildPipeline
    {
        public static void BuildResultsDistanceReview()
        {
            const string folder="Builds/M16_5_Results_Distance_Review";
            BuildVisualUpgrade(folder);
            File.WriteAllText(Path.Combine(folder,"START_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon M16.5\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            File.WriteAllText(Path.Combine(folder,"milestone.json"),"{\"schema_version\":1,\"id\":\"m16_5_results_distance_review\",\"status\":\"owner_review\",\"unity\":\""+Application.unityVersion+"\"}");
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),"BIATHLON — M16.5\r\n\r\nSTART_TEST.cmd — полный экран. Существующая карьера поддерживается.\r\n\r\nРезультаты: компактная сводка, 10 спортсменов на странице, флаги, выделение вашей строки, переход между страницами и личными отсечками.\r\nДистанции в подготовке, на воротах и в TAB приведены к официальной дистанции дисциплины. Каждый круг даёт свою долю километража; мужской масс-старт — 3 / 6 / 9 / 12 / 15 км. На промежуточных воротах расстояние нарастает с каждым прохождением. Слово TIMING убрано. Согласованная физическая длина игрового заезда, время и баланс сохранены.\r\nВечером зрители, трибуны, ограждения и рекламные щиты получают свет от того же поля освещения, что снег и спортсмены. Материалы, текстуры, тени и расстановка сохранены; дополнительные источники света не добавлены.\r\n\r\nM16.4 закрыт по вашему апруву. M16.5 ожидает вашего теста.\r\n");
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
namespace Biathlon.Unity
{
    public static partial class PlayerBuildPipeline
    {
        public static void BuildResultsDistanceReview()=>BuildM165Review("Builds/M16_5_Results_Distance_Review");
        public static void BuildSnowGapsReview()=>BuildM165Review("Builds/M16_5_Snow_Gaps_Review");
        private static void BuildM165Review(string folder)
        {
            BuildVisualUpgrade(folder);
            File.WriteAllText(Path.Combine(folder,"START_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon M16.5\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            File.WriteAllText(Path.Combine(folder,"milestone.json"),"{\"schema_version\":1,\"id\":\""+Path.GetFileName(folder).ToLowerInvariant()+"\",\"status\":\"owner_review\",\"unity\":\""+Application.unityVersion+"\"}");
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),"BIATHLON — M16.5\r\n\r\nSTART_TEST.cmd — полный экран. Существующая карьера поддерживается.\r\n\r\nИсправлены резкие снежные стыки на возврате спринта: уточнены границы полотна и сглаживание кромок у соединений.\r\nОтставания от минуты отображаются как +1:10,2 вместо +70,2; формат согласован в HUD, TAB, трансляции и повторе.\r\n\r\nРезультаты: компактная сводка, 10 спортсменов на странице, флаги, выделение вашей строки, переход между страницами и личными отсечками.\r\nДистанции в подготовке, на воротах и в TAB приведены к официальной дистанции дисциплины. Каждый круг даёт свою долю километража; мужской масс-старт — 3 / 6 / 9 / 12 / 15 км. На промежуточных воротах расстояние нарастает с каждым прохождением. Слово TIMING убрано. Согласованная физическая длина игрового заезда, время и баланс сохранены.\r\nВечером зрители, трибуны, ограждения и рекламные щиты получают свет от того же поля освещения, что снег и спортсмены. Материалы, текстуры, тени и расстановка сохранены; дополнительные источники света не добавлены.\r\n\r\nM16.4 закрыт по вашему апруву. M16.5 ожидает вашего теста.\r\n");
        }
    }
}
#endif

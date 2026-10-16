#if UNITY_EDITOR
using System.IO;
using UnityEngine;
namespace Biathlon.Unity
{
    public static partial class PlayerBuildPipeline
    {
        public static void BuildWinterVenueReview()
        {
            const string folder="Builds/M16_1_Winter_Venue_Review";BuildVisualUpgrade(folder);
            File.WriteAllText(Path.Combine(folder,"START_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon M16.1\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),"BIATHLON — M16.1 / Зимний стадион\r\n\r\nSTART_TEST.cmd — игра в полный экран; можно продолжить карьеру.\r\n\r\nПроверьте снежные края на поворотах, спусках, развилках спринта и штрафном круге.\r\nЗа мишенями — национальные флаги, реагирующие на ветер.\r\nM16.1 ожидает вашего теста. M17 не начат.\r\n");
            File.WriteAllText(Path.Combine(folder,"milestone.json"),"{\"schema_version\":1,\"id\":\"m16_1_winter_venue_review\",\"status\":\"owner_review\",\"unity\":\""+Application.unityVersion+"\"}");
        }
    }
}
#endif

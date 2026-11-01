#if UNITY_EDITOR
using System.IO;
using UnityEngine;
namespace Biathlon.Unity
{
    public static partial class PlayerBuildPipeline
    {
        public static void BuildM166AppearanceReview()=>BuildM166Review("Builds/M16_6_1_Appearance_Review","M16.6.1","Вариант без очков полностью скрывает линзу, оправу и ремешок. Сохранены очки на лице/на лбу и шапка/повязка/без головного убора. Новые соперники получают индивидуальную постоянную внешность без изменения игрового RNG; старые спортсмены сохраняют свой облик.");
        public static void BuildM166EquipmentReview()=>BuildM166Review("Builds/M16_6_2_Equipment_Review","M16.6.2","Магазин: 8 моделей лыж, 4 винтовки, независимые слоты, сравнение по снегу. Новая экономика и честные покупки ИИ. Старые карьеры получают только нейтральную винтовку; деньги, лыжи и внешность сохраняются.");
        private static void BuildM166Review(string folder,string stage,string details)
        {
            BuildVisualUpgrade(folder);
            File.WriteAllText(Path.Combine(folder,"START_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon "+stage+"\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            File.WriteAllText(Path.Combine(folder,"milestone.json"),"{\"schema_version\":1,\"id\":\""+Path.GetFileName(folder).ToLowerInvariant()+"\",\"stage\":\""+stage+"\",\"status\":\"owner_review\",\"unity\":\""+Application.unityVersion+"\"}");
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),"BIATHLON — "+stage+"\r\n\r\nSTART_TEST.cmd — полный экран. Существующие карьеры поддерживаются.\r\n\r\n"+details+"\r\n\r\nМ16.5 закрыт владельцем. М16.6 находится в разработке и ожидает вашего тестирования.\r\n");
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
namespace Biathlon.Unity
{
    public static partial class PlayerBuildPipeline
    {
        public static void BuildM166MenuUxReview()=>BuildM166Review("Builds/M16_6_7_Menu_UX_Review","M16.6.7","Постоянная навигация по разделам карьеры, выделение текущего экрана, блок следующего действия с планом недели, приоритет продолжения сохранённой карьеры и видимая прокрутка. Баланс, экономика и длительность гонок сохранены.");
        public static void BuildM166MenuArtReview()=>BuildM166Review("Builds/M16_6_6_Menu_Art_Review","M16.6.6","Новое оформление тренировок и магазина: 18 оригинальных изображений, 6 тренировок, 8 пар лыж и 4 винтовки. Крупные карточки, выделение выбранного предмета и фокуса, адаптация для крупного текста и узкого экрана. Игровые характеристики, экономика и формулы тренировок сохранены.");
        public static void BuildM166AppearanceReview()=>BuildM166Review("Builds/M16_6_1_Appearance_Review","M16.6.1","Вариант без очков полностью скрывает линзу, оправу и ремешок. Сохранены очки на лице/на лбу и шапка/повязка/без головного убора. Новые соперники получают индивидуальную постоянную внешность без изменения игрового RNG; старые спортсмены сохраняют свой облик.");
        public static void BuildM166EquipmentReview()=>BuildM166Review("Builds/M16_6_2_Equipment_Review","M16.6.2","Магазин: 8 моделей лыж, 4 винтовки, независимые слоты, сравнение по снегу. Новая экономика и честные покупки ИИ. Старые карьеры получают только нейтральную винтовку; деньги, лыжи и внешность сохраняются.");
        public static void BuildM166WeatherReview()=>BuildM166Review("Builds/M16_6_3_Weather_Review","M16.6.3","Общая погода по времени гонки: прогноз на ваш старт, плавные фронты и инерция снега, выбор купленных лыж перед стартом. HUD и TAB показывают текущее состояние. Погода и экипировка записываются в повтор; старые повторы сохраняют прежние условия.");
        public static void BuildM166TrainingReview()=>BuildM166Review("Builds/M16_6_4_Training_Review","M16.6.4","Шесть иллюстрированных тренировок, полный план недели, слоты, навыки, форма, усталость, здоровье и риск по существующему прогнозу. Формулы тренировок сохранены. Включены магазин, экипировка и динамическая погода предыдущих этапов; повторы учитывают версии оборудования, погоды и стартового стыка.");
        private const string M166RegressionDirectory="Builds/M16_6_5_Regression_Review";
        private const string M166RegressionDetails="Включены все этапы М16.6: внешность, магазин, экипировка, динамическая погода и тренировки. Исправлены взаимная блокировка у стартового стыка и выбор достижимого рулевого ввода ИИ. Размеры тел, физика, статистики и формулы тренировок сохранены; прежние повторы используют записанные версии политик.";
        public static void BuildM166RegressionReview()=>BuildM166Review(M166RegressionDirectory,"M16.6.5",M166RegressionDetails);
        public static void CompleteM166RegressionReview()=>CompleteM166Review(M166RegressionDirectory,"M16.6.5",M166RegressionDetails);
        private static void BuildM166Review(string folder,string stage,string details)
        {
            BuildVisualUpgrade(folder);
            CompleteM166Review(folder,stage,details);
        }
        private static void CompleteM166Review(string folder,string stage,string details)
        {
            ValidateProductionBuildDirectory(folder);
            var receipt=JsonUtility.FromJson<VisualBuildReceipt>(File.ReadAllText(Path.Combine(folder,"build-summary.json")));
            if(receipt.result!="Succeeded"||receipt.errors!=0||!File.Exists(Path.Combine(folder,"Biathlon.exe")))throw new System.InvalidOperationException("Only a successful existing player can be packaged for review.");
            File.Copy("Assets/Resources/Biathlon/G2ThirdPartyCredits.txt",Path.Combine(folder,"THIRD_PARTY_CREDITS.txt"),true);
            foreach(string notice in Directory.GetFiles("Assets/Resources/Biathlon/UI/Fonts","*OFL*.txt"))File.Copy(notice,Path.Combine(folder,Path.GetFileName(notice)),true);
            File.WriteAllText(Path.Combine(folder,"START_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon "+stage+"\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            File.WriteAllText(Path.Combine(folder,"milestone.json"),"{\"schema_version\":1,\"id\":\""+Path.GetFileName(folder).ToLowerInvariant()+"\",\"stage\":\""+stage+"\",\"status\":\"owner_review\",\"unity\":\""+Application.unityVersion+"\"}");
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),"BIATHLON — "+stage+"\r\n\r\nSTART_TEST.cmd — полный экран. Существующие карьеры поддерживаются.\r\n\r\n"+details+"\r\n\r\nМ16.5 закрыт владельцем. М16.6 находится в разработке и ожидает вашего тестирования.\r\n");
        }
    }
}
#endif

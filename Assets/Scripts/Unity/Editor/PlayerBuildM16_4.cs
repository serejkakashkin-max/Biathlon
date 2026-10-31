#if UNITY_EDITOR
using System.IO;
using UnityEngine;
namespace Biathlon.Unity
{
    public static partial class PlayerBuildPipeline
    {
        public static void BuildShootingContactReview()
            =>BuildShootingContactReview("Builds/M16_4_Shooting_Contact_Review");
        public static void BuildRaceLoadingReview()
            =>BuildShootingContactReview("Builds/M16_4_Loading_Review");
        public static void BuildCoachesReview()
            =>BuildShootingContactReview("Builds/M16_4_Coaches_Review");
        public static void BuildMiniMapReview()
            =>BuildShootingContactReview("Builds/M16_4_MiniMap_Review");
        public static void BuildVisibilityReview()
            =>BuildShootingContactReview("Builds/M16_4_Visibility_Review");
        private static void BuildShootingContactReview(string folder)
        {
            BuildVisualUpgrade(folder);
            File.WriteAllText(Path.Combine(folder,"START_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon M16.4\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            File.WriteAllText(Path.Combine(folder,"milestone.json"),"{\"schema_version\":1,\"id\":\"m16_4_shooting_contact_review\",\"status\":\"owner_review\",\"unity\":\""+Application.unityVersion+"\"}");
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),"BIATHLON — M16.4 / Изготовка и следы выстрелов\r\n\r\nSTART_TEST.cmd — полный экран; существующая карьера поддерживается.\r\nЛёжа палки укладываются справа от спортсмена, стоя — между лыжами. Подъём и изготовка идут по прежнему игровому времени. Лыжи остаются на снегу / коврике.\r\nСледы попаданий на передней панели мишени сохраняются в пределах гонки. Попадание через отверстие не оставляет пробоину на закрывающейся белой заслонке. Число следов ограничено.\r\nВ стрелковом HUD есть увеличенная схема последнего выстрела по фактической точке пересечения: попадание, габарит, промах у кромки. Точный край засчитывается; зона попадания не расширена, случайного решения нет.\r\nСхема не показывает текущую точку прицеливания. Калибр 5.6 мм — размер визуального следа; полный механизм импульсного срабатывания металлической мишени не симулируется.\r\nПроверьте обе изготовки, укладку и подбор палок, край мишени и сохранение следов. M16.3 закрыт по вашему апруву; M16.4 ждёт теста.\r\n");
            File.AppendAllText(Path.Combine(folder,"READ_ME.txt"),"\r\nПосле нажатия «Начать гонку» сразу открывается экран загрузки Frostline с превью трассы и текущим этапом подготовки. Он закрывается после готовности мира, перед штатным ожиданием и личным отсчётом 3–2–1. Настройки русского/английского языка, крупного текста и уменьшения анимации поддерживаются.\r\n");
            File.AppendAllText(Path.Combine(folder,"READ_ME.txt"),"\r\nЗа выездной дорожкой стрельбища добавлена приподнятая огороженная площадка тренеров: 12 разных лиц, возраст 44–69 лет, седина, национальные зимние комплекты, 12 труб на штативах. Это статичные версии нашей модели спортсмена с тремя уровнями детализации. Новая зона не вмешивается в гонку и стрельбу.\r\n");
            File.AppendAllText(Path.Combine(folder,"READ_ME.txt"),"\r\nСлева под таймером — мини-карта фактического маршрута выбранного формата. Голубая стрелка — вы и ваше направление; светлые точки — соперники; золотом отмечены штрафной круг/стрельбище и стреляющие соперники, клетчатым значком — финиш. Во время стрельбы и просмотра TAB/F2 карта скрывается. Это векторный интерфейс без второй камеры мира; действуют прежние управление и баланс.\r\n");
            File.AppendAllText(Path.Combine(folder,"READ_ME.txt"),"\r\nЗрители, лес и зимний декор больше не исчезают на видимых дистанциях 110–120 м. На всех уровнях качества сохраняется заполнение трибун и лесной силуэт; дальние модели, инстансинг и отсечение за камерой/полным туманом ограничивают нагрузку. Качество ближних текстур и геометрии не снижено.\r\n");
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
namespace Biathlon.Unity
{
    public static partial class PlayerBuildPipeline
    {
        public static void BuildLiveShootingReview()
        {
            const string folder="Builds/M16_2_Live_Shooting_Review";BuildVisualUpgrade(folder);
            File.WriteAllText(Path.Combine(folder,"START_TEST.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon M16.2\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            File.WriteAllText(Path.Combine(folder,"READ_ME.txt"),"BIATHLON — M16.2 / Прямая трансляция стрельбы\r\n\r\nSTART_TEST.cmd — полный экран; существующая карьера поддерживается.\r\nВ спринте стартовый номер определяется жеребьёвкой всех участников, включая игрока. Первый номер тоже возможен. Перезагрузка той же гонки не меняет жеребьёвку. Ожидание своего старта можно пропустить; затем личный отсчёт 3–2–1.\r\nНовая очередь стрелков справа: реальные выстрелы, удержание итога 3 секунды, плавное освобождение места. Игрок не закреплён первым.\r\nРеакция зрителей на каждый выстрел игрока: радость при попадании, разочарование при промахе. Для ботов эти реакции не звучат. Громкость — Окружение / общая громкость.\r\nПроверьте спринт с жеребьёвкой, масс-старт, русский/английский интерфейс, Tab, читаемость табло и звук на рубеже.\r\nM16.2 ожидает вашего теста. M17 не начат.\r\n");
            File.AppendAllText(Path.Combine(folder,"READ_ME.txt"),"\r\nКурсор скрыт во время игры, включая TAB. В паузе и меню он возвращается; при потере фокуса освобождается.\r\nTAB — отсечки: PgUp / PgDn — предыдущая / следующая; End — вернуться к последней. На геймпаде — правый стик влево / вправо, R3 — последняя. Ранее выбранная отсечка сохраняется при прохождении новой. Таблица показывает круг и километраж; гонка продолжает идти.\r\n");
            File.AppendAllText(Path.Combine(folder,"READ_ME.txt"),"\r\nСнег на развилках спринта и преследования: сглажены открытые торцы и переходы между откосами. Проверьте въезды и выезды с веток трассы.\r\nЗа победу в Кубке мира сохраняются 90 очков — это действующая шкала IBU; 60 очков применялись до сезона 2022/23.\r\n");
            File.WriteAllText(Path.Combine(folder,"milestone.json"),"{\"schema_version\":1,\"id\":\"m16_2_live_shooting_review\",\"status\":\"owner_review\",\"unity\":\""+Application.unityVersion+"\"}");
            File.AppendAllText(Path.Combine(folder,"READ_ME.txt"),"\r\nВозле штрафного круга убрана лишняя петля обхода стрельбища с пересекающимися поверхностями. Действующие стартовый выезд, стрелковые дорожки и штрафной круг сохранены.\r\n");
        }
    }
}
#endif

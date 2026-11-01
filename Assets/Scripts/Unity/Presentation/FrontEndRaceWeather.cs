using System;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine.UIElements;
namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private System.Globalization.CultureInfo WeatherCulture=>System.Globalization.CultureInfo.GetCultureInfo(Locale=="ru"?"ru-RU":"en-US");
        private void RenderRaceWeatherPlan(VisualElement parent)
        {
            var week=Session.PreparedWeek;var snapshot=CareerSave.FromJson(week.RaceSnapshotJson);var provider=new CareerRaceProvider(ReadCore,Session.Definitions,ProductionVenueHost.ReadResource);
            var timeline=provider.ForecastTimeline(snapshot,week.Event,week.RaceSeed);if(timeline==null)return;
            double start=provider.PlayerStartSeconds(snapshot,week.Event,week.RaceSeed);var current=timeline.Sample(start);var later=timeline.Sample(start+900);
            var panel=Tile(parent);panel.name="pre_race_weather_plan";
            AddLabel(panel,L("ПРОГНОЗ НА ВАШ СТАРТ · ","FORECAST AT YOUR START · ")+TimeSpan.FromSeconds(start).ToString(@"mm\:ss"),theme.body_size,true);
            AddLabel(panel,WeatherDescription(current),theme.caption_size).name="personal_start_weather";
            AddLabel(panel,(timeline.FrontCount==0?L("Существенных изменений не ожидается","No significant changes expected"):L("Через 15 минут: ","After 15 minutes: ")+WeatherDescription(later)),theme.caption_size).style.color=FrostlineStyle.Gold;
            AddLabel(panel,L("Выберите купленную пару под условия","Choose an owned pair for these conditions"),theme.caption_size);
            var gear=Session.Gear;var snow=SnowCatalog.FromJson(ReadCore("snow_catalog_provisional_v1.json"));var player=snapshot.FindWorldAthlete(snapshot.PlayerId);
            for(int i=0;i<gear.Skis.Count;i++)
            {
                var ski=gear.Skis.Get(i);if(!CareerInventory.Owns(player,ski.Id))continue;string id=ski.Id;var system=new SkiEquipmentSystem(ski,snow);var a=system.Evaluate(current);var b=system.Evaluate(later);bool chosen=player.SelectedSki==id;
                string performance=L("скольжение ","glide ")+(1/a.SnowResistanceMultiplier).ToString("0.00",WeatherCulture)+" → "+(1/b.SnowResistanceMultiplier).ToString("0.00",WeatherCulture)+L(" · контроль "," · control ")+a.CorneringGripMultiplier.ToString("0.00",WeatherCulture);
                AddButton(panel,"pre_race_ski_"+id,EquipmentName(id)+(chosen?" · "+T("equipment.selected"):"")+"\n"+performance,()=>{Session.SelectPreRaceSkis(id);Render();},chosen,!chosen&&!Race.Loading);
                var button=panel.Q<Button>("pre_race_ski_"+id);button.style.height=StyleKeyword.Auto;button.style.minHeight=64*Scale;button.style.whiteSpace=WhiteSpace.Normal;button.style.paddingTop=button.style.paddingBottom=8;
                if(chosen){button.style.opacity=1;button.style.color=FrostlineStyle.Deep;button.style.backgroundColor=FrostlineStyle.Accent;}
            }
        }
        private string WeatherDescription(RaceWeatherFrame frame)
        {return L("Воздух ","Air ")+frame.AirTemperatureC.ToString("0.0",WeatherCulture)+"°C · "+L("снег ","snow ")+frame.SnowTemperatureC.ToString("0.0",WeatherCulture)+"°C · "+T("snow."+frame.SnowProfile)+" · "+L("ветер ","wind ")+Math.Sqrt(frame.Wind.EastMetersPerSecond*frame.Wind.EastMetersPerSecond+frame.Wind.NorthMetersPerSecond*frame.Wind.NorthMetersPerSecond).ToString("0.0",WeatherCulture)+L(" м/с"," m/s")+" · "+(frame.Snowfall>0?L("снегопад ","snowfall ")+(frame.Snowfall*100).ToString("0")+"%":L("без осадков","dry"));}
    }
}

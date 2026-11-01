using System;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private bool rifleCategory;
        private void RenderEquipment(VisualElement actions,VisualElement card)
        {
            AddLabel(actions,F("equipment.money",Session.Player.Money/100.0),theme.section_size,true).name="equipment_money";
            AddButton(actions,"equipment_skis",L("Лыжи · 8 моделей","Skis · 8 models"),()=>{rifleCategory=false;equipmentPage=0;Render();},!rifleCategory);
            AddButton(actions,"equipment_rifles",L("Винтовки · 4 модели","Rifles · 4 models"),()=>{rifleCategory=true;equipmentPage=0;Render();},rifleCategory);
            var next=Session.NextEvent();var forecast=next==null?null:Session.Definitions.Forecast(Session.Career,next);
            string snowId=forecast?.SnowProfile??"packed_neutral";
            string SnowName(string id)=>L(id=="fresh_soft"?"свежий снег":id=="wet_slow"?"мокрый снег":id=="icy_fast"?"лёд":"плотный снег",id=="fresh_soft"?"fresh snow":id=="wet_slow"?"wet snow":id=="icy_fast"?"ice":"packed snow");
            AddLabel(card,rifleCategory?L("СТРЕЛЬБИЩЕ · ВЫБОР ВИНТОВКИ","RANGE · RIFLE SELECTION"):L("ЛЫЖНЫЙ СЕРВИС · ","SKI SERVICE · ")+SnowName(snowId).ToUpperInvariant(),theme.section_size,true);
            AddLabel(card,rifleCategory?L("Меньше отдача, быстрее перезарядка или удобнее изготовка — выбирайте компромисс.","Trade recoil, bolt speed and ergonomics to suit your shooting."):L("Сравнение с установленной парой по прогнозу следующего старта.","Compared with your equipped pair for the next start’s forecast."),theme.caption_size).style.color=FrostlineStyle.Muted;
            var gear=Session.Gear;var economy=Session.Definitions.Economy;var snow=SnowCatalog.FromJson(ReadCore("snow_catalog_provisional_v1.json"));
            var tiles=TileGrid(card,"equipment_tiles");int count=rifleCategory?gear.RifleCount:gear.Skis.Count;
            for(int i=0;i<count;i++)
            {
                string id=rifleCategory?gear.Rifle(i).Id:gear.Skis.Get(i).Id;
                bool owned=CareerInventory.Owns(Session.Player,id),selected=(rifleCategory?Session.Player.SelectedRifle:Session.Player.SelectedSki)==id;
                long price=owned?0:economy.EquipmentPrice(id);var row=Tile(tiles);row.name="equipment_card_"+id;row.style.width=Length.Percent(compactLayout?48.5f:32);
                AddLabel(row,EquipmentName(id),theme.body_size,true);
                AddLabel(row,selected?T("equipment.selected"):owned?L("КУПЛЕНО","OWNED"):F("equipment.buy",price/100.0),theme.caption_size).style.color=selected?FrostlineStyle.Accent:FrostlineStyle.Gold;
                if(rifleCategory)
                {
                    row.Add(new RifleProductGraphic(id));var r=gear.Rifle(id);var current=gear.Rifle(Session.Player.SelectedRifle);
                    DeltaMeter(row,L("Стабильность после выстрела","Recoil stability"),current.RecoilMultiplier/r.RecoilMultiplier-1);
                    DeltaMeter(row,L("Перезарядка и успокоение","Bolt and settling speed"),current.HandlingMultiplier/r.HandlingMultiplier-1);
                    DeltaMeter(row,L("Изготовка и выход","Setup and exit"),current.ErgonomicsMultiplier/r.ErgonomicsMultiplier-1);
                    AddLabel(row,L("Геометрия попадания одинакова для всех винтовок.","All rifles use the same hit geometry."),theme.caption_size).style.color=FrostlineStyle.Muted;
                }
                else
                {
                    var appearance=CareerSkiAppearance.Catalog.Get(id);row.Add(new SkiPairGraphic(FrontEndTheme.Color(appearance.BaseColorHex),FrontEndTheme.Color(appearance.AccentColorHex),id));
                    var model=gear.Skis.GetSki(id);var current=new SkiEquipmentSystem(gear.Skis.GetSki(Session.Player.SelectedSki),snow).Evaluate(snowId);var output=new SkiEquipmentSystem(model,snow).Evaluate(snowId);
                    DeltaMeter(row,L("Скольжение по прогнозу","Glide in forecast snow"),current.SnowResistanceMultiplier/output.SnowResistanceMultiplier-1);
                    DeltaMeter(row,L("Контроль поворота","Corner control"),output.CorneringGripMultiplier/current.CorneringGripMultiplier-1);
                    foreach(string condition in new[]{"packed_neutral","fresh_soft","icy_fast","wet_slow"})
                        AddLabel(row,SnowName(condition)+"  "+((model.GlideMultiplier*model.AffinityFor(condition)-1)*100).ToString("+0.0;-0.0;0.0")+"%",theme.caption_size).style.color=condition==snowId?FrostlineStyle.Gold:FrostlineStyle.Muted;
                }
                AddButton(row,"equip_"+id,selected?T("equipment.selected"):owned?T("equipment.select"):F("equipment.buy",price/100.0),()=>CareerAction(()=>Session.Equip(id)),true,!selected&&Session.Player.Money>=price);
                if(selected||Session.Player.Money<price){var button=row.Q<Button>("equip_"+id);button.style.opacity=1;button.style.color=selected?FrostlineStyle.Deep:FrostlineStyle.Muted;button.style.backgroundColor=selected?FrostlineStyle.Accent:FrostlineStyle.Surface;}
                if(!owned&&Session.Player.Money<price)AddLabel(row,L("Нужно ещё ","Save another ")+((price-Session.Player.Money)/100.0).ToString("0.00"),theme.caption_size);
                if(selected){row.style.borderTopWidth=3;row.style.borderTopColor=FrostlineStyle.Accent;}
            }
            PageRows(card,tiles,equipmentPage,compactLayout?2:3,value=>equipmentPage=value);
        }
        private string EquipmentName(string id)
        {
            switch(id)
            {
                case "ski_cold_race":return L("NORD · холодный снег","NORD · cold race");
                case "ski_fresh_float":return L("FLOAT · свежий снег","FLOAT · fresh snow");
                case "ski_wet_channel":return L("CHANNEL · мокрый снег","CHANNEL · wet snow");
                case "ski_ice_edge":return L("EDGE · ледяная трасса","EDGE · ice control");
                case "ski_soft_control":return L("CONTROL · мягкий снег","CONTROL · soft snow");
                case "rifle_neutral_v1":return L("Базовая винтовка","Standard rifle");
                case "rifle_precision":return L("PRECISION · стабильность","PRECISION · stability");
                case "rifle_agile":return L("AGILE · скорость работы","AGILE · fast handling");
                case "rifle_endurance":return L("ENDURANCE · эргономика","ENDURANCE · ergonomics");
                default:return T("equipment."+id);
            }
        }
    }
    internal sealed class RifleProductGraphic:VisualElement
    {
        internal RifleProductGraphic(string id)
        {
            name="rifle_art_"+id;style.height=100;style.flexShrink=0;pickingMode=PickingMode.Ignore;
            generateVisualContent+=context=>
            {
                var p=context.painter2D;float w=contentRect.width;var accent=id=="rifle_precision"?FrostlineStyle.Gold:id=="rifle_agile"?new Color(.8f,.3f,.26f):id=="rifle_endurance"?new Color(.48f,.65f,.85f):FrostlineStyle.Accent;
                p.strokeColor=new Color(.7f,.79f,.82f);p.lineWidth=5;p.BeginPath();p.MoveTo(new Vector2(w*.4f,38));p.LineTo(new Vector2(w*.94f,38));p.Stroke();
                p.fillColor=accent;p.BeginPath();p.MoveTo(new Vector2(12,35));p.LineTo(new Vector2(w*.32f,35));p.LineTo(new Vector2(w*.4f,46));p.LineTo(new Vector2(w*.56f,46));p.LineTo(new Vector2(w*.56f,58));p.LineTo(new Vector2(w*.32f,58));p.LineTo(new Vector2(w*.28f,75));p.LineTo(new Vector2(w*.21f,75));p.LineTo(new Vector2(w*.23f,56));p.LineTo(new Vector2(12,67));p.ClosePath();p.Fill();
                p.strokeColor=FrostlineStyle.Ink;p.lineWidth=4;p.BeginPath();p.MoveTo(new Vector2(w*.4f,34));p.LineTo(new Vector2(w*.4f,22));p.LineTo(new Vector2(w*.46f,22));p.Stroke();p.BeginPath();p.MoveTo(new Vector2(w*.86f,37));p.LineTo(new Vector2(w*.86f,27));p.Stroke();
            };
        }
    }
}

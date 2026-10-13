using UnityEngine;
using UnityEngine.UIElements;
namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private void AddAudioSettings(VisualElement parent)
        {
            var audio=BiathlonAudio.Instance;if(audio==null)return;
            var card=Tile(parent);card.name="audio_settings";
            AddLabel(card,L("ЗВУК • FROSTLINE","AUDIO • FROSTLINE"),theme.section_size,true);
            AddLabel(card,L("Лес вокруг. Дыхание рядом. Каждый выстрел слышен.","Forest around you. Breath close by. Every shot distinct."),theme.caption_size);
            var p=audio.Preferences;
            AddAudioSlider(card,"master",L("Общая громкость","Master"),p.master,v=>p.master=v);
            AddAudioSlider(card,"effects",L("Лыжи и винтовка","Skis & rifle"),p.effects,v=>p.effects=v);
            AddAudioSlider(card,"body",L("Дыхание и пульс","Breath & heartbeat"),p.body,v=>p.body=v);
            AddAudioSlider(card,"ambience",L("Погода и трибуны","Weather & crowd"),p.ambience,v=>p.ambience=v);
            AddAudioSlider(card,"ui",L("Интерфейс","Interface"),p.ui,v=>p.ui=v);
            AddAudioSlider(card,"music",L("Атмосфера меню","Menu atmosphere"),p.music,v=>p.music=v);
            var quiet=new Toggle(L("Мягкий микс — тише резкие звуки","Soft mix — gentler loud effects")){name="audio_quiet",value=p.quiet};StyleControl(quiet);card.Add(quiet);controls.Add(quiet);
            quiet.RegisterValueChangedCallback(e=>{p.quiet=e.newValue;audio.SetPreferences(p,PersistPreferences);});
            var background=new Toggle(L("Без звука в фоне","Mute when unfocused")){name="audio_background",value=p.mute_unfocused};StyleControl(background);card.Add(background);controls.Add(background);
            background.RegisterValueChangedCallback(e=>{p.mute_unfocused=e.newValue;audio.SetPreferences(p,PersistPreferences);});
            AddButton(card,"audio_preview",L("Прослушать выстрел","Preview rifle"),()=>audio.Play(AudioCue.Rifle,local:true));
        }
        private void AddAudioSlider(VisualElement card,string id,string title,float value,System.Action<float> set)
        {
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.alignItems=Align.Center;row.style.marginTop=5;card.Add(row);
            var label=AddLabel(row,title,theme.caption_size);label.style.width=200*Scale;label.style.flexShrink=1;
            var slider=new Slider(0,100){name="audio_"+id,value=value*100};slider.style.flexGrow=1;slider.style.minWidth=90;slider.style.height=32;
            slider.tooltip=title;row.Add(slider);controls.Add(slider);
            var amount=AddLabel(row,Mathf.RoundToInt(value*100)+"%",theme.caption_size);amount.style.width=55*Scale;amount.style.unityTextAlign=TextAnchor.MiddleRight;
            slider.RegisterValueChangedCallback(e=>{set(e.newValue/100);amount.text=Mathf.RoundToInt(e.newValue)+"%";BiathlonAudio.Instance.SetPreferences(BiathlonAudio.Instance.Preferences,PersistPreferences);});
        }
    }
}

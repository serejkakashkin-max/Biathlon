using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class GameplayHud
    {
        private VisualElement burstPanel,burstFill,normalPacePanel;
        private Label burstTime,burstHint;
        private string burstReleaseHint,burstRecoverHint;
        private void BuildShortBurst(VisualElement readout,VisualElement paceLayer)
        {
            normalPacePanel=paceLayer;
            var inputPreferences=host?.InputPreferences??PlayerSettingsPreferences.Current;
            burstReleaseHint=L("Отпустить ","Release ")+PlayerControlHints.Label(inputPreferences,"attack")+L(" → темп"," → pace");
            burstRecoverHint=PlayerControlHints.Label(inputPreferences,"recovery")+L(" — восстановление"," — recover");
            burstPanel=new VisualElement{name="burst_overlay"};
            burstPanel.style.position=Position.Absolute;burstPanel.style.left=burstPanel.style.right=0;
            burstPanel.style.top=burstPanel.style.bottom=0;
            var warm=FrostlineStyle.Gold;
            Text(burstPanel,"burst_title",L("РЫВОК","BURST"),settings.caption_size,true).style.color=warm;
            burstTime=Text(burstPanel,"burst_time","",settings.body_size,true);
            burstTime.style.color=warm;
            burstFill=Bar(burstPanel,"burst_fill",warm);burstFill.parent.style.height=burstFill.style.height=5;
            burstHint=Text(burstPanel,"burst_hint","",settings.caption_size);burstHint.style.color=muted;
            readout.Add(burstPanel);burstPanel.style.display=DisplayStyle.None;
        }
        private void RefreshShortBurst(AthletePresentationState athlete)
        {
            bool requested=host!=null&&host.BurstRequested&&!athlete.ShootingActive&&athlete.Phase<CompetitionPhase.Finished;
            burstPanel.style.display=requested?DisplayStyle.Flex:DisplayStyle.None;
            normalPacePanel.style.display=requested?DisplayStyle.None:DisplayStyle.Flex;
            if(!requested)return;
            var actor=host.Runner.Field.GetAthlete(host.PlayerIndex);
            // Read accepted physical work; the individual definition includes
            // career capacity. This overload does no allocation or simulation step.
            var burst=ShortBurstTelemetry.Evaluate(actor.PhysiologyDefinition,athlete.Physiology,
                athlete.Effort,true);
            burstFill.style.width=Length.Percent((float)(100*burst.ReserveFraction));
            burstTime.text=burst.FullPowerSecondsRemaining>0?
                "≈ "+burst.FullPowerSecondsRemaining.ToString("0")+L(" с"," s"):L("НА ИСХОДЕ","FADING");
            burstHint.text=burst.FullPowerSecondsRemaining>0?
                burstReleaseHint:burstRecoverHint;
        }
    }
}

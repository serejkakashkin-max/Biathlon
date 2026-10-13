using UnityEngine;
using UnityEngine.UIElements;
namespace Biathlon.Unity.Presentation
{
    public sealed partial class GameplayHud
    {
        private VisualElement paceFill,countdownPanel,startWaitPanel;
        private Label paceLabel,countdownLabel,countdownCaption,startWaitTime,startWaitCaption;
        private Button startWaitSkip;
        private HeartbeatGraphic heartbeat;
        private PulseTraceGraphic pulseTrace;
        private int previousCountdown;
        private double goUntil;
        private int paceCeiling;
        private int paintedCountdown=-1,paintedPace=-1;
        private bool paintedCountdownVisible;
        private int paintedWaitSeconds=-1;
        private bool paintedWaitVisible,paintedFastForward;
        private void BuildPaceStart()
        {
            paintedCountdown=paintedPace=-1;paintedCountdownVisible=false;
            previousCountdown=0;goUntil=0;paintedWaitSeconds=-1;paintedWaitVisible=paintedFastForward=false;
            paceCeiling=Biathlon.Domain.DeviceControlDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/device_control_gameplay_v3")).KeyboardEffortCeiling;
            var readout=new VisualElement{name="pace_readout"};readout.style.height=76*Scale;motionPanel.Add(readout);
            var paceLayer=new VisualElement{name="pace_layer"};readout.Add(paceLayer);
            paceLabel=Text(paceLayer,"pace_label","",settings.caption_size);
            paceFill=Bar(paceLayer,"pace_fill",accent);
            Text(paceLayer,"pace_keys",L("W +  ·  S − / тормоз\nShift — рывок","W +  ·  S − / brake\nShift — burst"),settings.caption_size).style.color=muted;
            BuildShortBurst(readout,paceLayer);
            countdownPanel=new VisualElement{name="start_countdown"};countdownPanel.style.position=Position.Absolute;
            countdownPanel.style.left=Length.Percent(42);countdownPanel.style.top=Length.Percent(24);countdownPanel.style.width=Length.Percent(16);
            countdownPanel.style.alignItems=Align.Center;countdownPanel.style.backgroundColor=FrontEndTheme.Color(settings.background);
            countdownPanel.style.borderTopLeftRadius=countdownPanel.style.borderTopRightRadius=countdownPanel.style.borderBottomLeftRadius=countdownPanel.style.borderBottomRightRadius=20;
            countdownCaption=Text(countdownPanel,"countdown_caption",L("ВАШ СТАРТ","YOUR START"),settings.caption_size,true);
            countdownCaption.style.marginTop=18;
            countdownLabel=Text(countdownPanel,"countdown_number","3",80,true);countdownLabel.style.color=accent;
            countdownLabel.style.unityFontStyleAndWeight=FontStyle.BoldAndItalic;
            Root.Add(countdownPanel);countdownPanel.style.display=DisplayStyle.None;
            startWaitPanel=new VisualElement{name="player_start_wait"};startWaitPanel.style.position=Position.Absolute;
            startWaitPanel.style.left=Length.Percent(30);startWaitPanel.style.width=Length.Percent(40);startWaitPanel.style.bottom=Length.Percent(16);
            startWaitPanel.style.alignItems=Align.Center;startWaitPanel.style.paddingTop=startWaitPanel.style.paddingBottom=16;
            startWaitPanel.style.backgroundColor=FrontEndTheme.Color(settings.background);
            startWaitPanel.style.borderTopLeftRadius=startWaitPanel.style.borderTopRightRadius=startWaitPanel.style.borderBottomLeftRadius=startWaitPanel.style.borderBottomRightRadius=20;
            startWaitCaption=Text(startWaitPanel,"player_start_wait_caption",L("СОПЕРНИКИ НА ТРАССЕ · ВАШ СТАРТ ЧЕРЕЗ","RIVALS ARE RACING · YOUR START IN"),settings.caption_size,true);
            startWaitTime=Text(startWaitPanel,"player_start_wait_time","",36,true);startWaitTime.style.color=accent;startWaitTime.style.unityFontStyleAndWeight=FontStyle.BoldAndItalic;
            startWaitSkip=new Button(()=>{host?.RequestSkipStartWait();RefreshPaceStart();}){name="skip_start_wait",text=L("К СТАРТУ  ·  ENTER / A","SKIP TO START  ·  ENTER / A")};
            startWaitSkip.style.height=40*Scale;startWaitSkip.style.marginTop=8;startWaitSkip.style.paddingLeft=startWaitSkip.style.paddingRight=20;
            startWaitSkip.style.backgroundColor=accent;startWaitSkip.style.color=FrontEndTheme.Color(theme.ink);startWaitSkip.style.fontSize=settings.caption_size*Scale;
            startWaitSkip.style.unityFontStyleAndWeight=FontStyle.Bold;
            startWaitSkip.style.borderTopLeftRadius=startWaitSkip.style.borderTopRightRadius=startWaitSkip.style.borderBottomLeftRadius=startWaitSkip.style.borderBottomRightRadius=10;
            startWaitPanel.Add(startWaitSkip);Root.Add(startWaitPanel);startWaitPanel.style.display=DisplayStyle.None;
        }
        private void RefreshPaceStart()
        {
            if(host==null||countdownPanel==null)return;
            int n=host.CountdownNumber;
            if(previousCountdown>0&&n==0)goUntil=Time.unscaledTimeAsDouble+.6;
            previousCountdown=n;
            bool visible=n>0||Time.unscaledTimeAsDouble<goUntil;
            if(visible!=paintedCountdownVisible){countdownPanel.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;paintedCountdownVisible=visible;}
            if(visible&&paintedCountdown!=n){paintedCountdown=n;countdownLabel.text=n>0?n.ToString():L("СТАРТ","GO");countdownLabel.style.fontSize=n>0?80:40;countdownCaption.text=n>0?L("ВАШ СТАРТ","YOUR START"):L("УДАЧНОЙ ГОНКИ","RACE ON");}
            bool waitVisible=host.CanSkipStartWait||host.StartWaitFastForwarding;
            if(waitVisible!=paintedWaitVisible){startWaitPanel.style.display=waitVisible?DisplayStyle.Flex:DisplayStyle.None;paintedWaitVisible=waitVisible;}
            if(waitVisible)
            {
                int seconds=(int)System.Math.Ceiling(host.PlayerStartWaitSeconds);
                if(seconds!=paintedWaitSeconds){paintedWaitSeconds=seconds;startWaitTime.text=(seconds/60).ToString("00")+":"+(seconds%60).ToString("00");}
                bool fast=host.StartWaitFastForwarding;
                if(fast!=paintedFastForward)
                {
                    paintedFastForward=fast;startWaitSkip.SetEnabled(!fast);
                    startWaitCaption.text=fast?L("ПРОПУСК ОЖИДАНИЯ","SKIPPING THE WAIT"):L("СОПЕРНИКИ НА ТРАССЕ · ВАШ СТАРТ ЧЕРЕЗ","RIVALS ARE RACING · YOUR START IN");
                }
            }
            int pace=host.SelectedPace;
            if(pace!=paintedPace)
            {paintedPace=pace;float percent=Mathf.Min(100,100f*pace/paceCeiling);
                paceLabel.text=pace==0?L("СТОП","STOP"):L("ТЕМП  ","PACE  ")+Mathf.RoundToInt(percent)+"%";
                paceFill.style.width=Length.Percent(percent);}
            heartbeat?.Animate(Time.unscaledTimeAsDouble);
            pulseTrace?.Animate(Time.unscaledTimeAsDouble);
        }
    }
    // Decorative pulse trace, not a medical ECG; phase follows the observed game HR.
    // Repaints only its tiny vector at 20 Hz, without texture uploads or per-frame objects.
    internal sealed class PulseTraceGraphic:VisualElement
    {
        private Color color;private double bpm=60,nextPaint;private float phase;
        internal PulseTraceGraphic(Color tint)
        {color=tint;name="pulse_trace";style.width=54;style.height=24;style.flexShrink=1;pickingMode=PickingMode.Ignore;generateVisualContent+=Draw;}
        internal void Observe(double hr,Color tint){bpm=hr;color=tint;}
        internal void Animate(double seconds){if(seconds<nextPaint)return;nextPaint=seconds+.05;phase=(float)(seconds*bpm/60%1);MarkDirtyRepaint();}
        private void Draw(MeshGenerationContext ctx)
        {
            var p=ctx.painter2D;float w=contentRect.width,y=contentRect.height*.55f;p.strokeColor=new Color(color.r,color.g,color.b,.25f);p.lineWidth=1;
            p.BeginPath();p.MoveTo(new Vector2(0,y));p.LineTo(new Vector2(w,y));p.Stroke();
            p.strokeColor=color;p.lineWidth=1.6f;p.BeginPath();p.MoveTo(new Vector2(0,y));p.LineTo(new Vector2(w*.24f,y));p.LineTo(new Vector2(w*.33f,y-3));p.LineTo(new Vector2(w*.40f,y+3));p.LineTo(new Vector2(w*.48f,y-10));p.LineTo(new Vector2(w*.55f,y+7));p.LineTo(new Vector2(w*.64f,y));p.LineTo(new Vector2(w,y));p.Stroke();
            p.fillColor=color;p.BeginPath();p.Arc(new Vector2(w*phase,y),2,Angle.Degrees(0),Angle.Degrees(360));p.Fill();
        }
    }
    // One small retained vector. Beat frequency follows observed HR; no textures,
    // coroutines, new materials or scene objects are created during animation.
    internal sealed class HeartbeatGraphic:VisualElement
    {
        private Color color;private float beat;private double nextPaint;private double bpm=60;
        internal HeartbeatGraphic(Color tint)
        {color=tint;name="animated_heartbeat";style.width=30;style.height=26;style.marginRight=8;pickingMode=PickingMode.Ignore;generateVisualContent+=Draw;}
        internal void Observe(double heartRate,Color tint){bpm=heartRate;color=tint;}
        internal void Animate(double seconds)
        {
            if(seconds<nextPaint)return;nextPaint=seconds+1.0/30;
            double phase=seconds*bpm/60%1;beat=(float)(1+.12*System.Math.Exp(-phase*14));MarkDirtyRepaint();
        }
        private void Draw(MeshGenerationContext ctx)
        {
            Vector2 c=contentRect.center;float s=beat*.9f;var p=ctx.painter2D;p.fillColor=color;
            p.BeginPath();p.MoveTo(c+new Vector2(0,10)*s);
            p.QuadraticCurveTo(c+new Vector2(-19,-2)*s,c+new Vector2(-9,-8)*s);
            p.QuadraticCurveTo(c+new Vector2(-3,-12)*s,c+new Vector2(0,-5)*s);
            p.QuadraticCurveTo(c+new Vector2(3,-12)*s,c+new Vector2(9,-8)*s);
            p.QuadraticCurveTo(c+new Vector2(19,-2)*s,c+new Vector2(0,10)*s);p.ClosePath();p.Fill();
        }
    }
}

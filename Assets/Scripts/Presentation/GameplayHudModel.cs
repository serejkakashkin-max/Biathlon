using System;
using System.Globalization;
using Biathlon.Simulation;

namespace Biathlon.Presentation
{
    /// <summary>Read-only wording from an accepted snapshot; never estimates rank or shot outcomes.</summary>
    public readonly struct GameplayHudModel
    {
        public string Clock {get;}
        public string Lap {get;}
        public string Timing {get;}
        public string Phase {get;}
        public string Speed {get;}
        public string HeartRate {get;}
        public string Reserve {get;}
        public string Fatigue {get;}
        public string ShootingHistory {get;}
        public string Range {get;}
        public string Weapon {get;}
        public string Breath {get;}
        public string Wind {get;}
        public string Shots {get;}
        public bool Shooting {get;}
        public int ClosedMask {get;}
        public float ReservePercent {get;}
        public float FatiguePercent {get;}
        public float BreathDebtPercent {get;}

        public GameplayHudModel(AthletePresentationState a,UiTextCatalog text,string locale)
        {
            if(text==null)throw new ArgumentNullException(nameof(text));
            var culture=CultureInfo.GetCultureInfo(locale=="ru"?"ru-RU":"en-US");
            string T(string key)=>text.Text("hud."+key,locale);
            string F(string key,params object[] args)=>string.Format(culture,T(key),args);
            var h=a.Hud;bool prestart=a.Phase==CompetitionPhase.Prestart;
            Clock=prestart?F("countdown",Math.Ceiling(a.StartCountdownSeconds)):FormatClock(a.ElapsedSeconds);
            Lap=F("lap",h.Lap,h.LapCount);
            Timing=h.Rank==0?T("timing_pending"):F(h.Final?"final_rank":"split_rank",h.Rank,h.ComparedAthletes,
                h.GapSeconds.HasValue?FormatGap(h.GapSeconds.Value,culture):"—",h.CheckpointOrdinal+1);
            Phase=a.PendingPenaltyLoops>0?F("penalty",a.PendingPenaltyLoops):T("phase."+(int)a.Phase);
            Speed=F("speed",a.SpeedMetersPerSecond*3.6);HeartRate=F("heart_rate",a.Physiology.HeartRateBpm);
            ReservePercent=(float)Math.Clamp(a.Physiology.AnaerobicReserve*100,0,100);
            FatiguePercent=(float)Math.Clamp(a.Physiology.MuscularFatigue*100,0,100);
            BreathDebtPercent=(float)Math.Clamp(a.BreathDebt*100,0,100);
            Reserve=F("reserve",ReservePercent);Fatigue=F("fatigue",FatiguePercent);
            ShootingHistory=F("history",h.ShootingResults);
            Shooting=a.ShootingActive;ClosedMask=a.ClosedTargetsMask;
            Range=F("range",a.RangeLane,text.Text("stance."+(int)a.Stance,locale));
            Shots=F("shots",a.ShotsFired);
            Weapon=T(WeaponKey(a.ShootingPhase,a.ShotsFired,a.NeedsBoltCycle));
            Breath=T(BreathKey(a.BreathBlocked,a.BreathHolding,a.BreathSwayMultiplier));
            double east=a.ShootingWind.EastMetersPerSecond,north=a.ShootingWind.NorthMetersPerSecond;
            double speed=Math.Sqrt(east*east+north*north);
            // Components are labelled in the model's world axes, not guessed from camera orientation.
            Wind=speed==0?T("wind_calm"):F("wind",speed,east,north);
        }
        public static string FormatClock(double seconds)
        {
            var time=TimeSpan.FromSeconds(Math.Max(0,seconds));
            return time.ToString(seconds>=3600?@"hh\:mm\:ss\.f":@"mm\:ss\.f",CultureInfo.InvariantCulture);
        }
        public static string FormatGap(double seconds,CultureInfo culture)
        {
            if(!double.IsFinite(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(culture==null)throw new ArgumentNullException(nameof(culture));
            double rounded=Math.Round(seconds,1,MidpointRounding.AwayFromZero);
            if(rounded<60)return "+"+rounded.ToString("0.0",culture);
            double minutes=Math.Floor(rounded/60),remainder=rounded-minutes*60;
            if(minutes<60)return "+"+minutes.ToString("0",culture)+":"+remainder.ToString("00.0",culture);
            return "+"+Math.Floor(minutes/60).ToString("0",culture)+":"+(minutes%60).ToString("00",culture)+":"+remainder.ToString("00.0",culture);
        }
        public static string WeaponKey(ShootingPhase phase,int shots,bool needsBolt)
        {
            if(shots>=5||phase==ShootingPhase.Exiting||phase==ShootingPhase.Complete)return "weapon_done";
            if(phase==ShootingPhase.Setup||phase==ShootingPhase.AwaitingLane)return "weapon_setup";
            if(phase==ShootingPhase.BoltCycle)return "weapon_cycling";
            return needsBolt?"weapon_reload":"weapon_ready";
        }
        public static string BreathKey(bool blocked,bool holding,double swayMultiplier)
        {return blocked?"breath_release":holding?(swayMultiplier>1?"breath_strained":"breath_hold"):"breath_ready";}
    }
}

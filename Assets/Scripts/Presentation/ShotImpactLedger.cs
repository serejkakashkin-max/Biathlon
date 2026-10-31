using System;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.Presentation
{
    /// <summary>Accepted impact evidence only. Never awards a hit or changes an aperture.</summary>
    public readonly struct ShotImpactEvidence
    {
        public string AthleteId {get;}
        public long Tick {get;}
        public int Bout {get;} public int Lane {get;} public int Ordinal {get;} public int NearestTarget {get;}
        public ShootingStance Stance {get;} public ShotOutcome Outcome {get;}
        public TargetIntersection Impact {get;}
        public double EdgeClearanceMeters {get;}
        public bool NearEdge {get;}
        public bool ClosedTarget=>Outcome==ShotOutcome.TargetClosed;
        public bool Valid=>AthleteId!=null;
        internal ShotImpactEvidence(string athlete,long tick,int bout,int lane,int ordinal,ShootingStance stance,
            ShotOutcome outcome,TargetIntersection impact,TargetGeometryDefinition geometry,double bulletDiameter)
        {
            AthleteId=athlete;Tick=tick;Bout=bout;Lane=lane;Ordinal=ordinal;Stance=stance;Outcome=outcome;Impact=impact;
            int nearest=0;double dx=impact.X-geometry.CenterX(0);
            for(int i=1;i<5;i++)if(Math.Abs(impact.X-geometry.CenterX(i))<Math.Abs(dx)){nearest=i;dx=impact.X-geometry.CenterX(i);}
            NearestTarget=impact.ReachesPlane?nearest:-1;
            EdgeClearanceMeters=impact.ReachesPlane?geometry.Radius(stance)-Math.Sqrt(dx*dx+impact.Y*impact.Y):double.NaN;
            NearEdge=impact.ReachesPlane&&Math.Abs(EdgeClearanceMeters)<=bulletDiameter*.5;
        }
    }

    /// <summary>Fixed storage for five actual shots per athlete's current bout, including misses.
    /// Consumers must observe accepted ticks, not interpolate sight positions or reconstruct missing shots.</summary>
    public sealed class ShotImpactLedger
    {
        private readonly string[] ids;
        private readonly ShotImpactEvidence[] last,shots;
        private readonly TargetGeometryDefinition geometry;
        private readonly double bulletDiameter;
        private int used;
        public int AcceptedCount {get;private set;}
        public ShotImpactLedger(TargetGeometryDefinition geometry,double bulletDiameter,int capacity)
        {
            if(geometry==null||!double.IsFinite(bulletDiameter)||bulletDiameter<=0||bulletDiameter>.02||capacity<1||capacity>128)throw new ArgumentException("Bounded impact history and physical bullet diameter required.");
            this.geometry=geometry;this.bulletDiameter=bulletDiameter;ids=new string[capacity];last=new ShotImpactEvidence[capacity];shots=new ShotImpactEvidence[capacity*5];
        }
        public bool TryObserve(string athlete,long tick,int bout,int lane,int ordinal,ShootingStance stance,
            ShotOutcome outcome,TargetIntersection impact,out ShotImpactEvidence evidence)
        {
            evidence=default;
            if(string.IsNullOrWhiteSpace(athlete)||tick<0||bout<0||lane<1||ordinal<1||ordinal>5||outcome==ShotOutcome.NoShot)return false;
            if(stance!=ShootingStance.Prone&&stance!=ShootingStance.Standing)return false;
            if(outcome!=ShotOutcome.Miss&&outcome!=ShotOutcome.TargetClosed&&outcome!=ShotOutcome.AlreadyClosed)return false;
            if(!double.IsFinite(impact.X)||!double.IsFinite(impact.Y)||((outcome==ShotOutcome.TargetClosed||outcome==ShotOutcome.AlreadyClosed)&&!impact.Hit))return false;
            if(impact.ReachesPlane&&(impact.TargetIndex < -1||impact.TargetIndex>=5)||outcome==ShotOutcome.Miss&&impact.Hit)return false;
            int index=Index(athlete);
            if(index<0){if(used==ids.Length)return false;index=used++;ids[index]=athlete;}
            var previous=last[index];
            if(previous.Valid&&(tick<=previous.Tick||bout<previous.Bout||bout==previous.Bout&&ordinal<=previous.Ordinal))return false;
            if(!previous.Valid||bout!=previous.Bout)Array.Clear(shots,index*5,5);
            evidence=new ShotImpactEvidence(athlete,tick,bout,lane,ordinal,stance,outcome,impact,geometry,bulletDiameter);
            last[index]=evidence;shots[index*5+ordinal-1]=evidence;AcceptedCount++;return true;
        }
        public ShotImpactEvidence Last(string athlete){int i=Index(athlete);return i<0?default:last[i];}
        public ShotImpactEvidence Shot(string athlete,int ordinal)
        {if(ordinal<1||ordinal>5)throw new ArgumentOutOfRangeException(nameof(ordinal));int i=Index(athlete);return i<0?default:shots[i*5+ordinal-1];}
        private int Index(string athlete){for(int i=0;i<used;i++)if(ids[i]==athlete)return i;return -1;}
        public void Clear(){Array.Clear(ids,0,ids.Length);Array.Clear(last,0,last.Length);Array.Clear(shots,0,shots.Length);used=AcceptedCount=0;}
    }
}

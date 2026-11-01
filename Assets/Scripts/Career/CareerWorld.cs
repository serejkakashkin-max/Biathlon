using System;
using System.Collections.Generic;
using System.Globalization;
using Biathlon.Domain;

namespace Biathlon.Career
{
    /// <summary>Bounded persistent rivals. Background weeks reuse the real training pipeline;</n+    /// aggregate race workload is not recorded as a simulated race result.</summary>
    public static class CareerWorld
    {
        private static IRandomSource Stream(CareerSave save,CareerWorldDefinition d,long ordinal,Func<ulong,IRandomSource> restore)
        {
            ulong seed=unchecked(save.RandomState+d.SubstreamSalt+(ulong)ordinal*d.SubstreamStride);var r=restore(seed);
            if(r==null||r.State!=seed)throw new ArgumentException("World substream checkpoint mismatch.");return r;
        }
        private static CareerAthlete InitialPeer(CareerSave save,CareerDefinitions d,CareerCircuit circuit,Func<ulong,IRandomSource> restore)
        {
            var profile=CareerStartDefinition.FromJson(d.ReadDefinition("career_start_v1.json")).Get(d.World.Circuit(circuit).StartProfileId);
            long ordinal=save.NextAthleteOrdinal;var random=Stream(save,d.World,ordinal,restore);
            var peer=CareerFactory.Generate(d.Roster,save.Id,ordinal,random,age:CareerFactory.Draw(random,profile.PeerAgeMin,profile.PeerAgeMax));
            int level=CareerFactory.Draw(random,profile.PeerSkillMin,profile.PeerSkillMax);peer.Tier=profile.PlayerTier;
            for(int stat=0;stat<13;stat++)peer.SetStat((CareerStat)stat,Math.Min(peer.Potential,Math.Max(0,level+CareerFactory.Draw(random,-profile.PeerStatSpread,profile.PeerStatSpread))));
            peer.Appearance=d.RivalAppearance.Generate(peer.Id);
            save.NextAthleteOrdinal=checked(ordinal+1);return peer;
        }
        public static CareerSave Initialize(CareerSave source,CareerDefinitions d,Func<ulong,IRandomSource> restore)
        {
            if(source==null||d==null||restore==null)throw new ArgumentNullException();source.Validate();
            if(source.WorldPolicyId!=null){if(source.WorldPolicyId!=d.World.Id)throw new InvalidOperationException("Unknown saved world policy.");return source;}
            if(d.Calendar.Circuit==CareerCircuit.Open)return source;
            var save=CareerSave.FromJson(source.ToJson());var pool=new List<CareerWorldEntry>();
            for(int circuit=1;circuit<=3;circuit++)if((CareerCircuit)circuit!=d.Calendar.Circuit)
                for(int i=0;i<d.Roster.Population;i++)pool.Add(new CareerWorldEntry(InitialPeer(save,d,(CareerCircuit)circuit,restore),(CareerCircuit)circuit));
            save.SetPool(d.World.Id,pool);save.Validate();return save;
        }
        internal static void TrainBackgroundWeek(CareerSave save,CareerDefinitions d)
        {
            if(save.WorldPolicyId==null)return;if(save.WorldPolicyId!=d.World.Id)throw new InvalidOperationException("Unknown background world policy.");
            // One immutable calendar per circuit, rather than per athlete.
            var calendars=new CareerCalendar[4];for(int c=1;c<=3;c++)calendars[c]=CareerCalendar.FromJson(d.ReadDefinition(d.World.Circuit((CareerCircuit)c).CalendarResource));
            for(int i=0;i<save.PoolCount;i++)
            {
                var row=save.Pool(i);var a=row.Athlete;var calendar=calendars[(int)row.Circuit];
                var plan=CareerAiTraining.Plan(a,d.Training,calendar,d.Policy,save.Week,d.Growth,d.Adaptation);
                int quality=a.PendingCampQuality>0?a.PendingCampQuality:d.Policy.Quality;
                bool taper=save.Week<calendar.WeeksPerSeason&&calendar.AtWeek(save.Week+1)!=null&&plan.UsedSlots<calendar.TrainingSlots(save.Week);
                CareerTrainingWeek.Apply(a,plan,d.Training,d.Adaptation,d.Fatigue,d.Form,d.Mental,d.Health,quality,taper,d.Growth);
                if(plan.Count>0)a.PendingCampQuality=0;
                var e=calendar.AtWeek(save.Week);if(e!=null&&e.Eligible(a))CareerFatigue.Race(a,d.Fatigue,d.World.AggregateRaceMinutes*60000L);
                CareerFatigue.Rest(a,d.Fatigue,calendar.TrainingSlots(save.Week)-plan.UsedSlots);CareerMental.Rest(a,d.Mental);
                a.BackgroundWeeks=checked(a.BackgroundWeeks+1);a.ObservePeak(save.Season,d.Ratings);
            }
        }
        private static CareerCircuit Classify(CareerAthlete a,CareerCircuit previous,CareerDefinitions d)
        {
            int rating=BiathlonRatingEngine.Calculate(a,d.Ratings).Overall;
            if(rating>=(previous==CareerCircuit.WorldCup?d.World.WorldCupRetentionOverall:d.World.WorldCupAdmissionOverall))
            {a.Tier=CareerTier.International;return CareerCircuit.WorldCup;}
            if(a.Age<=d.World.JuniorMaximumAge)
            {a.Tier=CareerTier.Regional;return CareerCircuit.Junior;}
            a.Tier=CareerTier.National;return CareerCircuit.Development;
        }
        private static CareerCircuit PlayerCircuit(CareerAthlete player,CareerCircuit previous,CareerWorldDefinition d)=>
            player.Tier==CareerTier.International?CareerCircuit.WorldCup:previous==CareerCircuit.Junior&&player.Age<=d.JuniorMaximumAge?CareerCircuit.Junior:CareerCircuit.Development;
        private static CareerAthlete Prospect(CareerSave save,CareerDefinitions d,Func<ulong,IRandomSource> restore)
        {
            long ordinal=save.NextAthleteOrdinal;var random=Stream(save,d.World,ordinal,restore);
            var a=CareerFactory.Generate(d.Roster,save.Id,ordinal,random,age:CareerFactory.Draw(random,d.Generation.ProspectAgeMin,d.Generation.ProspectAgeMax));
            int level=CareerFactory.Draw(random,d.Generation.ProspectSkillMin,d.Generation.ProspectSkillMax);
            var spread=CareerStartDefinition.FromJson(d.ReadDefinition("career_start_v1.json")).Get(d.World.Circuit(CareerCircuit.Junior).StartProfileId).PeerStatSpread;
            for(int s=0;s<13;s++)a.SetStat((CareerStat)s,Math.Min(a.Potential,Math.Max(0,level+CareerFactory.Draw(random,-spread,spread))));
            a.Appearance=d.RivalAppearance.Generate(a.Id);
            save.NextAthleteOrdinal=checked(ordinal+1);return a;
        }
        /// <summary>After results/development archival and annual ageing. Returns a detached world.</summary>
        public static CareerSave AdvanceSeason(CareerSave source,CareerDefinitions d,Func<ulong,IRandomSource> restore)
        {
            if(source==null||d==null||restore==null)throw new ArgumentNullException();source.Validate();
            if(source.WorldPolicyId!=d.World.Id||source.RaceCount!=0||source.Week!=1||source.Season<2||source.LastWorldTransitionSeason>=source.Season)throw new InvalidOperationException("World transition requires one newly archived season.");
            var save=CareerSave.FromJson(source.ToJson());var entries=new List<CareerWorldEntry>();var compact=new List<CareerRetiredAthlete>();var incumbents=new HashSet<string>(StringComparer.Ordinal);
            var player=save.FindWorldAthlete(save.PlayerId);var target=PlayerCircuit(player,d.Calendar.Circuit,d.World);
            for(int i=0;i<save.AthleteCount;i++)incumbents.Add(save.Athlete(i).Id);
            for(int i=0;i<save.WorldAthleteCount;i++)
            {
                var a=save.WorldAthlete(i);if(a.Id==save.PlayerId)continue;
                if(a.Growth==null)throw new InvalidOperationException("World ageing requires individual profiles.");
                if(a.Age>=a.Growth.RetirementAge){compact.Add(new CareerRetiredAthlete(a,save.Season-1,"retired"));continue;}
                var previous=i<save.AthleteCount?d.Calendar.Circuit:save.Pool(i-save.AthleteCount).Circuit;
                entries.Add(new CareerWorldEntry(a,Classify(a,previous,d)));
            }
            int entrants=Math.Max(d.World.AnnualProspects,d.World.MinimumPopulation-(entries.Count+1));
            if(target==CareerCircuit.Junior)
            {int juniors=0;foreach(var e in entries)if(e.Circuit==CareerCircuit.Junior)juniors++;entrants=Math.Max(entrants,d.Roster.Population-1-juniors);}
            for(int i=0;i<entrants;i++)entries.Add(new CareerWorldEntry(Prospect(save,d,restore),CareerCircuit.Junior));
            // Select real existing identities for the next field; no new adult cohort or capability boost.
            var eligible=new List<CareerWorldEntry>();foreach(var e in entries)if(e.Circuit==target)eligible.Add(e);
            int Score(CareerWorldEntry e)=>BiathlonRatingEngine.Calculate(e.Athlete,d.Ratings).Overall+(incumbents.Contains(e.Athlete.Id)?d.World.IncumbentSelectionBonus:0);
            eligible.Sort((a,b)=>{int score=Score(b).CompareTo(Score(a));return score!=0?score:StringComparer.Ordinal.Compare(a.Athlete.Id,b.Athlete.Id);});
            if(eligible.Count<d.Roster.Population-1)
            {
                // Vacant senior slots qualify the best actual available non-junior athletes.
                var reserves=new List<CareerWorldEntry>();foreach(var e in entries)if(e.Circuit!=target&&e.Athlete.Age>d.World.JuniorMaximumAge)reserves.Add(e);
                reserves.Sort((a,b)=>{int score=Score(b).CompareTo(Score(a));return score!=0?score:StringComparer.Ordinal.Compare(a.Athlete.Id,b.Athlete.Id);});
                foreach(var e in reserves){if(eligible.Count>=d.Roster.Population-1)break;e.Circuit=target;e.Athlete.Tier=target==CareerCircuit.WorldCup?CareerTier.International:CareerTier.National;eligible.Add(e);}
            }
            if(eligible.Count<d.Roster.Population-1)throw new InvalidOperationException("Persistent world has insufficient eligible senior athletes.");
            var field=new CareerAthlete[d.Roster.Population];int playerIndex=0;for(int i=0;i<save.AthleteCount;i++)if(save.Athlete(i).Id==save.PlayerId)playerIndex=i;
            var chosen=new HashSet<string>(StringComparer.Ordinal);field[playerIndex]=player;int next=0;
            for(int i=0;i<field.Length;i++)if(i!=playerIndex){var a=eligible[next++].Athlete;field[i]=a;chosen.Add(a.Id);}
            var pool=new List<CareerWorldEntry>();foreach(var e in entries)if(!chosen.Contains(e.Athlete.Id))pool.Add(e);
            if(pool.Count+field.Length>d.World.MaximumPopulation)
            {
                // Compact overflow retains stable identity/history, and is explicitly not labelled retirement.
                pool.Sort((a,b)=>{int age=b.Athlete.Age.CompareTo(a.Athlete.Age);return age!=0?age:StringComparer.Ordinal.Compare(a.Athlete.Id,b.Athlete.Id);});
                int overflow=pool.Count+field.Length-d.World.MaximumPopulation;
                for(int i=0;i<overflow;i++)compact.Add(new CareerRetiredAthlete(pool[i].Athlete,save.Season-1,"world_capacity"));pool.RemoveRange(0,overflow);
            }
            pool.Sort((a,b)=>StringComparer.Ordinal.Compare(a.Athlete.Id,b.Athlete.Id));save.Athletes=field;save.SetPool(d.World.Id,pool);save.AddRetired(compact,d.World.RetiredHistoryLimit);
            save.CalendarId=CareerCalendar.FromJson(d.ReadDefinition(d.World.Circuit(target).CalendarResource)).Id;save.LastWorldTransitionSeason=save.Season;
            CareerDevelopment.InitializeWorld(save,d.Growth,d.Ratings,restore);save.Validate();return save;
        }
    }
}

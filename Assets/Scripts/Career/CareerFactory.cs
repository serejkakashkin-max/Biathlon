using System;
using System.Globalization;
using Biathlon.Domain;

namespace Biathlon.Career
{
    public static class CareerFactory
    {
        internal static int Draw(IRandomSource random,int minimum,int maximum)=>minimum+(int)(random.NextUInt64()%(ulong)(maximum-minimum+1));
        internal static CareerAthlete Generate(CareerRosterDefinition rules,string careerId,long ordinal,IRandomSource random,bool player=false,string playerName=null,string nation=null,string appearance="appearance_default",int? age=null)
        {
            string name=player?playerName:rules.GivenName(Draw(random,0,rules.GivenNameCount-1))+" "+rules.FamilyName(Draw(random,0,rules.FamilyNameCount-1));
            string country=player?nation:rules.Nation(Draw(random,0,rules.NationCount-1));
            int potential=Draw(random,rules.PotentialMin,rules.PotentialMax);var stats=new int[CareerUnits.StatCount];
            for(int i=0;i<stats.Length;i++)stats[i]=Math.Min(potential,Draw(random,player?rules.PlayerSkillMin:rules.AiSkillMin,player?rules.PlayerSkillMax:rules.AiSkillMax));
            return CareerAthlete.Create(careerId+"_athlete_"+ordinal.ToString(CultureInfo.InvariantCulture),name,country,appearance,rules.Personality(Draw(random,0,rules.PersonalityCount-1)),
                age??(player?rules.PlayerAge:Draw(random,rules.AiAgeMin,rules.AiAgeMax)),potential,stats,rules.InitialState,rules.StartingMoney);
        }
        public static CareerSave Create(CareerRosterDefinition rules,CareerCalendar calendar,string careerId,string playerName,string nation,string appearance,IRandomSource random)
        {
            if(rules==null||calendar==null||random==null)throw new ArgumentNullException();CareerUnits.Id(careerId);CareerUnits.Text(playerName,100,"player name");CareerUnits.Id(appearance);
            if(careerId.Length>64||!rules.HasNation(nation))throw new ArgumentException("Invalid career identity or unsupported nation.");
            var roster=new CareerAthlete[rules.Population];
            for(int i=0;i<roster.Length;i++)roster[i]=Generate(rules,careerId,i,random,i==0,playerName,nation,i==0?appearance:"appearance_default");
            return CareerSave.Create(careerId,rules.Id,calendar.Id,roster,roster[0].Id,random.State,roster.Length);
        }
        public static CareerSave Create(CareerRosterDefinition rules,CareerCalendar calendar,string careerId,string playerName,string nation,string appearance,IRandomSource random,CareerStartDefinition starts,string profileId,int? age=null)
        {
            if(starts==null)throw new ArgumentNullException(nameof(starts));var profile=starts.Get(profileId);int selectedAge=age??profile.DefaultAge;
            CareerUnits.Range(selectedAge,profile.MinimumAge,profile.MaximumAge,"selected starting age");
            if(calendar==null||calendar.Id!=profile.CalendarResource.Substring(0,profile.CalendarResource.Length-5))throw new ArgumentException("Start profile and calendar must match.");
            var save=Create(rules,calendar,careerId,playerName,nation,appearance,random);
            // Each peer owns a coherent ability level plus bounded specialization.
            // These become ordinary permanent stats; no controller-only power boost.
            for(int i=0;i<save.AthleteCount;i++)
            {
                var a=save.Athlete(i);a.Age=i==0?selectedAge:Draw(random,profile.PeerAgeMin,profile.PeerAgeMax);a.Tier=profile.PlayerTier;
                int level=Draw(random,i==0?profile.PlayerSkillMin:profile.PeerSkillMin,i==0?profile.PlayerSkillMax:profile.PeerSkillMax);
                for(int stat=0;stat<CareerUnits.StatCount;stat++)a.SetStat((CareerStat)stat,Math.Min(a.Potential,Math.Max(0,level+Draw(random,-profile.PeerStatSpread,profile.PeerStatSpread))));
            }
            save.StartProfileId=profile.Id;save.FirstSeasonYear=starts.FirstSeasonYear;save.RandomState=random.State;save.Validate();return save;
        }
        public static CareerSave Restore(string json,CareerRosterDefinition rules,CareerCalendar calendar,IRandomSource random)
        {
            if(rules==null||calendar==null||random==null)throw new ArgumentNullException();var save=CareerSave.FromJson(json);
            if(save.RulesId!=rules.Id||save.CalendarId!=calendar.Id||save.AthleteCount!=rules.Population||save.Week>calendar.WeeksPerSeason||save.RandomState!=random.State)throw new ArgumentException("Save context/RNG does not match the supplied career definitions.");return save;
        }
    }
}

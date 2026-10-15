using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
namespace Biathlon.AI
{
    [DataContract] public sealed class CareerRaceDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="tick_frequency",IsRequired=true)] public int TickFrequency { get; private set; }
        [DataMember(Name="maximum_ticks",IsRequired=true)] public int MaximumTicks { get; private set; }
        [DataMember(Name="maximum_fatigue_penalty",IsRequired=true)] public int MaximumFatiguePenalty { get; private set; }
        public static CareerRaceDefinition FromJson(string json)
        {
            CareerRaceDefinition d;using(var stream=new System.IO.MemoryStream(Encoding.UTF8.GetBytes(json)))d=(CareerRaceDefinition)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(CareerRaceDefinition)).ReadObject(stream);
            if(d==null||d.SchemaVersion!=1||string.IsNullOrWhiteSpace(d.Id)||d.TickFrequency<1||d.TickFrequency>120||d.MaximumTicks<1||d.MaximumTicks>1000000||d.MaximumFatiguePenalty<0||d.MaximumFatiguePenalty>10000)throw new SerializationException("Invalid career race settings.");return d;
        }
    }
    public sealed class CareerRaceProvider : ICareerRaceProvider
    {
        private readonly Func<string,string> read,fixture,venueResource;private readonly CareerDefinitions career;private readonly CareerRaceDefinition definition;
        private const string Woodland="woodland_basin_v1";
        private const string WoodlandRoot="Biathlon/Venues/woodland_basin_v1/";
        public long LastTicks { get; private set; } public ulong LastEventHash { get; private set; } public int LastShots { get; private set; }
        public CareerRaceProvider(Func<string,string> read,Func<string,string> fixture,CareerDefinitions career,
            Func<string,string> venueResource=null)
        {this.read=read??throw new ArgumentNullException(nameof(read));this.fixture=fixture;this.career=career??throw new ArgumentNullException(nameof(career));this.venueResource=venueResource;definition=CareerRaceDefinition.FromJson(read("career_race_v1.json"));}
        public CareerRaceProvider(Func<string,string> read,CareerDefinitions career,Func<string,string> venueResource)
            :this(read,null,career,venueResource) { }
        private static readonly string[] statNames={"endurance","anaerobic_capacity","recovery","strength","ski_technique","climbing","downhill","cornering","breath_control","prone_stability","standing_stability","shooting_speed","composure"};
        private static string AthleteJson(CareerAthlete athlete,int from,int to)
        {var b=new StringBuilder("{\"schema_version\":1,\"id\":\"").Append(athlete.Id).Append('"');for(int i=from;i<to;i++)b.Append(",\"").Append(statNames[i]).Append("\":").Append(Math.Round(athlete.Stat((CareerStat)i)/(decimal)CareerUnits.StatScale,0,MidpointRounding.ToEven).ToString(CultureInfo.InvariantCulture));return b.Append('}').ToString();}
        public CareerRaceRecord Run(CareerSave snapshot,CareerCalendarEvent e,ulong seed)
        {
            var prepared=Prepare(snapshot,e,seed);var runner=prepared.Runner;
            if(prepared.Venue!=null)
            {
                var movement=ManualProductionFieldComposition.Create(runner.Field,prepared.Venue,prepared.ManualRead,prepared.CourseReactionPolicy);
                runner.BindManualScopedCandidate(movement,ManualAiSteeringDefinition.FromJson(read("manual_ai_steering_v2.json")),
                    ManualSkiSteeringDefinition.FromJson(read("manual_ski_steering_v2.json")),PlayerCourseAssistanceDefinition.FromJson(read("player_course_assistance_v1.json")));
            }
            while(!runner.Field.IsComplete){if(runner.Field.Tick.Value>=definition.MaximumTicks)throw new InvalidOperationException("Career race exceeded its tick budget.");runner.Step();}
            var result=prepared.CollectResult();
            LastTicks=runner.Field.Tick.Value;LastEventHash=prepared.Audit.Hash;LastShots=prepared.Audit.Shots;
            return result;
        }
        public RaceWeatherPreset Forecast(CareerSave snapshot,CareerCalendarEvent e)
        {
            if(snapshot==null||e==null)throw new ArgumentNullException();
            if(e.VenueId!=Woodland)return null;
            for(int i=0;i<career.Calendar.Count;i++)if(career.Calendar.Get(i).Id==e.Id)
                return RaceWeatherCatalog.FromJson(read("career_weather_v1.json")).Select(i,snapshot.Season);
            throw new ArgumentException("Weather needs an event from this career calendar.");
        }
        public CareerPreparedRace Prepare(CareerSave snapshot,CareerCalendarEvent e,ulong seed,IRaceEventSink observedEvents=null,
            RaceWeatherPreset recordedWeather=null,bool legacyNeutralWeather=false,bool legacyMassStartOrder=false,
            ManualCourseReactionPolicy courseReactionPolicy=ManualCourseReactionPolicy.PassiveCurvedV26)
        {
            if(snapshot==null||e==null)throw new ArgumentNullException();
            if(courseReactionPolicy!=ManualCourseReactionPolicy.LegacyV25&&courseReactionPolicy!=ManualCourseReactionPolicy.PassiveCurvedV26)
                throw new ArgumentOutOfRangeException(nameof(courseReactionPolicy));
            if(e.VenueId!="synthetic_alpine"&&e.VenueId!=Woodland)throw new ArgumentException("No route adapter for this venue.");
            if(e.VenueId==Woodland&&venueResource==null)throw new InvalidOperationException("Woodland career needs its explicit resource reader.");
            if(e.VenueId=="synthetic_alpine"&&fixture==null)throw new InvalidOperationException("Synthetic career needs its explicit fixture reader.");
            var athletes=new Dictionary<string,CareerAthlete>(StringComparer.Ordinal);for(int i=0;i<snapshot.AthleteCount;i++)if(e.Eligible(snapshot.Athlete(i)))athletes.Add(snapshot.Athlete(i).Id,snapshot.Athlete(i));
            PursuitQualificationSnapshot qualification=null;
            if(e.Format==CompetitionFormat.Pursuit)
            {
                CareerRaceRecord source=null;for(int i=0;i<snapshot.RaceCount;i++)if(snapshot.Race(i).CalendarEventId==e.SourceEventId)source=snapshot.Race(i);
                if(source==null||source.AuthorityResultJson==null||BalanceRegression.Hash(source.AuthorityResultJson)!=source.AuthoritySha256)throw new ArgumentException("Missing or altered authoritative Sprint result.");
                var pursuitProfile=e.VenueId==Woodland?
                    CompetitionProfile.FromJson(venueResource(WoodlandRoot+"pursuit_profile")):
                    CompetitionProfile.FromJson(read("pursuit_test_v1.json"));
                qualification=PursuitQualificationSnapshot.Create(snapshot.SeasonId+"_"+e.Id+"_qualification",SprintResultSnapshot.FromJson(source.AuthorityResultJson),pursuitProfile);
                if(qualification.Count!=athletes.Count)throw new ArgumentException("Qualified and eligible fields differ.");
            }
            var roster=new List<CareerAthlete>();if(qualification!=null){for(int i=0;i<qualification.Count;i++)roster.Add(athletes[qualification.Get(i).AthleteId]);}else for(int i=0;i<snapshot.AthleteCount;i++)if(athletes.ContainsKey(snapshot.Athlete(i).Id))roster.Add(snapshot.Athlete(i));
            if(e.Format==CompetitionFormat.MassStart&&!legacyMassStartOrder)
            {
                var seeded=CareerMassStartSeeding.Capture(snapshot,career.Calendar,career.Scoring,e);roster.Clear();
                foreach(string id in seeded)roster.Add(athletes[id]);
            }
            var entrants=new SprintEntrant[roster.Count];var personalities=new AiPersonalityDefinition[roster.Count];var forms=new double[roster.Count];var pressures=new ShootingPressureContext[roster.Count];
            for(int i=0;i<roster.Count;i++)
            {
                var a=roster[i];entrants[i]=new SprintEntrant(TimeTrialAthlete.FromJson(AthleteJson(a,0,8)),ShootingAthleteDefinition.FromJson(AthleteJson(a,8,13)),a.SelectedSki);personalities[i]=AiPersonalityDefinition.FromJson(read(a.PersonalityId+".json"));
                // Explicit temporary readiness projection, shared by every career entrant.
                forms[i]=CareerForm.RaceMultiplier(a,career.Form)*(1-a.Fatigue/(double)CareerUnits.Basis*definition.MaximumFatiguePenalty/CareerUnits.Basis);
                pressures[i]=new ShootingPressureContext(CareerMental.Pressure(a,career.Mental),1,1,a.Confidence/(double)CareerUnits.Basis);
            }
            var audit=new BalanceEventAudit(5);
            IRaceEventSink eventSink=observedEvents==null?audit:new ObservedEvents(audit,observedEvents);
            PackAiRaceRunner runner;VenueAuthoringPlan venue=null;
            var weather=legacyNeutralWeather?null:recordedWeather??Forecast(snapshot,e);
            if(e.VenueId==Woodland)
            {
                venue=new VenueAuthoringPlan(VenueManifest.FromJson(venueResource(WoodlandRoot+"manifest")),venueResource);
                string format=e.Format==CompetitionFormat.Sprint?"sprint":e.Format==CompetitionFormat.Pursuit?"pursuit":"mass_start";
                runner=ProductionVenueRaceFactory.Create(venue,
                    CompetitionProfile.FromJson(venueResource(WoodlandRoot+format+"_profile")),
                    RaceRoutePlan.FromJson(venueResource(WoodlandRoot+format+"_route")),
                    weather==null?RaceSnowConditions.FromJson(read("race_snow_neutral_v1.json")):
                        weather.SnowConditions(RaceSnowConditions.FromJson(read("race_snow_neutral_v1.json"))),read,eventSink,new AiDecisionCounts(),
                    qualification,roster.Count,definition.TickFrequency,seed,entrants,personalities,forms,pressures,
                    raceWind:weather==null?default:new WindVector(weather.WindEast,weather.WindNorth),
                    massStartNumberFromRight:!legacyMassStartOrder&&career.Calendar.MassStartNumberFromRight);
            }
            else runner=ChampionshipRaceFactory.Create(read,fixture,e.Format,eventSink,new AiDecisionCounts(),qualification,roster.Count,definition.TickFrequency,seed,entrants,personalities,forms,pressures);
            Func<string,string> manualRead=!legacyMassStartOrder&&weather?.CourseContactResource!=null?
                n=>read(n=="manual_course_contact_gameplay_v2.json"?weather.CourseContactResource:n):read;
            return new CareerPreparedRace(snapshot.SeasonId,snapshot.PlayerId,e,seed,runner,audit,venue,weather,manualRead,courseReactionPolicy);
        }
        private sealed class ObservedEvents:IRaceEventSink
        {
            private readonly IRaceEventSink authority,observer;
            internal ObservedEvents(IRaceEventSink authority,IRaceEventSink observer){this.authority=authority;this.observer=observer;}
            public void Record(RaceEvent value){authority.Record(value);observer.Record(value);}
        }
    }
    /// <summary>The same career composition used headlessly and by a live player host.</summary>
    public sealed class CareerPreparedRace
    {
        public PackAiRaceRunner Runner { get; }
        public VenueAuthoringPlan Venue { get; }
        public CareerCalendarEvent Event { get; }
        public ulong Seed { get; }
        public RaceWeatherPreset Weather {get;}
        public Func<string,string> ManualRead {get;}
        public ManualCourseReactionPolicy CourseReactionPolicy {get;}
        public int PlayerIndex { get; }
        internal BalanceEventAudit Audit { get; }
        private readonly string seasonId;
        internal CareerPreparedRace(string seasonId,string playerId,CareerCalendarEvent e,ulong seed,PackAiRaceRunner runner,BalanceEventAudit audit,VenueAuthoringPlan venue,RaceWeatherPreset weather,Func<string,string> manualRead,ManualCourseReactionPolicy courseReactionPolicy)
        {
            this.seasonId=seasonId;Event=e;Seed=seed;Runner=runner;Audit=audit;Venue=venue;Weather=weather;ManualRead=manualRead;CourseReactionPolicy=courseReactionPolicy;PlayerIndex=-1;
            for(int i=0;i<runner.Field.Count;i++)if(runner.Field.GetAthlete(i).AthleteId.Value==playerId)PlayerIndex=i;
        }
        public CareerRaceRecord CollectResult()
        {
            var field=Runner.Field;var e=Event;
            if(!field.IsComplete)throw new InvalidOperationException("A complete authoritative field is required for a career result.");
            string payload=e.Format==CompetitionFormat.Sprint?field.Result.ToJson():e.Format==CompetitionFormat.Pursuit?field.PursuitResult.ToJson():field.MassStartResult.ToJson();var finishes=new List<CareerFinish>();
            for(int rank=0;rank<field.Count;rank++)
            {
                string id=e.Format==CompetitionFormat.Sprint?field.Result.Get(rank).AthleteId:e.Format==CompetitionFormat.Pursuit?field.PursuitResult.Get(rank).AthleteId:field.MassStartResult.Get(rank).AthleteId;
                SprintAthleteSession actor=null;for(int i=0;i<field.Count;i++)if(field.GetAthlete(i).AthleteId.Value==id)actor=field.GetAthlete(i);
                long elapsed=(long)Math.Round(actor.Lifecycle.ElapsedFinishTime.Seconds*1000,MidpointRounding.ToEven);
                long ordering=e.Format==CompetitionFormat.Sprint?(long)Math.Round(field.Result.Get(rank).RegisteredUnits.Value*1000m/ResultTimingDefinition.FromJson(field.Result.TimingDefinitionJson).UnitsPerSecond,MidpointRounding.ToEven):(long)Math.Round(actor.Lifecycle.GlobalFinishTime.Seconds*1000,MidpointRounding.ToEven);
                var masks=new int[field.Profile.BoutCount];var stances=new ShootingStance[masks.Length];
                for(int b=0;b<masks.Length;b++){masks[b]=actor.GetBoutClosedMask(b);stances[b]=field.Profile.GetBout(b).Stance;}
                var splits=new CareerResultSplit[actor.Checkpoints.Count];
                for(int s=0;s<splits.Length;s++)
                {
                    var split=actor.Checkpoints.Get(s);
                    splits[s]=new CareerResultSplit(split.CheckpointId,split.LapId,
                        (long)Math.Round(split.ElapsedTime.Seconds*1000,MidpointRounding.ToEven),
                        (long)Math.Round(split.GlobalTime.Seconds*1000,MidpointRounding.ToEven));
                }
                var details=new CareerResultDetails(id,masks,stances,splits);
                finishes.Add(new CareerFinish(id,ordering,elapsed,actor.TotalPenaltyLoops,details:details));
            }
            return new CareerRaceRecord(seasonId+"_"+e.Id,seasonId,e.Id,e.Format,Seed,BalanceRegression.Hash(payload),finishes,payload);
        }
    }
}

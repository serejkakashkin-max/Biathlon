using System;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class FreshMassStartPackTests
    {
        // Main 80–180 m is the actual first sustained Woodland climb. This
        // short production-field check isolates fresh pack behaviour before
        // any shooting, penalties or season-fatigue interpretation can apply.
        private static string Read(string n)=>PhysiologyContractTests.DefinitionJson(n);
        private static string Venue(string n)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+n+".json");
        private static (PackAiRaceRunner runner,ManualScopedFieldStepper movement) Field(string profile)
        {
            if(profile=="review") {var f=ManualProductionFieldTests.Initial("mass_start",30);return(f.runner,f.stepper);}
            var d=CareerDefinitions.ForNewProductionCareer(Read,profile);
            var save=CareerFactory.Create(d.Roster,d.Calendar,"fresh_pack","Alex North","NOR","player_appearance",new SeededRandomSource(71000),CareerStartDefinition.FromJson(Read("career_start_v1.json")),profile);
            CareerCalendarEvent race=null;
            for(int i=0;i<d.Calendar.Count;i++)if(d.Calendar.Get(i).Format==CompetitionFormat.MassStart){race=d.Calendar.Get(i);break;}
            var p=new CareerRaceProvider(Read,d,Venue).Prepare(save,race,123);
            var movement=ManualProductionFieldComposition.Create(p.Runner.Field,p.Venue,Read);
            p.Runner.BindManualScopedCandidate(movement,ManualAiSteeringDefinition.FromJson(Read("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2.json")));
            return(p.Runner,movement);
        }
        [Test,Timeout(180000)]
        public void FreshAiDoesNotChooseLowPowerTuckWhileClimbing()
        {
            var f=Field("junior_novice");var sampler=new TrackSampler(f.runner.Field.Track.GetLoop("main"));int observed=0;
            for(int tick=0;tick<1000;tick++)
            {
                f.runner.Step();
                for(int i=0;i<f.runner.Field.Count;i++)
                {
                    var point=f.runner.Field.GetPhysicalPoint(i);
                    if(point.LoopId!="main")continue;
                    var sample=sampler.Sample(new TrackDistance(point.LocalNanometers/SkiState.NanometersPerMillimeter));
                    if(sample.Grade<=0.005)continue;observed++;
                    Assert.That(f.runner.GetLastInput(i).Ski.TuckRequested,Is.False,
                        "Automatic posture must not use player freedom to crouch at low power on a climb; actor "+i+" main="+point.LocalNanometers/(double)SkiState.NanometersPerMeter);
                }
            }
            Assert.That(observed,Is.GreaterThan(100));
        }
        [Test,Timeout(180000)]
        public void RepeatedFreshOpeningKeepsExactCommandsPosesAndResources()
        {
            var first=Field("junior_novice");var second=Field("junior_novice");
            Assert.That(first.runner.ContextJson,Is.EqualTo(second.runner.ContextJson));
            for(int tick=0;tick<1000;tick++)
            {
                first.runner.Step();second.runner.Step();
                if(tick%20!=0)continue;
                for(int i=0;i<first.runner.Field.Count;i++)
                {
                    var a=first.movement.World.GetActor(i);var b=second.movement.World.GetActor(i);
                    Assert.That(a.Pose.X,Is.EqualTo(b.Pose.X));Assert.That(a.Pose.Z,Is.EqualTo(b.Pose.Z));
                    Assert.That(a.Pose.HeadingRadians,Is.EqualTo(b.Pose.HeadingRadians));
                    Assert.That(first.runner.GetLastManualBrake(i),Is.EqualTo(second.runner.GetLastManualBrake(i)));
                    Assert.That(first.runner.GetLastInput(i).Ski.Effort.RequestedEffort,Is.EqualTo(second.runner.GetLastInput(i).Ski.Effort.RequestedEffort));
                    Assert.That(first.runner.Field.GetAthlete(i).PhysiologyState.AnaerobicReserve,Is.EqualTo(second.runner.Field.GetAthlete(i).PhysiologyState.AnaerobicReserve));
                }
            }
        }
        [TestCase("review"),TestCase("junior_novice"),TestCase("senior_debut"),Timeout(180000)]
        public void FirstRegisteredSplitRetainsACompetitiveFreshField(string profile)
        {
            var f=Field(profile);var r=f.runner;int completed=0;var times=new double[r.Field.Count];
            while(completed<times.Length&&r.Field.Tick.Value<6000)
            {
                r.Step();completed=0;
                for(int i=0;i<times.Length;i++)
                {
                    var checkpoints=r.Field.GetAthlete(i).Checkpoints;
                    if(checkpoints.Count==0)continue;
                    Assert.That(checkpoints.Get(0).CheckpointId,Is.EqualTo("climb_top"));
                    times[i]=checkpoints.Get(0).ElapsedTime.Seconds;completed++;
                }
            }
            Array.Sort(times);
            TestContext.WriteLine(FormattableString.Invariant($"FIRST_REGISTERED_SPLIT profile={profile} completed={completed} p50_gap_s={times[times.Length/2]-times[0]:F3} p90_gap_s={times[(times.Length-1)*9/10]-times[0]:F3} max_gap_s={times[times.Length-1]-times[0]:F3}"));
            Assert.That(completed,Is.EqualTo(times.Length));
            Assert.That(times[(times.Length-1)*9/10]-times[0],Is.LessThan(20),
                "The first actual timing station must not split most fresh entrants by dozens of seconds.");
        }
        [TestCase("review"),TestCase("junior_novice"),TestCase("senior_debut"),Timeout(180000)]
        public void FreshThirtyFieldClimbsWithoutStationaryOrCoastingTail(string profile)
        {
            var f=Field(profile);var r=f.runner;int n=r.Field.Count;
            var crossings=new long[n];var speeds=new double[n];var minimum=new double[n];var hillTicks=new long[n];var slow=new long[n];var brakes=new long[n];var zeroEffort=new long[n];var reserve=new double[n];var fatigue=new double[n];var efforts=new long[n];
            Array.Fill(minimum,double.MaxValue);int completed=0;
            while(completed<n&&r.Field.Tick.Value<3000)
            {
                r.Step();
                for(int i=0;i<n;i++)
                {
                    var at=r.Field.GetPhysicalPoint(i);var a=r.Field.GetAthlete(i);
                    if(at.LoopId!="main"||at.LocalNanometers<80e9||at.LocalNanometers>220e9)continue;
                    double speed=a.Motion.SpeedMetersPerSecond;
                    if(at.LocalNanometers<180e9){hillTicks[i]++;minimum[i]=Math.Min(minimum[i],speed);if(speed<1)slow[i]++;if(r.GetLastManualBrake(i)>.05)brakes[i]++;if(r.GetLastInput(i).Ski.Effort.RequestedEffort==0)zeroEffort[i]++;efforts[i]+=r.GetLastInput(i).Ski.Effort.RequestedEffort;}
                    else if(crossings[i]==0){crossings[i]=r.Field.Tick.Value;speeds[i]=speed;reserve[i]=a.PhysiologyState.AnaerobicReserve;fatigue[i]=a.PhysiologyState.MuscularFatigue;completed++;}
                }
            }
            var ordered=(long[])crossings.Clone();Array.Sort(ordered);var orderedSpeeds=(double[])speeds.Clone();Array.Sort(orderedSpeeds);
            for(int i=0;i<n;i++)TestContext.WriteLine("FRESH_PACK profile="+profile+" actor="+i+" crest_s="+crossings[i]/20.0+" crest_mps="+speeds[i]+" min_mps="+minimum[i]+" slow_ticks="+slow[i]+" brake_ticks="+brakes[i]+" zero_effort_ticks="+zeroEffort[i]+" hill_ticks="+hillTicks[i]+" average_effort="+efforts[i]/Math.Max(1,hillTicks[i])+" reserve="+reserve[i]+" fatigue="+fatigue[i]);
            TestContext.WriteLine("FRESH_PACK_SUMMARY profile="+profile+" completed="+completed+" p50_gap_s="+(ordered[n/2]-ordered[0])/20.0+" p90_gap_s="+(ordered[(n-1)*9/10]-ordered[0])/20.0+" max_gap_s="+(ordered[n-1]-ordered[0])/20.0+" p10_speed="+orderedSpeeds[(n-1)/10]+" p50_speed="+orderedSpeeds[n/2]+" max_speed="+orderedSpeeds[n-1]);
            Assert.That(completed,Is.EqualTo(n),"Fresh entrants must reach the first crest in a bounded opening.");
            Assert.That(orderedSpeeds[(n-1)/10],Is.GreaterThan(2),"Most rested entrants must climb rather than creep below walking speed.");
            Assert.That((ordered[(n-1)*9/10]-ordered[0])/20.0,Is.LessThan(20),"The opening should not immediately split a fresh coherent field by dozens of seconds.");
        }
    }
}

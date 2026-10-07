using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualWorldRosterLedgerTests
    {
        private static string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        private static long Nano(double metres)=>(long)Math.Round(metres*SkiState.NanometersPerMeter);
        private static (RaceRoute route,TrackVisualDefinition visual,OccupancyDefinition body,
            ManualRouteProjectionMap map,ManualRouteJointSweep sweep,ManualRouteProgression progress) Create()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+"mass_start_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(root+"mass_start_route")),
                profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            var body=OccupancyDefinition.FromJson(Read("Biathlon/occupancy_provisional_v1"));
            var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"world_ledger_contact_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"world_ledger_work_v1\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            var map=new ManualRouteProjectionMap(route,venue.Visual);
            return (route,venue.Visual,body,map,new ManualRouteJointSweep(map,body,contact,work),
                new ManualRouteProgression(route));
        }
        private static SkiWorldMotionCandidate Actor(string id,OccupancyDefinition body,
            ManualSkiHeadingState pose,SkiPlanarVelocity velocity)
        {
            var athlete=new AthleteId(id);
            var contact=new ManualSkiContactBody(athlete,pose,velocity,75);
            var trajectory=new ManualSkiSnowContactSystem(body,1).PreviewTrajectory(contact,
                new SkiContactMotion(velocity,pose.YawRateRadiansPerSecond),0,0,.05);
            return new SkiWorldMotionCandidate(athlete,new ManualSkiWorldTrajectory(trajectory));
        }
        private static ManualSkiHeadingState At(TrackVisualPoint p)
            =>new ManualSkiHeadingState(p.X,p.Z,Math.Atan2(p.ForwardX,p.ForwardZ));
        private static ManualWorldRosterLedger Ledger(RaceRoute route,ManualRouteProjectionMap map,
            ManualRouteJointSweep sweep,ManualRouteProgression progress,
            SkiWorldMotionCandidate[] actors,long[] stations)
        {
            var cursors=new ManualRouteProgressCursor[actors.Length];
            var poses=new ManualSkiHeadingState[actors.Length];var velocities=new SkiPlanarVelocity[actors.Length];
            for(int i=0;i<actors.Length;i++)
            {
                cursors[i]=progress.Reconstitute(actors[i].Athlete,0,stations[i],stations[i]);
                poses[i]=actors[i].Trajectory.Sample(0);velocities[i]=actors[i].Trajectory.VelocityAt(0);
            }
            return new ManualWorldRosterLedger(route,map,sweep,progress,cursors,poses,velocities,20);
        }
        private static ManualRouteActorWindow[] Windows(SkiWorldMotionCandidate[] actors,long[] station)
        {
            var result=new ManualRouteActorWindow[actors.Length];
            for(int i=0;i<actors.Length;i++)result[i]=new ManualRouteActorWindow(actors[i].Athlete,0,station[i],2,false);
            return result;
        }
        private static bool Prepare(ManualWorldRosterLedger ledger,SkiWorldMotionCandidate[] actors,
            ManualRouteActorWindow[] windows,long[] caps,out ManualWorldRosterPreview preview)
            =>ledger.TryPrepare(actors,windows,caps,Array.Empty<SkiReachedContactPair>(),0,
                Array.Empty<SkiActorCourseEdge>(),0,out preview);

        [TestCase("lateral")]
        [TestCase("heading")]
        [TestCase("yaw_rate")]
        [TestCase("velocity")]
        [TestCase("route_credit")]
        [TestCase("clock")]
        public void InitialWorldStateMustHaveAnUnambiguousDeterministicContext(string changed)
        {
            var f=Create();var point=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var pose=At(point);var velocity=new SkiPlanarVelocity(0,0);var id=new AthleteId("context_actor");
            long station=Nano(10);
            ManualWorldRosterLedger Make(bool alternative)
            {
                var at=pose;var speed=velocity;long observed=station,credit=station;
                if(alternative)
                {
                    if(changed=="lateral")at=new ManualSkiHeadingState(
                        pose.X+.1*point.ForwardZ,pose.Z-.1*point.ForwardX,pose.HeadingRadians);
                    if(changed=="heading")at=new ManualSkiHeadingState(pose.X,pose.Z,pose.HeadingRadians+.1);
                    if(changed=="yaw_rate")at=new ManualSkiHeadingState(pose.X,pose.Z,pose.HeadingRadians,.1);
                    if(changed=="velocity")speed=new SkiPlanarVelocity(.1,0);
                    if(changed=="route_credit")credit=station+Nano(1);
                }
                Assert.That(f.map.TryProject(at,0,station,.001,false,out var projection),Is.True);
                observed=projection.RouteNanometers;credit=Math.Max(credit,observed);
                return new ManualWorldRosterLedger(f.route,f.map,f.sweep,f.progress,
                    new[]{f.progress.Reconstitute(id,0,observed,credit)},new[]{at},new[]{speed},20,
                    initialTick:new SimulationTick(alternative&&changed=="clock"?7:0));
            }
            var baseline=Make(false);
            Assert.That(Make(false).ContextJson,Is.EqualTo(baseline.ContextJson));
            Assert.That(string.Equals(Make(true).ContextJson,baseline.ContextJson,StringComparison.Ordinal),Is.False,
                "A replay context must distinguish accepted initial "+changed+" before any world tick.");
        }

        [Test] public void InitialWorldReceiptRoundTripsExactStateAndIsCultureIndependent()
        {
            var f=Create();var point=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var actor=Actor("receipt_actor",f.body,At(point),new SkiPlanarVelocity(3*point.ForwardX,3*point.ForwardZ));
            var pose=actor.Trajectory.Sample(0);var velocity=actor.Trajectory.VelocityAt(0);
            var cursor=f.progress.Reconstitute(actor.Athlete,0,Nano(10),Nano(12));
            var motion=new SkiState(Nano(7),3);var tick=new SimulationTick(7);
            ManualWorldRosterLedger Make()=>new ManualWorldRosterLedger(f.route,f.map,f.sweep,f.progress,
                new[]{cursor},new[]{pose},new[]{velocity},20,new[]{motion},tick);
            string[] Decode(string json)
            {
                using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    return (string[])new DataContractJsonSerializer(typeof(string[])).ReadObject(stream);
            }
            var original=Make();string receipt=original.InitialStateContextJson;
            var fields=Decode(receipt);var row=Decode(fields[5]);
            Assert.That(fields[0],Is.EqualTo("manual_world_initial_roster_state_v1"));
            Assert.That(fields[1],Is.EqualTo("20"));Assert.That(fields[2],Is.EqualTo("7"));
            Assert.That(fields[3],Is.EqualTo("1"));Assert.That(row.Length,Is.EqualTo(12));
            var format=CultureInfo.InvariantCulture;
            var recoveredPose=new ManualSkiHeadingState(double.Parse(row[4],format),double.Parse(row[5],format),
                long.Parse(row[6],format)/(double)ManualSkiHeadingState.NanoradiansPerRadian,double.Parse(row[7],format));
            var restored=new ManualWorldRosterLedger(f.route,f.map,f.sweep,f.progress,
                new[]{f.progress.Reconstitute(new AthleteId(row[0]),int.Parse(row[1],format),
                    long.Parse(row[2],format),long.Parse(row[3],format))},new[]{recoveredPose},
                new[]{new SkiPlanarVelocity(double.Parse(row[8],format),double.Parse(row[9],format))},
                int.Parse(fields[1],format),new[]{new SkiState(long.Parse(row[10],format),double.Parse(row[11],format))},
                new SimulationTick(long.Parse(fields[2],format)));
            Assert.That(restored.GetActor(0).Pose,Is.EqualTo(pose));
            Assert.That(restored.GetActor(0).Velocity,Is.EqualTo(velocity));
            Assert.That(restored.GetCursor(0),Is.EqualTo(cursor));
            Assert.That(restored.GetPhysicalMotion(0),Is.EqualTo(motion));
            Assert.That(restored.InitialStateContextJson,Is.EqualTo(receipt));
            var priorCulture=CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture=new CultureInfo("ru-RU");
                Assert.That(Make().InitialStateContextJson,Is.EqualTo(receipt));
            }
            finally{CultureInfo.CurrentCulture=priorCulture;}
            Assert.That(original.TryPrepareWithOdometer(new[]{actor},
                Windows(new[]{actor},new[]{Nano(10)}),new[]{Nano(20)},
                Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,
                new[]{0d},out var next),Is.True);
            original.Commit(next);
            Assert.That(original.Tick.Value,Is.EqualTo(8));
            Assert.That(original.InitialStateContextJson,Is.EqualTo(receipt));
        }

        [Test] public void ExactEndpointHandoffKeepsWorldPoseClockAndOdometer()
        {
            var f=Create();var first=f.route.GetSection(0);var second=f.route.GetSection(1);
            long end=checked((first.Start.Millimeters+first.Length.Millimeters)*SkiState.NanometersPerMillimeter);
            Assert.That(second.Start.Millimeters*SkiState.NanometersPerMillimeter,Is.EqualTo(end));
            var point=f.visual.Get(first.LoopId).Sample(first.LoopEnd.Meters);
            var actor=Actor("handoff",f.body,At(point),new SkiPlanarVelocity(0,0));
            var cursor=f.progress.Reconstitute(actor.Athlete,0,end,end);
            var pose=actor.Trajectory.Sample(0);var velocity=actor.Trajectory.VelocityAt(0);
            var ledger=new ManualWorldRosterLedger(f.route,f.map,f.sweep,f.progress,
                new[]{cursor},new[]{pose},new[]{velocity},20,
                new[]{new SkiState(Nano(50),0)});
            var motion=ledger.GetPhysicalMotion(0);var before=ledger.GetActor(0);
            Assert.That(ledger.TryHandoffSections(new[]{0}),Is.True);
            Assert.That(ledger.Tick.Value,Is.Zero);
            Assert.That(ledger.GetActor(0).Pose,Is.EqualTo(before.Pose));
            Assert.That(ledger.GetActor(0).Velocity,Is.EqualTo(before.Velocity));
            Assert.That(ledger.GetPhysicalMotion(0),Is.EqualTo(motion));
            Assert.That(ledger.GetCursor(0).SectionIndex,Is.EqualTo(1));
            Assert.That(ledger.GetCursor(0).CreditedStationNanometers,Is.EqualTo(end));
            Assert.That(ledger.GetActor(0).Projection.SectionIndex,Is.EqualTo(1));
            Assert.That(ledger.TryHandoffSections(new[]{0}),Is.False);
            Assert.That(ledger.ContextJson,Does.Contain("manual_selected_section_zero_credit_handoff_v1"));
        }

        [Test] public void InvalidSecondHandoffCannotPartiallyMoveFirstCursor()
        {
            var f=Create();var loop=f.visual.Get(f.route.GetSection(0).LoopId);
            var first=f.route.GetSection(0);
            long end=checked((first.Start.Millimeters+first.Length.Millimeters)*SkiState.NanometersPerMillimeter);
            var atEnd=Actor("end",f.body,At(loop.Sample(first.LoopEnd.Meters)),new SkiPlanarVelocity(0,0));
            var early=Actor("early",f.body,At(loop.Sample(10)),new SkiPlanarVelocity(0,0));
            var ledger=Ledger(f.route,f.map,f.sweep,f.progress,new[]{atEnd,early},new[]{end,Nano(10)});
            var original=ledger.GetActor(0);
            Assert.That(ledger.TryHandoffSections(new[]{0,1}),Is.False);
            Assert.That(ledger.GetCursor(0).SectionIndex,Is.Zero);
            Assert.That(ledger.GetActor(0).Projection.SectionIndex,Is.EqualTo(original.Projection.SectionIndex));
            Assert.Throws<ArgumentException>(()=>ledger.TryHandoffSections(new[]{0,0}));
            Assert.That(ledger.TryHandoffSections(new[]{0}),Is.True);
            Assert.That(ledger.GetCursor(1).SectionIndex,Is.Zero);
            Assert.That(ledger.Tick.Value,Is.Zero);
        }

        [Test] public void EntireClearRosterCommitsOnceWithoutGuessingRaceOdometer()
        {
            var f=Create();var loop=f.visual.Get(f.route.GetSection(0).LoopId);
            var a=loop.Sample(10);var b=loop.Sample(30);
            var actors=new[]{Actor("a",f.body,At(a),new SkiPlanarVelocity(3*a.ForwardX,3*a.ForwardZ)),
                Actor("b",f.body,At(b),new SkiPlanarVelocity(0,0))};
            var stations=new[]{Nano(10),Nano(30)};
            var ledger=Ledger(f.route,f.map,f.sweep,f.progress,actors,stations);
            var before=ledger.GetActor(0);
            Assert.That(Prepare(ledger,actors,Windows(actors,stations),new[]{Nano(20),Nano(40)},out var preview),Is.True);
            Assert.That(preview.Tick.Value,Is.EqualTo(1));
            Assert.That(ledger.Tick.Value,Is.Zero);
            Assert.That(ledger.GetActor(0).Pose,Is.EqualTo(before.Pose));
            for(int i=0;i<100;i++)Prepare(ledger,actors,Windows(actors,stations),new[]{Nano(20),Nano(40)},out _);
            var reusedWindows=Windows(actors,stations);var caps=new[]{Nano(20),Nano(40)};
            long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)
                if(!Prepare(ledger,actors,reusedWindows,caps,out _))Assert.Fail("Identical clear roster changed.");
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
            Assert.Throws<ArgumentException>(()=>ledger.Commit(preview));
            Assert.That(Prepare(ledger,actors,reusedWindows,caps,out preview),Is.True);
            ledger.Commit(preview);
            Assert.That(ledger.Tick.Value,Is.EqualTo(1));
            Assert.That(ledger.GetActor(0).Pose,Is.Not.EqualTo(before.Pose));
            Assert.That(ledger.GetCursor(0).CreditedStationNanometers,Is.GreaterThan(stations[0]));
            Assert.That(ledger.GetCursor(1).CreditedStationNanometers,Is.EqualTo(stations[1]));
            Assert.Throws<ArgumentException>(()=>ledger.Commit(preview));
            Assert.That(ledger.ContextJson,Does.Contain("manual_full_clear_world_roster_v1"));
        }

        [Test] public void PairContactOrRangeCapCannotPublishOneAthleteOrAdvanceTick()
        {
            var f=Create();var loop=f.visual.Get(f.route.GetSection(0).LoopId);
            var a=loop.Sample(10);var b=loop.Sample(11.6);
            var actors=new[]{Actor("a",f.body,At(a),new SkiPlanarVelocity(6*a.ForwardX,6*a.ForwardZ)),
                Actor("b",f.body,At(b),new SkiPlanarVelocity(0,0))};
            var stations=new[]{Nano(10),Nano(11.6)};
            var ledger=Ledger(f.route,f.map,f.sweep,f.progress,actors,stations);
            var first=ledger.GetActor(0);var second=ledger.GetActor(1);
            Assert.That(Prepare(ledger,actors,Windows(actors,stations),new[]{Nano(20),Nano(20)},out var blocked),Is.False);
            Assert.That(blocked.IsValid,Is.False);
            Assert.That(ledger.Tick.Value,Is.Zero);
            Assert.That(ledger.GetActor(0).Pose,Is.EqualTo(first.Pose));
            Assert.That(ledger.GetActor(1).Pose,Is.EqualTo(second.Pose));
            Assert.Throws<ArgumentException>(()=>ledger.Commit(blocked));

            var distant=loop.Sample(30);
            var clear=new[]{actors[0],Actor("b",f.body,At(distant),new SkiPlanarVelocity(0,0))};
            var safe=Ledger(f.route,f.map,f.sweep,f.progress,clear,new[]{Nano(10),Nano(30)});
            Assert.That(Prepare(safe,clear,Windows(clear,new[]{Nano(10),Nano(30)}),
                new[]{Nano(10),Nano(40)},out var capped),Is.False);
            Assert.That(capped.IsValid,Is.False);
            Assert.That(safe.Tick.Value,Is.Zero);
        }

        [Test] public void StaleWorldStartAndForeignRouteCannotEnterPreparedLedger()
        {
            var f=Create();var p=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var actor=Actor("a",f.body,At(p),new SkiPlanarVelocity(3*p.ForwardX,3*p.ForwardZ));
            var actors=new[]{actor};var stations=new[]{Nano(10)};
            var ledger=Ledger(f.route,f.map,f.sweep,f.progress,actors,stations);
            var wrong=Actor("a",f.body,new ManualSkiHeadingState(p.X+.1,p.Z,p.ForwardX),
                new SkiPlanarVelocity(3*p.ForwardX,3*p.ForwardZ));
            Assert.That(Prepare(ledger,new[]{wrong},Windows(actors,stations),new[]{Nano(20)},out _),Is.False);
            var different=Create();
            Assert.Throws<ArgumentException>(()=>new ManualWorldRosterLedger(f.route,different.map,f.sweep,f.progress,
                new[]{f.progress.Reconstitute(actor.Athlete,0,Nano(10),Nano(10))},
                new[]{actor.Trajectory.Sample(0)},new[]{actor.Trajectory.VelocityAt(0)},20));
        }

        [Test] public void RepeatedFullClearTicksKeepWorldHeadingAndRouteCreditDeterministic()
        {
            var f=Create();var p=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var athlete=new AthleteId("a");
            var initial=Actor("a",f.body,At(p),new SkiPlanarVelocity(3*p.ForwardX,3*p.ForwardZ));
            var left=Ledger(f.route,f.map,f.sweep,f.progress,new[]{initial},new[]{Nano(10)});
            var right=Ledger(f.route,f.map,f.sweep,f.progress,new[]{initial},new[]{Nano(10)});
            for(int tick=1;tick<=25;tick++)
            {
                var a=Actor("a",f.body,left.GetActor(0).Pose,left.GetActor(0).Velocity);
                var b=Actor("a",f.body,right.GetActor(0).Pose,right.GetActor(0).Velocity);
                var aw=new[]{new ManualRouteActorWindow(athlete,0,left.GetCursor(0).ObservedStationNanometers,2,false)};
                var bw=new[]{new ManualRouteActorWindow(athlete,0,right.GetCursor(0).ObservedStationNanometers,2,false)};
                Assert.That(Prepare(left,new[]{a},aw,new[]{Nano(50)},out var lp),Is.True);
                Assert.That(Prepare(right,new[]{b},bw,new[]{Nano(50)},out var rp),Is.True);
                left.Commit(lp);right.Commit(rp);
                Assert.That(left.GetActor(0).Pose,Is.EqualTo(right.GetActor(0).Pose));
                Assert.That(left.GetActor(0).Velocity,Is.EqualTo(right.GetActor(0).Velocity));
                Assert.That(left.GetCursor(0),Is.EqualTo(right.GetCursor(0)));
                Assert.That(left.GetActor(0).Pose.HeadingNanoradians,
                    Is.EqualTo(initial.Trajectory.Sample(0).HeadingNanoradians));
            }
            Assert.That(left.Tick.Value,Is.EqualTo(25));
            Assert.That(left.GetCursor(0).CreditedStationNanometers,Is.GreaterThan(Nano(10)));
        }

        [Test] public void SideSlipPaysPhysicalDistanceWithoutAwardingRouteStation()
        {
            var f=Create();var point=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var id=new AthleteId("sideways");
            var actor=Actor(id.Value,f.body,At(point),
                new SkiPlanarVelocity(2*point.ForwardZ,-2*point.ForwardX));
            long station=Nano(10);
            var ledger=new ManualWorldRosterLedger(f.route,f.map,f.sweep,f.progress,
                new[]{f.progress.Reconstitute(id,0,station,station)},
                new[]{actor.Trajectory.Sample(0)},new[]{actor.Trajectory.VelocityAt(0)},20,
                new[]{new SkiState(0,2)});
            var candidates=new[]{actor};
            var windows=new[]{new ManualRouteActorWindow(id,0,station,2,false)};
            Assert.That(ledger.TryPrepareWithOdometer(candidates,windows,new[]{Nano(20)},
                Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,
                new[]{0d},out var proposal),Is.True);
            Assert.That(ledger.GetPhysicalMotion(0).DistanceNanometers,Is.Zero);
            ledger.Commit(proposal);
            Assert.That(ledger.GetPhysicalMotion(0).DistanceNanometers,Is.EqualTo(Nano(.1)));
            Assert.That(ledger.GetCursor(0).CreditedStationNanometers,Is.EqualTo(station));
            Assert.That(ledger.GetActor(0).Pose.HeadingNanoradians,
                Is.EqualTo(actor.Trajectory.Sample(0).HeadingNanoradians));
            Assert.That(ledger.ContextJson,Does.Contain("manual_full_clear_world_odometer_roster_v2"));
        }

        [Test] public void RejectedWorldCandidateCannotAdvanceBoundPhysicalOdometer()
        {
            var f=Create();var point=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var id=new AthleteId("capped");
            var actor=Actor(id.Value,f.body,At(point),
                new SkiPlanarVelocity(3*point.ForwardX,3*point.ForwardZ));
            long station=Nano(10);
            var ledger=new ManualWorldRosterLedger(f.route,f.map,f.sweep,f.progress,
                new[]{f.progress.Reconstitute(id,0,station,station)},
                new[]{actor.Trajectory.Sample(0)},new[]{actor.Trajectory.VelocityAt(0)},20,
                new[]{new SkiState(Nano(5),3)});
            Assert.That(ledger.TryPrepareWithOdometer(new[]{actor},
                new[]{new ManualRouteActorWindow(id,0,station,2,false)},new[]{station},
                Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,
                new[]{0d},out var rejected),Is.False);
            Assert.That(rejected.IsValid,Is.False);
            Assert.That(ledger.Tick.Value,Is.Zero);
            Assert.That(ledger.GetPhysicalMotion(0).DistanceNanometers,Is.EqualTo(Nano(5)));
            Assert.Throws<InvalidOperationException>(()=>ledger.TryPrepare(new[]{actor},
                new[]{new ManualRouteActorWindow(id,0,station,2,false)},new[]{Nano(20)},
                Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,out _));
        }
    }
}

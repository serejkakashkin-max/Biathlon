using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class MassStartGridTests
    {
        public static CompetitionProfile Profile()=>CompetitionProfile.FromJson(PhysiologyContractTests.DefinitionJson("mass_start_standard_men_v1.json"));
        public static MassStartGridDefinition Definition()=>MassStartGridDefinition.FromJson(PhysiologyContractTests.DefinitionJson("mass_start_grid_provisional_v1.json"));
        public static AthleteId[] Roster(int count=30)
        {var ids=new AthleteId[count];for(int i=0;i<count;i++)ids[i]=new AthleteId("grid_"+i);return ids;}
        public static MassStartGrid Grid()=>new MassStartGrid(Profile(),Definition(),OccupancyTests.Definition(),Roster(),6,30);
        [Test] public void ThirtyStableSlotsHaveRealBodyClearanceAndNoCreditedRearRowDistance()
        {
            var roster=Roster();var body=OccupancyTests.Definition();var grid=new MassStartGrid(Profile(),Definition(),body,roster,6,30);
            roster[0]=new AthleteId("changed");Assert.That(grid.Get(0).AthleteId.Value,Is.EqualTo("grid_0"));
            Assert.That(grid.Count,Is.EqualTo(30));Assert.That(grid.Get(29).Row,Is.EqualTo(9));
            Assert.That(grid.Get(29).LongitudinalNanometers,Is.EqualTo(-27000000000L));
            Assert.That(grid.Get(0).LateralNanometers,Is.EqualTo(-2000000000L));Assert.That(grid.Get(2).LateralNanometers,Is.EqualTo(2000000000L));
            for(int i=0;i<grid.Count;i++)
            {
                var a=grid.Get(i);Assert.That(a.StartOrdinal,Is.EqualTo(i+1));Assert.That(a.LongitudinalNanometers,Is.LessThanOrEqualTo(0));
                Assert.That(Math.Abs(a.LateralNanometers/1e9)+body.EdgeMargin,Is.LessThanOrEqualTo(3));
                for(int j=0;j<i;j++)
                {var b=grid.Get(j);Assert.That(Math.Abs(a.LongitudinalNanometers-b.LongitudinalNanometers)/1e9>=body.LongitudinalSeparation ||
                    Math.Abs(a.LateralNanometers-b.LateralNanometers)/1e9>=body.LateralSeparation,Is.True);}
            }
            var restored=new MassStartGrid(Profile(),MassStartGridDefinition.FromJson(Definition().ToJson()),body,Roster(),6,30);
            Assert.That(restored.ContextJson,Is.EqualTo(grid.ContextJson));
            for(int i=0;i<grid.Count;i++)Assert.That(grid.Get(i),Is.EqualTo(restored.Get(i)));
        }
        [Test] public void WholeFieldStartsAtOneTickAndBufferFailureIsAtomic()
        {
            var grid=Grid();var log=new RaceEventLog(40);var scheduler=new SimultaneousStartScheduler(grid,20,new SimulationTick(2),log);
            var due=new ScheduledStart[30];var races=new CompetitionLifecycle[30];
            for(int i=0;i<30;i++)races[i]=new CompetitionLifecycle(Profile(),grid.Get(i).AthleteId,20,new RaceEventLog(10));
            Assert.That(scheduler.Step(new SimulationTick(1),due),Is.Zero);
            foreach(var race in races)race.Step(new SimulationTick(1));
            Assert.Catch(()=>scheduler.Step(new SimulationTick(2),new ScheduledStart[29]));
            Assert.That(scheduler.Tick.Value,Is.EqualTo(1));Assert.That(log.Count,Is.Zero);Assert.That(scheduler.EmittedCount,Is.Zero);
            Assert.That(scheduler.Step(new SimulationTick(2),due),Is.EqualTo(30));
            for(int i=0;i<30;i++)
            {
                Assert.That(due[i].AthleteId,Is.EqualTo(grid.Get(i).AthleteId));Assert.That(due[i].Tick.Value,Is.EqualTo(2));
                races[i].Step(new SimulationTick(2),CompetitionAction.Start);
                Assert.That(races[i].StartTick.Value,Is.EqualTo(2));Assert.That(races[i].Phase,Is.EqualTo(CompetitionPhase.Skiing));
                Assert.That(log[i].EventType,Is.EqualTo(RaceEventType.ScheduledStartDue));
            }
            Assert.That(log.Count,Is.EqualTo(30));Assert.That(scheduler.Step(new SimulationTick(3),due),Is.Zero);
            Assert.That(scheduler.EmittedCount,Is.EqualTo(30));
        }
        [Test] public void DuplicateOverlappingOversizedAndUnrepresentableLayoutsReject()
        {
            var body=OccupancyTests.Definition();var d=Definition();var p=Profile();var roster=Roster();
            Assert.Catch(()=>new MassStartGrid(p,d,body,roster,4,30));Assert.Catch(()=>new MassStartGrid(p,d,body,roster,6,20));
            Assert.Catch(()=>new MassStartGrid(p,d,body,Roster(31),6,40));Assert.Catch(()=>new MassStartGrid(p,d,body,Array.Empty<AthleteId>(),6,30));
            roster[1]=roster[0];Assert.Catch(()=>new MassStartGrid(p,d,body,roster,6,30));
            Assert.Catch(()=>new MassStartGrid(CompetitionProfileTests.TestProfile(),d,body,Roster(),6,30));
            foreach(var pair in new[]{("row_spacing_m","0.001"),("column_spacing_m","0.001")})
            {var overlap=MassStartGridDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(),pair.Item1,pair.Item2));Assert.Catch(()=>new MassStartGrid(p,overlap,body,Roster(),6,30));}
            foreach(var pair in new[]{("schema_version","2"),("columns","0"),("row_spacing_m","0"),("column_spacing_m","0.0001")})
                Assert.Catch(()=>MassStartGridDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(),pair.Item1,pair.Item2)));
        }
        [Test] public void RepeatedGunEventsAreExactAndWarmSchedulingAllocatesNothing()
        {
            var grid=Grid();var la=new RaceEventLog(40);var lb=new RaceEventLog(40);
            var a=new SimultaneousStartScheduler(grid,20,new SimulationTick(2),la);var b=new SimultaneousStartScheduler(Grid(),20,new SimulationTick(2),lb);
            var due=new ScheduledStart[30];a.Step(new SimulationTick(1),due);b.Step(new SimulationTick(1),due);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=2;i<1000;i++){a.Step(new SimulationTick(i),due);b.Step(new SimulationTick(i),due);}
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);Assert.That(a.ContextJson,Is.EqualTo(b.ContextJson));
            Assert.That(la.Count,Is.EqualTo(30));for(int i=0;i<30;i++)Assert.That(la[i],Is.EqualTo(lb[i]));
            Assert.Catch(()=>a.Step(new SimulationTick(1001),due));
            Assert.Catch(()=>new SimultaneousStartScheduler(grid,0,new SimulationTick(1),la));
            Assert.Catch(()=>new SimultaneousStartScheduler(grid,20,new SimulationTick(0),la));
            Assert.That(new SimultaneousStartScheduler(grid,20,new SimulationTick(3),la).ContextJson,Is.Not.EqualTo(a.ContextJson));
        }
        [Test] public void ThirtyGridBodiesLaunchForOneMinuteThroughTheExistingPackKernel()
        {
            var grid=Grid();var track=DraftingTests.Flat();var initial=grid.CreateLaunchPlacements(track.LoopId);
            var la=new RaceEventLog(5000);var lb=new RaceEventLog(5000);
            PackForceSession Create(RaceEventLog log)=>new PackForceSession(SkiForceTests.Definition(),DraftingTests.Definition(),OvertakeTests.Definition(),OccupancyTests.Definition(),20,initial,log);
            var a=Create(la);var b=Create(lb);var commands=new PackForceCommand[30];var old=new SkiPlacement[30];
            var occupancy=new OccupancySystem(OccupancyTests.Definition());
            long minimumTravel=long.MaxValue;ulong hash=14695981039346656037UL;
            for(int tick=1;tick<=1200;tick++)
            {
                for(int i=0;i<30;i++)
                {
                    old[i]=a.GetPlacement(i);
                    // First tick is the common gun, then every actor receives the same test power.
                    commands[i]=new PackForceCommand(new SimulationTick(tick),old[i].AthleteId,track,tick==1?0:350,initial[i].LateralMeters,6);
                }
                a.Step(commands);b.Step(commands);
                for(int i=0;i<30;i++)
                {
                    var next=a.GetPlacement(i);Assert.That(next,Is.EqualTo(b.GetPlacement(i)));
                    if(tick==1)Assert.That(next.Motion.DistanceNanometers,Is.EqualTo(initial[i].Motion.DistanceNanometers));
                    for(int j=0;j<i;j++)Assert.That(occupancy.QueryRelativeMove(old[i],next,old[j],a.GetPlacement(j)).Blocked,Is.False);
                    unchecked {hash=(hash^(ulong)next.Motion.DistanceNanometers)*1099511628211UL;}
                }
            }
            for(int i=0;i<30;i++)
            {
                long travel=a.GetPlacement(i).Motion.DistanceNanometers-initial[i].Motion.DistanceNanometers;
                minimumTravel=Math.Min(minimumTravel,travel);Assert.That(travel,Is.GreaterThan(100L*SkiState.NanometersPerMeter));
                Assert.That(initial[i].Motion.DistanceNanometers-grid.FrontReferenceNanometers,Is.EqualTo(grid.Get(i).LongitudinalNanometers));
            }
            Assert.That(la.Count,Is.EqualTo(lb.Count));for(int i=0;i<la.Count;i++)Assert.That(la[i],Is.EqualTo(lb[i]));
            TestContext.Out.WriteLine("grid_launch ticks=1200; min_travel_nm="+minimumTravel+"; trace="+hash.ToString("x16"));
        }
    }
}

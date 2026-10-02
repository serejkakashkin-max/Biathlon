using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalStartContinuationTests
    {
        private const long M=SkiState.NanometersPerMeter;
        private const string PolicyResource="Biathlon/Venues/production_venue_validation_v1";
        public static string ReadPolicy(string resource)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+resource+".json");
        public static StartApproachDefinition ApproachDefinition(PhysicalRangePackTests.Fixture f)
            =>StartApproachDefinition.FromJson("{\"schema_version\":1,\"id\":\"authored_start_v1\",\"track_id\":\""+f.Track.Id+"\",\"loop_id\":\"authored_start\",\"route_entry_loop_id\":\"main\"}");
        private static string Number(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
        private static string Knot(double d,double x,double z)=>"{\"distance_m\":"+Number(d)+",\"x\":"+Number(x)+",\"y\":0,\"z\":"+Number(z)+"}";
        public static string ContractJson(PhysicalRangePackTests.Fixture f)
        {
            double chord=1.963,radius=chord/(2*Math.Sin(Math.PI/64));
            var points=new string[19];points[0]=Knot(0,-1-radius,radius+7.592);points[1]=Knot(7.592,-1-radius,radius);
            for(int i=1;i<=16;i++)
            {double angle=Math.PI+i*Math.PI/32;points[i+1]=Knot(7.592+i*chord,i==16?-1:-1+radius*Math.Cos(angle),i==16?0:radius+radius*Math.Sin(angle));}
            points[18]=Knot(40,0,0);
            return "{\"schema_version\":1,\"id\":\"curved_start_continuation_v1\",\"track_id\":\""+f.Track.Id+
                "\",\"start_approach_id\":\"authored_start_v1\",\"connection_id\":\"authored_start_to_main\",\"occupancy_definition_id\":\"occupancy_provisional_v1\",\"validation_policy_resource\":\""+PolicyResource+
                "\",\"corridors\":[{\"loop_id\":\"authored_start\",\"knots\":["+string.Join(",",points)+"]},{\"loop_id\":\"main\",\"knots\":["+Knot(0,0,0)+","+Knot(10,10,0)+"]}]}";
        }
        public static PhysicalRangePackTests.Fixture Fixture(bool threeFront=true)
        {
            var f=AuthoredStartApproachTests.Fixture();
            string junctions=f.Junctions.ToJson();junctions=junctions.Replace("\"guard_before_m\":1,\"id\":\"authored_start_end\"","\"guard_before_m\":18,\"id\":\"authored_start_end\"");
            // Canonical member ordering is serializer-owned: target the complete source/main port object.
            junctions=Regex.Replace(junctions,"\\{[^{}]*\"anchor\":\\{[^{}]*\"loop_id\":\"authored_start\"[^{}]*\\}[^{}]*\\}",m=>Regex.Replace(m.Value,"(\"guard_before_m\":)1(?=[,}])","${1}18"));
            junctions=Regex.Replace(junctions,"\\{[^{}]*\"anchor\":\\{[^{}]*\"loop_id\":\"main\"[^{}]*\\}[^{}]*\"id\":\"authored_start_main\"[^{}]*\\}",m=>Regex.Replace(Regex.Replace(m.Value,"(\"guard_before_m\":)1(?=[,}])","${1}18"),"(\"guard_after_m\":)1(?=[,}])","${1}2"));
            f.Junctions=PhysicalJunctionDefinition.FromJson(junctions,f.Track);
            f.Connections=PhysicalConnectionDefinition.FromJson(f.Connections.ToJson(),f.Junctions);
            if(threeFront)
            {
                string Lane(string id,string mat,int fork,double stop)=>"{\"id\":\""+id+"\",\"mat_anchor\":{\"loop_id\":\""+mat+"\",\"distance_m\":"+Number(stop)+"},\"slices\":[{\"loop_id\":\"main\",\"start_m\":0,\"end_m\":"+fork+"},{\"loop_id\":\""+mat+"\",\"start_m\":0,\"end_m\":40},{\"loop_id\":\"exit_aisle\",\"start_m\":"+fork+",\"end_m\":60}]}";
                string paths="{\"schema_version\":1,\"id\":\"start_test_paths\",\"track_id\":\""+f.Track.Id+"\",\"main_loop_id\":\"main\",\"entry_distance_m\":0,\"exit_distance_m\":100,\"lanes\":["+
                    Lane("lane_1","mat_1",10,18)+","+Lane("lane_2","mat_2",30,18)+","+Lane("lane_3","mat_1",10,18.1)+"]}";
                f.Layout=new PhysicalRangeFieldLayout(f.Map,f.Track,f.Route,PhysicalRangePathDefinition.FromJson(paths,f.Track,f.Junctions,f.Connections));
                var old=f.Entrants[0];f.Entrants=new SprintEntrant[3];var ids=new AthleteId[3];
                for(int i=0;i<3;i++){ids[i]=new AthleteId("continuation_"+i);f.Entrants[i]=new SprintEntrant(TimeTrialAthlete.FromJson(old.SkiAthlete.ToJson().Replace(old.SkiAthlete.Id,ids[i].Value)),ShootingAthleteDefinition.FromJson(old.ShootingAthlete.ToJson().Replace(old.ShootingAthlete.Id,ids[i].Value)),"ski_neutral");}
                f.Grid=new MassStartGrid(f.Profile,MassStartGridTests.Definition(),OccupancyTests.Definition(),ids,6,40);
            }
            else f.Layout=new PhysicalRangeFieldLayout(f.Map,f.Track,f.Route,PhysicalRangePathDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("paths"),f.Track,f.Junctions,f.Connections));
            return f;
        }
        public static PhysicalStartContinuationDefinition Definition(PhysicalRangePackTests.Fixture f,string json=null)
            =>PhysicalStartContinuationDefinition.FromJson(json??ContractJson(f),f.Track,ApproachDefinition(f),f.Junctions,f.Connections,OccupancyTests.Definition(),ReadPolicy);
        public static PhysicalStartContinuationMap Map(PhysicalRangePackTests.Fixture f,PhysicalStartContinuationDefinition definition=null)
            =>new PhysicalStartContinuationMap(f.Map,f.Track,f.Route,f.Approach,f.Junctions,f.Connections,OccupancyTests.Definition(),definition??Definition(f));
        public static PackSprintFieldSession Create(PhysicalRangePackTests.Fixture f,RaceEventLog log,PhysicalStartContinuationDefinition definition)
            =>new PackSprintFieldSession(new SimulationConfig(1,"pack_path_sim",20,123),f.Profile,new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson),ShootingSessionTests.Model(),f.Track,f.Route,
                RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed","packed")),SprintResultsTests.Timing(),f.Entrants,new SimulationTick(1),"pack_path_result",OccupancyTests.Definition(),
                DraftingTests.Definition(),OvertakeTests.Definition(),f.Junctions,f.Connections,log,massGrid:f.Grid,startApproach:f.Approach,rangeLayout:f.Layout,authoredStartApproach:true,startContinuation:definition);
        private static void Fill(PackSprintFieldSession field,SprintAthleteInput[] commands)
        {var tick=field.Tick.Next();for(int i=0;i<commands.Length;i++){var id=field.GetAthlete(i).AthleteId;commands[i]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,800),1,1,0),new WeaponAimCommand(tick,id,0,0),true);}}
        private static PhysicalOccupancySweep Sweep(PhysicalRangePackTests.Fixture f,string id,string loop,double from,double travel,double lateral,double endLateral)
            =>new PhysicalOccupancySweep(new AthleteId(id),f.Map.MapLoop(loop,(long)(from*M)),(long)(travel*M),lateral,endLateral);
        private static Dictionary<string,string> VenueResources()
        {
            var f=Fixture(false);var definition=Definition(f);var resources=VenuePathAuthoringTests.Resources();
            resources["manifest"]=resources["manifest"].Replace("\"schema_version\": 2","\"schema_version\": 2,\"start_approach_resource\":\"start\",\"start_continuation_resource\":\"continuation\"")
                .Replace("\"distance_m\": 60","\"distance_m\": 100");
            resources["track"]=f.Track.ToJson();resources["paths"]=PhysicalRangePathDefinitionTests.Fixture("paths");
            resources["junctions"]=f.Junctions.ToJson();resources["connections"]=f.Connections.ToJson();resources["start"]=ApproachDefinition(f).ToJson();resources["continuation"]=definition.ToJson();
            resources[PolicyResource]=ReadPolicy(PolicyResource);
            var loops=new string[f.Track.LoopCount];for(int i=0;i<loops.Length;i++)
            {
                var loop=f.Track.GetLoop(i);double length=loop.Length.Meters;
                if(loop.Id==definition.Source.LoopId)
                {var knots=new double[definition.Source.Count][];for(int k=0;k<knots.Length;k++){var p=definition.Source.Get(k);knots[k]=new[]{p.Distance,p.X,p.Y,p.Z};}loops[i]=VenuePathAuthoringTests.Visual(loop.Id,knots);}
                else if(loop.Id=="main")loops[i]=VenuePathAuthoringTests.Visual(loop.Id,new[]{new[]{0d,0,0,0},new[]{10d,10,0,0},new[]{75d,75,0,0},new[]{150d,75,0,-75},new[]{225d,0,0,-75},new[]{300d,0,0,0}});
                else
                {double x=-1000-i*1000;loops[i]=VenuePathAuthoringTests.Visual(loop.Id,loop.Closed?new[]{new[]{0d,x,0,-1000},new[]{length/4,x+length/4,0,-1000},new[]{length/2,x+length/4,0,-1000-length/4},new[]{length*3/4,x,0,-1000-length/4},new[]{length,x,0,-1000}}:new[]{new[]{0d,x,0,-1000},new[]{length,x+length,0,-1000}});}
            }
            resources["visual"]="{\"schema_version\":1,\"id\":\"start_contract_visual_v1\",\"track_id\":\""+f.Track.Id+"\",\"sample_spacing_m\":1,\"loops\":["+string.Join(",",loops)+"]}";
            return resources;
        }

        [Test] public void CurvedThirtyBodyGridUsesWorldClearanceWithActualEighteenMetreGuards()
        {
            var f=Fixture();var definition=Definition(f);var map=Map(f,definition);var ids=MassStartGridTests.Roster();var grid=new MassStartGrid(f.Profile,MassStartGridTests.Definition(),OccupancyTests.Definition(),ids,6,40);
            Assert.That(f.Junctions.Get(definition.JunctionIndex).GetPort(0).GuardBefore.Meters,Is.EqualTo(18));
            int pairs=0;for(int i=0;i<30;i++)for(int j=0;j<i;j++)
            {
                var a=grid.Get(i);var b=grid.Get(j);
                Assert.That(map.Conflicts(Sweep(f,a.AthleteId.Value,"authored_start",40+a.LongitudinalNanometers/(double)M,0,a.LateralNanometers/(double)M,a.LateralNanometers/(double)M),
                    Sweep(f,b.AthleteId.Value,"authored_start",40+b.LongitudinalNanometers/(double)M,0,b.LateralNanometers/(double)M,b.LateralNanometers/(double)M)),Is.False,"World grid pair "+i+","+j);pairs++;
            }
            Assert.That(pairs,Is.EqualTo(435));
        }
        [Test] public void CompleteSweptBoundsRejectLateralCrossingAndInteriorCurvedContact()
        {
            var f=Fixture();var map=Map(f);
            Assert.That(map.Conflicts(Sweep(f,"a","authored_start",30,1,-1,1),Sweep(f,"b","authored_start",30,1,1,-1)),Is.True);
            Assert.That(map.Conflicts(Sweep(f,"a","authored_start",30,1,-1,-1),Sweep(f,"b","authored_start",30,1,1,1)),Is.False);
            Assert.That(map.Conflicts(Sweep(f,"a","authored_start",37,3,0,0),Sweep(f,"b","main",1.25,0,0,0)),Is.True,"Paid motion through seam neighbourhood cannot tunnel into outgoing body.");
            Assert.That(map.Conflicts(Sweep(f,"a","authored_start",37,3,-2,-2),Sweep(f,"b","main",0,2,2,2)),Is.False);
        }
        [TestCase(19.37)][TestCase(25.259)][TestCase(35.074)]
        public void SmallKnotTurnsKeepSeparatedBodiesClearAcrossTheCompleteRotation(double knot)
        {
            var f=Fixture();var map=Map(f);double from=knot-.0005;
            var a=Sweep(f,"a","authored_start",from,.001,0,0);
            var stationary=Sweep(f,"b","authored_start",from,0,1.1,1.1);
            var moving=Sweep(f,"b","authored_start",from,.001,1.1,1.1);
            Assert.That(map.Conflicts(a,stationary),Is.False,"A small authored turn must not expand the complete body into a full circle.");
            Assert.That(map.Conflicts(a,moving),Is.False,"Two separated bodies must retain clearance during their complete knot rotations.");
            Assert.That(map.Conflicts(a,Sweep(f,"close","authored_start",from,.001,.7,.7)),Is.True,"Configured body clearance and outward margin remain blocking.");
        }
        [TestCase(-2d)][TestCase(0d)][TestCase(2d)]
        public void KnotProjectionContainsRotatingCornersAndTheirActualLateralCentre(double lateral)
        {
            var f=Fixture();var definition=Definition(f);var map=Map(f,definition);
            var source=definition.Source;var a=source.Get(14);var b=source.Get(15);var c=source.Get(16);
            double before=Math.Atan2(b.Z-a.Z,b.X-a.X),after=Math.Atan2(c.Z-b.Z,c.X-b.X);
            double turn=Math.Atan2(Math.Sin(after-before),Math.Cos(after-before));
            var body=OccupancyTests.Definition();double halfLength=body.LongitudinalSeparation*.5,halfWidth=body.LateralSeparation*.5;
            var sweep=Sweep(f,"rotating","authored_start",b.Distance-.001,.002,lateral,lateral);
            var bounds=typeof(PhysicalStartContinuationMap).GetMethod("Bounds",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Assert.That(bounds,Is.Not.Null);double violation=0;
            for(int axis=0;axis<72;axis++)
            {
                double angle=axis*Math.PI/36,x=Math.Cos(angle),z=Math.Sin(angle);
                object[] args={sweep,x,z,0d,0d};bounds.Invoke(map,args);double low=(double)args[3],high=(double)args[4];
                for(int sample=0;sample<=256;sample++)
                {
                    double heading=before+turn*sample/256,fx=Math.Cos(heading),fz=Math.Sin(heading);
                    for(int along=-1;along<=1;along+=2)for(int across=-1;across<=1;across+=2)
                    {
                        double normal=lateral+across*halfWidth;
                        double projection=(b.X+along*halfLength*fx-normal*fz)*x+(b.Z+along*halfLength*fz+normal*fx)*z;
                        violation=Math.Max(violation,Math.Max(low-projection,projection-high));
                    }
                }
            }
            Assert.That(violation,Is.LessThanOrEqualTo(1e-10),"Every sampled corner and offset normal must remain inside the full rotation proof.");
            Assert.That(map.ContextJson,Does.Contain("shortest_knot_rotation_corner_support_v2"));
        }
        [Test] public void TerminalTrafficOtherBranchesAndForeignPointsCannotUseContinuationProof()
        {
            var f=Fixture();var map=Map(f);
            Assert.That(map.Contains(Sweep(f,"a","main",299,1,0,0)),Is.False);
            Assert.That(map.Contains(f.Map.MapLoop("mat_1",0)),Is.False);
            Assert.Catch(()=>map.Conflicts(Sweep(f,"a","main",299,1,0,0),Sweep(f,"b","authored_start",39,1,0,0)));
            var foreign=Fixture();Assert.Catch(()=>map.Contains(foreign.Map.MapLoop("main",0)));
        }
        [Test] public void ZeroForwardTravelStillSweepsTheCompletePaidLateralIntervalAcrossTheSeam()
        {
            var f=Fixture();var map=Map(f);
            Assert.That(map.Conflicts(Sweep(f,"a","authored_start",40,0,-2,2),Sweep(f,"b","main",0,0,0,0)),Is.True);
            Assert.That(map.Conflicts(Sweep(f,"a","authored_start",30,0,-2,2),Sweep(f,"b","authored_start",30,0,0,0)),Is.True);
            Assert.That(map.Conflicts(Sweep(f,"a","authored_start",30,0,-2,-1),Sweep(f,"b","authored_start",30,0,1,1)),Is.False);
            // Exact knot has both edge tangents and a rotation envelope, including the final lateral line.
            Assert.That(map.Conflicts(Sweep(f,"a","authored_start",39,0,-2,2),Sweep(f,"b","authored_start",39,0,0,0)),Is.True);
        }
        [TestCase(300d)][TestCase(281.5d)]
        public void OversizedOutgoingPrefixCannotIncludeClosedIncomingTerminalGuard(double length)
        {var f=Fixture();Assert.Catch(()=>Definition(f,ContractJson(f).Replace(Knot(10,10,0),Knot(length,length,0))));}
        [Test] public void KnotAndStationaryLateralProofIsSymmetricForEveryCommandOrder()
        {
            var f=Fixture();var map=Map(f);var positions=new[]{30d,39d,40d};var lines=new[]{-2d,0,2d};
            foreach(double at in positions)foreach(double from in lines)foreach(double to in lines)
            {
                var a=Sweep(f,"a","authored_start",at,0,from,to);var b=Sweep(f,"b",at==40?"main":"authored_start",at==40?0:at,0,0,0);
                Assert.That(map.Conflicts(a,b),Is.EqualTo(map.Conflicts(b,a)),"At "+at+" lateral "+from+" → "+to);
            }
        }
        [Test] public void ThreeFrontBodiesOwnIncomingEndpointUntilPaidHandoffAndKeepTheirSeamTails()
        {
            var f=Fixture();var log=new RaceEventLog(1000);var field=Create(f,log,Definition(f));var commands=new SprintAthleteInput[3];var lines=new[]{-2d,0,2d};
            for(int i=0;i<3;i++){Assert.That(field.GetPhysicalPoint(i).LoopId,Is.EqualTo("authored_start"));Assert.That(field.GetPhysicalPoint(i).LocalNanometers,Is.EqualTo(40*M));}
            Fill(field,commands);field.Step(commands,lines);
            for(int i=0;i<3;i++){Assert.That(field.GetPhysicalPoint(i).LoopId,Is.EqualTo("authored_start"));Assert.That(field.GetAthlete(i).Motion.DistanceNanometers,Is.Zero);}
            Fill(field,commands);field.Step(commands,lines);
            for(int i=0;i<3;i++){Assert.That(field.GetPhysicalPoint(i).LoopId,Is.EqualTo("main"));Assert.That(field.GetAthlete(i).Motion.DistanceNanometers,Is.GreaterThan(0));Assert.That(field.GetAthlete(i).RouteNanometers,Is.EqualTo(field.GetAthlete(i).Motion.DistanceNanometers));}
            int seams=0;for(int i=0;i<log.Count;i++)if(log[i].EventType==RaceEventType.PhysicalSeamCrossed)seams++;Assert.That(seams,Is.EqualTo(3));
            // Requested lateral convergence cannot move the still-straddling footprints.
            Fill(field,commands);field.Step(commands,new double[3]);
            Assert.That(field.GetLateral(0),Is.EqualTo(-2));Assert.That(field.GetLateral(2),Is.EqualTo(2));MassStartFieldTests.AssertClear(field);
        }
        [Test] public void ContractRoundTripAndNullPolicyPreserveIndependentContextFamilies()
        {
            var f=Fixture(false);var definition=Definition(f);Assert.That(Definition(f,definition.ToJson()).ToJson(),Is.EqualTo(definition.ToJson()));
            Assert.Catch(()=>AuthoredStartApproachTests.Create(f,new RaceEventLog(1000)),"Without the opt-in proof the real broad guard remains exclusive.");
            var plain=AuthoredStartApproachTests.Create(AuthoredStartApproachTests.Fixture(),new RaceEventLog(1000));var opt=Create(f,new RaceEventLog(1000),definition);
            Assert.That(plain.ContextJson,Does.Not.Contain("start_continuation_world"));Assert.That(opt.ContextJson,Does.Contain("start_continuation_world_swept_rectangles_v1"));
            var legacy=PhysicalRangePackTests.CreateFixture();var a=PhysicalRangePackTests.CreatePack(legacy,new RaceEventLog(1000));var b=PhysicalRangePackTests.CreatePack(legacy,new RaceEventLog(1000));
            Assert.That(a.ContextJson,Is.EqualTo(b.ContextJson));Assert.That(a.UsesSharedStartContinuation,Is.False);
        }
        [TestCase("schema_version","2")]
        [TestCase("track_id","\"foreign\"")]
        [TestCase("start_approach_id","\"foreign\"")]
        [TestCase("connection_id","\"foreign\"")]
        [TestCase("occupancy_definition_id","\"foreign\"")]
        public void MalformedContractRejectsCold(string field,string value)
        {var f=Fixture();string json=Regex.Replace(ContractJson(f),"(\""+field+"\":)([^,}]+)","${1}"+value);Assert.Catch(()=>Definition(f,json));}
        [Test] public void GeometryShortcutsGapsAndLateralSeamRotationRejectBeforeAnyFieldEvents()
        {
            var f=Fixture();string json=ContractJson(f);
            Assert.Catch(()=>Definition(f,json.Replace(Knot(40,0,0),Knot(40,.001,0))));
            Assert.Catch(()=>Definition(f,json.Replace(Knot(10,10,0),Knot(10,0,10))));
            Assert.Catch(()=>Definition(f,json.Replace(Knot(10,10,0),Knot(.1,.1,0))));
            Assert.Catch(()=>Definition(f,json.Replace(Knot(10,10,0),Knot(10,9,0))));
            var definition=Definition(f);var log=new RaceEventLog(1000);var differentBody=OccupancyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(OccupancyTests.Definition().ToJson(),"body_width_m","0.61"));
            Assert.Catch(()=>new PhysicalStartContinuationMap(f.Map,f.Track,f.Route,f.Approach,f.Junctions,f.Connections,differentBody,definition));Assert.That(log.Count,Is.Zero);
        }
        [Test] public void ExtraIncidentBranchAndForeignApproachIdentityCannotUseSharedStartPolicy()
        {
            var f=Fixture();var definition=Definition(f);string sourcePort=VenuePathAuthoringTests.Port("extra_start_crossing","penalty",0,1,1);
            string junctions=Regex.Replace(f.Junctions.ToJson(),"(\"id\":\"authored_start_join\",\"ports\":\\[)","${1}"+sourcePort+",");
            var extra=PhysicalJunctionDefinition.FromJson(junctions,f.Track);var connections=PhysicalConnectionDefinition.FromJson(f.Connections.ToJson(),extra);
            Assert.Catch(()=>PhysicalStartContinuationDefinition.FromJson(definition.ToJson(),f.Track,ApproachDefinition(f),extra,connections,OccupancyTests.Definition(),ReadPolicy));
            var foreignApproach=new StartApproachMap(f.Track,f.Route,StartApproachDefinition.FromJson(ApproachDefinition(f).ToJson().Replace("authored_start_v1","other_approach_v1")));
            Assert.Catch(()=>new PhysicalStartContinuationMap(f.Map,f.Track,f.Route,foreignApproach,f.Junctions,f.Connections,OccupancyTests.Definition(),definition));
        }
        [Test] public void OptionalVenueBindingKeepsV1CanonicalAndRejectsMissingDependencyOrUnsafeResource()
        {
            var resources=VenueResources();var plan=VenuePathAuthoringTests.Plan(resources);Assert.That(plan.StartContinuation,Is.Not.Null);
            Assert.That(plan.StartContinuation.MatchesPresentation(plan.Visual),Is.True);
            Assert.That(plan.StartContinuation.TrackSnapshotJson,Is.EqualTo(plan.Track.ToJson()));
            Assert.That(VenueValidatorTests.Plan().Manifest.ToJson(),Does.Not.Contain("start_continuation_resource"));
            Assert.Catch(()=>VenueManifest.FromJson(resources["manifest"].Replace("\"start_approach_resource\":\"start\",","")));
            Assert.Catch(()=>VenueManifest.FromJson(resources["manifest"].Replace("\"continuation\"","\"../continuation\"")));
            Assert.Catch(()=>VenueManifest.FromJson(VenueValidatorTests.Plan().Manifest.ToJson().Replace("\"schema_version\":1","\"schema_version\":1,\"start_continuation_resource\":\"continuation\"")));
        }
        [Test] public void RenderingKnotShortcutIsReportedAgainstUnchangedAuthoritativeWorld()
        {
            var resources=VenueResources();var original=VenuePathAuthoringTests.Plan(resources);string authority=original.StartContinuation.ToJson();
            resources["visual"]=resources["visual"].Replace("\"distance_m\":10,\"x\":10,\"y\":0,\"z\":0", "\"distance_m\":5,\"x\":5,\"y\":0,\"z\":0.2},{\"distance_m\":10,\"x\":10,\"y\":0,\"z\":0");
            var changed=VenuePathAuthoringTests.Plan(resources);Assert.That(changed.StartContinuation.ToJson(),Is.EqualTo(authority));Assert.That(changed.StartContinuation.MatchesPresentation(changed.Visual),Is.False);
            var report=VenueValidator.Validate(changed,VenueValidatorTests.Policy(),_=>true);bool issue=false;for(int i=0;i<report.Count;i++)issue|=report.Get(i).Code=="start_continuation.presentation_mismatch";
            Assert.That(issue,Is.True,report.ToJson());
        }
        [Test] public void ShortForeignPresentationWithSameTrackIdReportsMismatchInsteadOfThrowing()
        {
            var f=Fixture();string shortened=Regex.Replace(f.Track.ToJson(),"\\{\"closed\":false,\"id\":\"authored_start\",\"kind\":\"race\",\"segments\":\\[[\\s\\S]*?\\]\\}",VenuePathAuthoringTests.Loop("authored_start",false,6,20));
            var foreignTrack=TrackDefinition.FromJson(shortened);
            string visual="{\"schema_version\":1,\"id\":\"short_foreign_visual\",\"track_id\":\""+f.Track.Id+"\",\"sample_spacing_m\":1,\"loops\":["+VenuePathAuthoringTests.Visual("authored_start",new[]{new[]{0d,0,0,0},new[]{20d,20,0,0}})+"]}";
            var foreignVisual=TrackVisualDefinition.FromJson(visual,foreignTrack);Assert.That(Definition(f).MatchesPresentation(foreignVisual),Is.False);
        }
        [Test] public void AuthoritativeKnotDistancesRejectSubMillimetresAndRoundedDuplicateCoordinates()
        {
            var f=Fixture();double radius=1.963/(2*Math.Sin(Math.PI/64));string json=ContractJson(f);
            Assert.Catch(()=>Definition(f,json.Replace(Knot(7.592,-1-radius,radius),Knot(7.59201,-1-radius,radius))),"A whole-mm simulation coordinate cannot silently become a rendering-only decimal.");
            // TrackDistance accepts conversion error <=1nm; the converted integer ordering must
            // still reject this nonzero double coordinate that rounds back onto the first knot.
            Assert.Catch(()=>Definition(f,json.Replace(Knot(7.592,-1-radius,radius),Knot(.0000000005,-1-radius,radius+7.592-.0000000005))));
        }
        [Test] public void ZeroTolerancePolicyStillReservesScaledWorldRoundingAndArithmetic()
        {
            var f=Fixture();string json=ContractJson(f).Replace(Knot(10,10,0),Knot(.001,1.001,0)+","+Knot(10,10,0));
            string policy=PhysiologyContractTests.ReplaceNumber(ReadPolicy(PolicyResource),"seam_tolerance_m","0");
            policy=PhysiologyContractTests.ReplaceNumber(policy,"distance_abs_tolerance_m","1");
            var definition=PhysicalStartContinuationDefinition.FromJson(json,f.Track,ApproachDefinition(f),f.Junctions,f.Connections,
                OccupancyTests.Definition(),resource=>policy);var map=Map(f,definition);
            // One logical millimetre spans1.001worldmetres: a <1 logical-nm slice floor error
            // therefore needs >1.001um outward reserve even though policy seam tolerance is0.
            Assert.That(map.ProjectionMarginMeters,Is.GreaterThan(1001d/M));
            Assert.That(map.ContextJson,Does.Contain("outward_scaled_nm_and_ieee_reserve_v1"));
            Assert.That(map.Conflicts(Sweep(f,"a","authored_start",40,0,-2,2),Sweep(f,"b","main",.001,0,0,0)),Is.True);
        }
        [Test] public void PersistedActualCurvedStartReplayRestoresPaidTravelTailLockAndExactEvents()
        {
            var f=Fixture(false);var events=new RaceEventLog(20000);var source=Create(f,events,Definition(f));var commands=new SprintAthleteInput[2];var lines=new double[2];var tape=new PackSprintRaceCommandTape("continued_start_tape",source,2);
            while(!source.IsComplete&&source.Tick.Value<80000){Fill(source,commands);tape.StepAndRecord(source,commands,lines);}
            Assert.That(source.IsComplete,Is.True);var restored=PackSprintRaceCommandTape.FromJson(tape.ToJson());var replayFixture=Fixture(false);var replayEvents=new RaceEventLog(20000);var target=Create(replayFixture,replayEvents,Definition(replayFixture));var replay=restored.CreateReplay(target);
            while(replay.HasNext)replay.Step();Assert.That(target.MassStartResult.ToJson(),Is.EqualTo(source.MassStartResult.ToJson()));Assert.That(replayEvents.Count,Is.EqualTo(events.Count));
            for(int i=0;i<events.Count;i++)Assert.That(replayEvents[i],Is.EqualTo(events[i]));
            Assert.That(target.GetAthlete(1).Motion.DistanceNanometers,Is.EqualTo(source.GetAthlete(1).Motion.DistanceNanometers));
            Assert.Catch(()=>restored.CreateReplay(AuthoredStartApproachTests.Create(Fixture(false),new RaceEventLog(20000))));
        }
        [Test] public void WarmCurvedWorldProofAndSharedPackStepsAllocateNothing()
        {
            var f=Fixture();var map=Map(f);var a=Sweep(f,"a","authored_start",30,.1,-1,-1);var b=Sweep(f,"b","authored_start",30,.1,1,1);map.Conflicts(a,b);
            long before=GC.GetAllocatedBytesForCurrentThread();int clear=0;for(int i=0;i<1000;i++)if(!map.Conflicts(a,b))clear++;long bytes=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(clear,Is.EqualTo(1000));Assert.That(bytes,Is.Zero);
            var field=Create(f,new RaceEventLog(1000),Definition(f));var commands=new SprintAthleteInput[3];var lines=new[]{-2d,0,2d};for(int i=0;i<30;i++){Fill(field,commands);field.Step(commands,lines);}
            before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10;i++){Fill(field,commands);field.Step(commands,lines);}bytes=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(bytes,Is.Zero);
        }
    }
}

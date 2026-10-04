using System;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ProneMatSupportTests
    {
        private static string Root()
        {
            foreach(string start in new[]{TestContext.CurrentContext.TestDirectory,Environment.CurrentDirectory})
            {
                var dir=new DirectoryInfo(start);
                while(dir!=null)
                {
                    string permanent=Path.Combine(dir.FullName,"tests/fixtures/venues/shared_apron_support_v1");
                    if(File.Exists(Path.Combine(permanent,"candidate/apron.json")))return permanent;
                    dir=dir.Parent;
                }
            }
            throw new FileNotFoundException("Prone support fixture is required.");
        }
        private static string Read(string id)=>File.ReadAllText(Path.Combine(Root(),"candidate/resources",id+".json"));
        private static PhysicalRangeMatAdmissionMap Gate(out VenueRangeMatDefinition mat)=>Gate(out mat,out _);
        private static PhysicalRangeMatAdmissionMap Gate(out VenueRangeMatDefinition mat,out double forwardPerLine)
        {
            const string v="Biathlon/Venues/woodland_basin_v1/";
            var track=TrackDefinition.FromJson(Read(v+"track"));var visual=TrackVisualDefinition.FromJson(Read(v+"visual"),track);
            var profile=CompetitionProfile.FromJson(Read(v+"mass_start_profile"));
            var junctions=PhysicalJunctionDefinition.FromJson(Read(v+"junctions"),track);
            var connections=PhysicalConnectionDefinition.FromJson(Read(v+"connections"),junctions);
            var paths=PhysicalRangePathDefinition.FromJson(Read(v+"paths"),track,junctions,connections);
            var route=new RaceRoute(track,RaceRoutePlan.FromJson(Read(v+"mass_start_route")),profile.ToRouteRequirements(),junctions,connections);
            var layout=new PhysicalRangeFieldLayout(new PhysicalTrackMap(track,route),track,route,paths);
            var anchor=paths.GetLane(0).MatAnchor;var stop=visual.Get(anchor.LoopId).Sample(anchor.Distance.Meters);
            forwardPerLine=-stop.ForwardX;
            mat=VenueManifest.FromJson(Read(v+"manifest")).Range.MatDefinition;
            return new PhysicalRangeMatAdmissionMap(track,route,profile,layout,visual,OccupancyTests.Definition(),mat,0,1,.05);
        }
        [Test]
        public void ProneSupportIsAnAdditionalSignedPredicateAndStandingKeepsItsBodyBounds()
        {
            var gate=Gate(out var mat,out double forwardPerLine);var body=OccupancyTests.Definition();mat.Validate(body);
            Assert.That(body.BodyLength,Is.EqualTo(1.4));Assert.That(body.BodyWidth,Is.EqualTo(.6));
            for(int path=0;path<30;path++)
            {
                Assert.That(gate.ContainsBody(path,-.30)&&gate.ContainsBody(path,.12),Is.True);
                Assert.That(gate.ContainsBodyAndSupports(path,0,-.159/forwardPerLine),Is.True);
                Assert.That(gate.ContainsBodyAndSupports(path,0,.039/forwardPerLine),Is.True);
                Assert.That(gate.ContainsBodyAndSupports(path,0,-.161/forwardPerLine),Is.False);
                Assert.That(gate.ContainsBodyAndSupports(path,0,.041/forwardPerLine),Is.False);
                // Mass Start bouts 0/1 are prone; bout 2 is standing.
                Assert.That(gate.ContainsBodyAndSupports(path,2,-.419/forwardPerLine),Is.True);
                Assert.That(gate.ContainsBodyAndSupports(path,2,.179/forwardPerLine),Is.True);
                Assert.That(gate.ContainsBodyAndSupports(path,2,-.421/forwardPerLine),Is.False);
                Assert.That(gate.ContainsBodyAndSupports(path,2,.181/forwardPerLine),Is.False);
            }
            Assert.That(gate.ContextJson,Does.Contain("optional_prone_torso_elbow_boot_support_plus_unchanged_body"));
        }
        [TestCase("schema_version","2")]
        [TestCase("lateral_min_m","0.1")]
        [TestCase("lateral_max_m","-0.1")]
        [TestCase("forward_min_m","0.1")]
        [TestCase("forward_max_m","-0.1")]
        [TestCase("clearance_m","-0.001")]
        [TestCase("clearance_m","0.101")]
        [TestCase("forward_min_m","-2")]
        [TestCase("forward_max_m","2")]
        public void MalformedOrUnfittableSupportCannotOptIn(string member,string value)
        {
            Gate(out var mat);string original=mat.ToJson();
            int split=original.IndexOf("\"prone_support\":",StringComparison.Ordinal);
            Assert.That(split,Is.GreaterThanOrEqualTo(0));
            string prefix=original.Substring(0,split),support=original.Substring(split);
            string changed=prefix+PhysiologyContractTests.ReplaceNumber(support,member,value);
            Assert.Catch(()=>VenueRangeMatDefinition.FromJson(changed));
        }
        [Test]
        public void SupportRoundTripPreservesItsSourceDataWhileAbsenceRetainsTheLegacyBodyPredicate()
        {
            Gate(out var mat);var copy=VenueRangeMatDefinition.FromJson(mat.ToJson());
            Assert.That(copy.ToJson(),Is.EqualTo(mat.ToJson()));Assert.That(copy.ProneSupport.Id,Is.EqualTo(mat.ProneSupport.Id));
            var bare=VenueRangeMatDefinition.FromJson(VenueRangeMatDefinitionTests.MatJson);var body=OccupancyTests.Definition();
            Assert.That(bare.ProneSupport,Is.Null);Assert.That(bare.ToJson(),Does.Not.Contain("prone_support"));
            foreach(double line in new[]{-.31,-.30,0,.30,.31})
                foreach(var stance in new[]{ShootingStance.Prone,ShootingStance.Standing})
                    Assert.That(bare.ContainsBodyAndSupports(0,line,body,stance),Is.EqualTo(bare.ContainsBody(0,line,body)));
            Assert.That(mat.ContainsBodyAndSupports(1,0,body,ShootingStance.Prone),Is.False,"Support bounds never replace the unchanged body.");
        }
    }
}

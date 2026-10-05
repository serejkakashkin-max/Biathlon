using System;
using System.Collections.Generic;
using System.Globalization;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class VenueFormatBindingTests
    {
        private static Dictionary<string,string> Resources()
        {
            var resources=VenuePathAuthoringTests.Resources();var track=TrackDefinition.FromJson(resources["track"]);
            resources["manifest"]=resources["manifest"].Replace("\"schema_version\": 2","\"schema_version\": 2,\"format_bindings_resource\":\"formats\"");
            var profile=CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(CompetitionProfileTests.TestProfile().ToJson(),"distance_m","480"));
            resources["profile"]=profile.ToJson();resources["route"]=RaceRoutePlan.TimeTrial("catalog_route",track.Id,"main",profile.ToRouteRequirements()).ToJson();
            resources["formats"]="{\"schema_version\":1,\"id\":\"fixture_formats\",\"venue_id\":\""+VenueManifest.FromJson(resources["manifest"]).Id+
                "\",\"track_id\":\""+track.Id+"\",\"formats\":[{\"id\":\"sprint\",\"profile_resource\":\"profile\",\"route_resource\":\"route\"}]}";
            return resources;
        }
        private static VenueAuthoringPlan Plan(Dictionary<string,string> data)=>new VenueAuthoringPlan(VenueManifest.FromJson(data["manifest"]),id=>data[id]);
        [Test] public void OptionalCatalogRoundTripBindsExactProfileAndRouteWithoutChangingLegacyOutput()
        {
            var data=Resources();var plan=Plan(data);Assert.That(plan.Formats.Count,Is.EqualTo(1));var format=plan.Formats.Get(0);
            Assert.That(format.Id,Is.EqualTo("sprint"));Assert.That(format.Profile.Distance.Meters,Is.EqualTo(480));Assert.That(format.Route.TrackId,Is.EqualTo(plan.Track.Id));
            Assert.That(plan.Formats.UsesLoop("main"),Is.True);Assert.That(plan.Formats.UsesLoop("mat"),Is.False);
            string canonical=plan.Formats.ToJson();Assert.That(VenueFormatBindings.FromJson(canonical,plan.Manifest,plan.Track,plan.Junctions,plan.Connections,id=>data[id]).ToJson(),Is.EqualTo(canonical));
            Assert.That(VenuePathAuthoringTests.Plan().Formats,Is.Null);Assert.That(VenueValidatorTests.Plan().Manifest.ToJson(),Does.Not.Contain("format_bindings_resource"));
        }
        [Test] public void CatalogRejectsForeignVenueTrackFormatDuplicateAndUnsafeResources()
        {
            foreach(string change in new[]{"venue","track","format","duplicate","resource"})
            {
                var data=Resources();var manifest=VenueManifest.FromJson(data["manifest"]);var track=TrackDefinition.FromJson(data["track"]);
                if(change=="venue")data["formats"]=data["formats"].Replace(manifest.Id,"foreign_venue");
                if(change=="track")data["formats"]=data["formats"].Replace(track.Id,"foreign_track");
                if(change=="format")data["formats"]=data["formats"].Replace("\"id\":\"sprint\"","\"id\":\"pursuit\"");
                if(change=="duplicate")data["formats"]=data["formats"].Replace("\"formats\":[","\"formats\":[{\"id\":\"sprint\",\"profile_resource\":\"profile\",\"route_resource\":\"route\"},");
                if(change=="resource")data["formats"]=data["formats"].Replace("\"profile_resource\":\"profile\"","\"profile_resource\":\"../profile\"");
                Assert.Catch(()=>Plan(data),change);
            }
            Assert.Catch(()=>VenueManifest.FromJson(VenueValidatorTests.Read("manifest").Replace("\"schema_version\": 1","\"schema_version\": 1,\"format_bindings_resource\":\"formats\"")));
        }
        [Test] public void ProfileDistanceAndPenaltyCircuitCannotBeSilentlyReinterpreted()
        {
            var data=Resources();data["profile"]=PhysiologyContractTests.ReplaceNumber(data["profile"],"distance_m","481");Assert.Catch(()=>Plan(data));
            data=Resources();data["profile"]=PhysiologyContractTests.ReplaceNumber(data["profile"],"penalty_loop_m","149");Assert.Catch(()=>Plan(data));
        }
        [Test] public void ValidCatalogDoesNotExemptAnUnusedOpenWorldLoop()
        {
            var data=Resources();data["track"]=data["track"].Replace("\"loops\":[","\"loops\":["+VenuePathAuthoringTests.Loop("unused_open",false,6,20)+",");
            data["visual"]=data["visual"].Replace("\"loops\":[","\"loops\":["+VenuePathAuthoringTests.Visual("unused_open",new[]{new[]{0d,1000,0,1000},new[]{20d,1020,0,1000}})+",");
            var report=VenueValidator.Validate(Plan(data),VenueValidatorTests.Policy(),_=>true);bool found=false;
            for(int i=0;i<report.Count;i++)found|=report.Get(i).Code=="loop.open"&&report.Get(i).Path=="loops/unused_open";
            Assert.That(found,Is.True,report.ToJson());
        }
        [Test] public void CatalogCannotUseLegacyWholeLoopOccurrencesToCertifyOpenBranches()
        {
            var data=Resources();data["route"]=data["route"].Replace("\"main\"","\"mat\"");data["profile"]=PhysiologyContractTests.ReplaceNumber(data["profile"],"distance_m","120");
            Assert.Catch(()=>Plan(data));
        }
        [Test] public void ActualProductionCatalogUsesExactDirectedBranchesAndSharedPhysicalMainIdentity()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);Assert.That(plan.Formats.Count,Is.EqualTo(3));
            // Owner-authorized compressed game distances; WP169 historic lengths remain
            // frozen separately in docs/quality/fixtures/directed-passages-wp169.
            var expected=new Dictionary<string,long>{{"sprint",4200001},{"pursuit",5000000},{"mass_start",6000000}};
            for(int i=0;i<plan.Formats.Count;i++)
            {
                var format=plan.Formats.Get(i);var route=new RaceRoute(plan.Track,format.Route,format.Profile.ToRouteRequirements(),plan.Junctions,plan.Connections);
                Assert.That(route.Length.Millimeters,Is.EqualTo(expected[format.Id]));var layout=new PhysicalRangeFieldLayout(new PhysicalTrackMap(plan.Track,route),plan.Track,route,plan.Paths);
                Assert.That(layout.Count,Is.EqualTo(30));for(int lane=0;lane<30;lane++)Assert.That(layout.GetPath(lane).MapRoute(0).LoopId,Is.EqualTo("main"));
            }
            var policy=VenueValidationPolicy.FromJson(Read("Biathlon/Venues/production_venue_validation_v1"));
            var report=VenueValidator.Validate(plan,policy,_=>true);Assert.That(report.Passed,Is.True,report.ToJson());
            string captured=plan.Formats.ToJson();var previous=CultureInfo.CurrentCulture;
            try{CultureInfo.CurrentCulture=new CultureInfo("ru-RU");Assert.That(new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read).Formats.ToJson(),Is.EqualTo(captured));}
            finally{CultureInfo.CurrentCulture=previous;}
        }
    }
}

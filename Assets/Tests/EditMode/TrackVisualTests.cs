using Biathlon.Domain;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class TrackVisualTests
    {
        [Test] public void VisualGeometryBindsAuthoritativeDistancesAndRejectsBrokenSeams()
        {
            var track=TrackDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/synthetic_stadium_v1.json"));string json=PhysiologyContractTests.DefinitionJson("stadium_visual_v1.json");var d=TrackVisualDefinition.FromJson(json,track);
            Assert.That(d.Count,Is.EqualTo(track.LoopCount));Assert.That(TrackVisualDefinition.FromJson(d.ToJson(),track).ToJson(),Is.EqualTo(d.ToJson()));
            for(int i=0;i<d.Count;i++){var loop=d.Get(i);var start=loop.Sample(0);var end=loop.Sample(track.GetLoop(loop.LoopId).Length.Meters);if(track.GetLoop(loop.LoopId).Closed){Assert.That(start.X,Is.EqualTo(end.X));Assert.That(start.Z,Is.EqualTo(end.Z));}Assert.Catch(()=>loop.Sample(double.NaN));}
            Assert.Catch(()=>TrackVisualDefinition.FromJson(json.Replace("synthetic_stadium_v1","wrong_track"),track));
            var main=d.Get("main_long");Assert.That(main.Sample(800).Z,Is.EqualTo(100));Assert.That(main.Sample(900).Z,Is.Zero);
            for(int i=1;i<=30;i++){var lane=d.Get("range_lane_"+i);Assert.That(lane.Sample(0).Z,Is.EqualTo(main.Sample(800).Z));Assert.That(lane.Sample(100).X,Is.EqualTo(main.Sample(900).X));}
        }
    }
}

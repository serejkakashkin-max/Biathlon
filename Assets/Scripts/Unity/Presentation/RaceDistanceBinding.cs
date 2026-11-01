using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
namespace Biathlon.Unity.Presentation
{
    public static class RaceDistanceBinding
    {
        public static RaceDistanceView Create(CompetitionProfile profile,RaceRoute route=null)
        {
            string resource=RaceDistanceView.OfficialProfileResource(profile);
            var official=resource==null?profile:CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/"+resource));
            return new RaceDistanceView(profile,official,route);
        }
    }
}

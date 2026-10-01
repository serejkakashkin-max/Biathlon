#if UNITY_EDITOR
using System.IO;
using Biathlon.Domain;
using UnityEditor;
using UnityEngine;
namespace Biathlon.Unity
{
    public static class ProductionVenueValidation
    {
        public const string PolicyResource="Biathlon/Venues/production_venue_validation_v1";
        public static VenueValidationReport Validate(string manifestResource,string policyResource=PolicyResource)
        {
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueAuthoring.ReadResource(manifestResource)),ProductionVenueAuthoring.ReadResource);
            var policy=VenueValidationPolicy.FromJson(ProductionVenueAuthoring.ReadResource(policyResource));
            return VenueValidator.Validate(plan,policy,id=>Resources.Load<GameObject>(id)!=null);
        }
        [MenuItem("BIATHLON/Venues/Validate Pipeline Fixture")]
        public static void ValidateFixture()
        {
            var report=Validate(ProductionVenueAuthoring.Fixture);Directory.CreateDirectory("artifacts/venue-validation");
            File.WriteAllText("artifacts/venue-validation/"+report.Id+".json",report.ToJson());
            Debug.Log("Venue static validation: "+(report.Passed?"PASS":"FAIL")+"; "+report.Count+" diagnostics. See artifacts/venue-validation.");
        }
    }
}
#endif

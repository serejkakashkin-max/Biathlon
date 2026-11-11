using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Cold composition from versioned resource definitions. The
    /// caller selects signed movement explicitly; legacy fields are unchanged.</summary>
    public static class ManualProductionFieldComposition
    {
        public static ManualScopedFieldStepper Create(PackSprintFieldSession field,VenueAuthoringPlan venue,Func<string,string> readCore,
            ManualCourseReactionPolicy courseReactionPolicy=ManualCourseReactionPolicy.PassiveCurvedV26)
        {
            if(field==null||venue==null||readCore==null)throw new ArgumentNullException();
            var models=new SkiModelDefinitions(ProductionGameplayDefinitions.Bind(readCore));
            var contact=SkiContactDefinition.FromJson(readCore("manual_ski_contact_gameplay_v1.json"));
            var work=SkiContactSweepDefinition.FromJson(readCore("manual_ski_contact_sweep_v1.json"));
            string groupResource = courseReactionPolicy == ManualCourseReactionPolicy.StableAthleteContactV28
                ? "manual_ski_contact_group_gameplay_v2.json" : "manual_ski_contact_group_gameplay_v1.json";
            var group = SkiContactGroupDefinition.FromJson(readCore(groupResource));
            var steering=ManualSkiSteeringDefinition.FromJson(readCore("manual_ski_steering_v2.json"));
            field.ArrangeManualPursuitStartLines(venue.Occupancy);
            var world=ManualScopedFieldStepper.CreateInitialWorld(field,venue.Track,venue.Visual,venue.Occupancy,contact,work,group,models.Physics,
                ManualFinishRunoutDefinition.FromJson(readCore("manual_finish_runout_gameplay_v1.json")),
                ManualNumericalToleranceDefinition.FromJson(readCore("manual_gameplay_tolerance_v1.json")),
                ManualMatCaptureDefinition.FromJson(readCore("manual_mat_capture_v1.json")),
                ManualCourseContactDefinition.FromJson(readCore(ProductionGameplayDefinitions.Resource("manual_course_contact_v1.json"))),courseReactionPolicy);
            if(venue.SharedApron!=null&&venue.Manifest.Range.Forward.X==0&&venue.Manifest.Range.Forward.Z==1)
                world.BindRangeApronSnow(new ManualRangeApronSnowRegion(venue));
            return new ManualScopedFieldStepper(field,world,models,venue.Occupancy,steering,work,
                ManualMatBrakingDefinition.FromJson(readCore("manual_mat_braking_v1.json")));
        }
    }
}

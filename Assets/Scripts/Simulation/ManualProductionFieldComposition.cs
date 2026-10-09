using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Cold composition from versioned resource definitions. The
    /// caller selects signed movement explicitly; legacy fields are unchanged.</summary>
    public static class ManualProductionFieldComposition
    {
        public static ManualScopedFieldStepper Create(PackSprintFieldSession field,VenueAuthoringPlan venue,Func<string,string> readCore)
        {
            if(field==null||venue==null||readCore==null)throw new ArgumentNullException();
            var models=new SkiModelDefinitions(readCore);
            var contact=SkiContactDefinition.FromJson(readCore("manual_ski_contact_v1.json"));
            var work=SkiContactSweepDefinition.FromJson(readCore("manual_ski_contact_sweep_v1.json"));
            var group=SkiContactGroupDefinition.FromJson(readCore("manual_ski_contact_group_v1.json"));
            var steering=ManualSkiSteeringDefinition.FromJson(readCore("manual_ski_steering_v2.json"));
            var world=ManualScopedFieldStepper.CreateInitialWorld(field,venue.Track,venue.Visual,venue.Occupancy,contact,work,group,models.Physics,
                ManualFinishRunoutDefinition.FromJson(readCore("manual_finish_runout_v1.json")),
                ManualNumericalToleranceDefinition.FromJson(readCore("manual_numerical_tolerance_v1.json")));
            return new ManualScopedFieldStepper(field,world,models,venue.Occupancy,steering,work,
                ManualMatBrakingDefinition.FromJson(readCore("manual_mat_braking_v1.json")));
        }
    }
}

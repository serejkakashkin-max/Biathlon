using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    public readonly struct PackForceCommand
    {
        public SimulationTick Tick { get; }
        public AthleteId AthleteId { get; }
        public TrackSample Track { get; }
        public double DeliveredPowerWatts { get; }
        public double DesiredLateralMeters { get; }
        public double MinimumPathWidthMeters { get; }
        public double Headwind { get; }
        public double Crosswind { get; }
        public double BrakingNewtons { get; }
        public double SnowResistanceMultiplier { get; }
        public PackForceCommand(SimulationTick tick,AthleteId athlete,TrackSample track,double power,double lateral,double width,
            double headwind=0,double crosswind=0,double braking=0,double snowResistance=1)
        { Tick=tick; AthleteId=athlete; Track=track; DeliveredPowerWatts=power; DesiredLateralMeters=lateral; MinimumPathWidthMeters=width;
          Headwind=headwind; Crosswind=crosswind; BrakingNewtons=braking; SnowResistanceMultiplier=snowResistance; }
    }
    public readonly struct PackForceFrame
    {
        public SimulationTick Tick { get; }
        public SkiPlacement Placement { get; }
        public DraftOutput Draft { get; }
        public SkiForceOutput UnconstrainedForces { get; }
        public ContactObservation Contact { get; }
        public double LateralPropulsionMultiplier { get; }
        internal PackForceFrame(SimulationTick tick,SkiPlacement placement,DraftOutput draft,SkiForceOutput forces,ContactObservation contact,double lateralMultiplier)
        { Tick=tick; Placement=placement; Draft=draft; UnconstrainedForces=forces; Contact=contact; LateralPropulsionMultiplier=lateralMultiplier; }
    }
    /// <summary>Fixed-corridor whole-tick kernel, with owned authoritative positions. Route branches,
    /// boundaries and physiological power production must be composed by the race-path orchestrator.</summary>
    public sealed class PackForceSession
    {
        private readonly SkiPlacement[] placements, proposals, resolved;
        private readonly double[] widths, lateralMultipliers;
        private readonly SkiForceOutput[] forces;
        private readonly DraftOutput[] drafts;
        private readonly PackForceFrame[] frames;
        private readonly SkiForceSystem forceSystem;
        private readonly DraftingSystem draftSystem;
        private readonly SeparationResolver separation;
        private readonly OccupancySystem occupancy;
        private readonly OvertakeDefinition movement;
        private readonly double lateralStep;
        private readonly double maximumPower;
        public SimulationTick Tick { get; private set; }
        public int Count=>placements.Length;
        public SkiPlacement GetPlacement(int index)=>placements[index];
        public PackForceFrame GetFrame(int index)=>frames[index];
        public PackForceSession(SkiPhysicsDefinition physics,DraftDefinition draft,OvertakeDefinition movement,
            OccupancyDefinition occupancy,int frequency,ReadOnlySpan<SkiPlacement> initial,IRaceEventSink events)
        {
            if(initial.Length==0 || frequency<=0 || frequency>10000) throw new ArgumentException("Nonempty roster and valid clock required.");
            this.movement=movement??throw new ArgumentNullException(nameof(movement));
            this.occupancy=new OccupancySystem(occupancy); this.occupancy.ValidateRoster(initial);
            forceSystem=new SkiForceSystem(physics,frequency); draftSystem=new DraftingSystem(draft);
            maximumPower=physics.MaximumPowerWatts;
            for(int i=0;i<initial.Length;i++) PhysiologyNumbers.RequireRange(initial[i].Motion.SpeedMetersPerSecond,0,physics.MaximumSpeedMetersPerSecond,"initial speed");
            separation=new SeparationResolver(this.occupancy,initial.Length,frequency,events);
            lateralStep=movement.LateralSpeed/frequency;
            if(lateralStep<PhysiologyNumbers.MinimumPositive) throw new ArgumentException("Lateral step below coordinate precision.");
            placements=initial.ToArray(); proposals=new SkiPlacement[Count]; resolved=new SkiPlacement[Count];
            widths=new double[Count]; lateralMultipliers=new double[Count]; forces=new SkiForceOutput[Count]; drafts=new DraftOutput[Count]; frames=new PackForceFrame[Count];
        }
        public void Step(ReadOnlySpan<PackForceCommand> commands)
        {
            if(commands.Length!=Count) throw new ArgumentException("Complete stable-order pack command batch required.");
            var tick=Tick.Next();
            // Preparation uses private scratch only: later invalid rows cannot advance earlier athletes.
            for(int i=0;i<Count;i++)
            {
                var input=commands[i]; var before=placements[i];
                if(!input.Tick.Equals(tick) || !input.AthleteId.Equals(before.AthleteId)) throw new ArgumentException("Pack row identity/tick mismatch.");
                PhysiologyNumbers.RequireRange(input.DesiredLateralMeters,-1000000,1000000,nameof(input.DesiredLateralMeters));
                if(input.MinimumPathWidthMeters>input.Track.WidthMeters || !occupancy.FitsWidth(before.LateralMeters,input.MinimumPathWidthMeters))
                    throw new ArgumentException("Minimum path width must include the current placement/track.");
                double delta=Math.Max(-lateralStep,Math.Min(lateralStep,input.DesiredLateralMeters-before.LateralMeters));
                double lateral=PhysiologyNumbers.Quantize(before.LateralMeters+delta);
                double fraction=Math.Min(1,Math.Abs(lateral-before.LateralMeters)/lateralStep);
                double multiplier=1-fraction*movement.LateralPropulsionLoss;
                var draft=draftSystem.Evaluate(before,placements,input.Track,input.Headwind,input.Crosswind);
                // Power is supplied after physiology accounts for effort, including any lateral effort surcharge.
                PhysiologyNumbers.RequireRange(input.DeliveredPowerWatts,0,maximumPower,nameof(input.DeliveredPowerWatts));
                var force=forceSystem.Step(before.Motion,input.DeliveredPowerWatts*multiplier,input.Track,input.Headwind,
                    draft.DragAreaMultiplier,input.BrakingNewtons,input.SnowResistanceMultiplier);
                drafts[i]=draft; forces[i]=force; widths[i]=input.MinimumPathWidthMeters; lateralMultipliers[i]=multiplier;
                proposals[i]=new SkiPlacement(before.AthleteId,before.CorridorId,force.State,lateral);
            }
            separation.Resolve(tick,placements,proposals,widths,resolved);
            for(int i=0;i<Count;i++)
            {
                placements[i]=resolved[i];
                frames[i]=new PackForceFrame(tick,placements[i],drafts[i],forces[i],separation.GetContact(i),lateralMultipliers[i]);
            }
            Tick=tick;
        }
    }
}

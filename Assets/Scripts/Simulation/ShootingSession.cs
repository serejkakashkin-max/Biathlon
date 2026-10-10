using System;
using System.Globalization;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct ShootingSessionInput
    {
        public WeaponAimCommand Aim { get; } public ShootingAction Action { get; } public ShootingLaneAssignment Lane { get; }
        public bool HoldBreath { get; } public double HeartRateBpm { get; } public double Fatigue { get; } public double ArmFatigue { get; }
        public WindVector Wind { get; } public double HeadingRadians { get; } public double Exposure { get; } public ShootingPressureContext Pressure { get; }
        public ShootingSessionInput(WeaponAimCommand aim, ShootingAction action, ShootingLaneAssignment lane, bool holdBreath,
            double heartRateBpm, double fatigue, double armFatigue, WindVector wind = default, double headingRadians = 0, double exposure = 1,
            ShootingPressureContext pressure = default)
        {
            Aim = aim; Action = action; Lane = lane; HoldBreath = holdBreath; HeartRateBpm = heartRateBpm; Fatigue = fatigue; ArmFatigue = armFatigue;
            Wind = wind; HeadingRadians = headingRadians; Exposure = exposure; Pressure = pressure;
        }
    }
    public readonly struct ShootingSessionFrame
    {
        public ShootingSessionInput Input { get; } public WeaponAimState Aim { get; } public BreathingFrame Breathing { get; }
        public HeartbeatFrame Heartbeat { get; } public MuscularSwayFrame Muscular { get; } public BreathControlFrame BreathControl { get; }
        public ShootingPressureFrame Pressure { get; } public ShootingSwayFrame Sway { get; } public ShootingHandlingFrame Handling { get; }
        public WeaponAimState ShotAim { get; } public ShotResolutionFrame Shot { get; }
        public ShootingSessionFrame(ShootingSessionInput input, WeaponAimState aim, BreathingFrame breathing, HeartbeatFrame heartbeat,
            MuscularSwayFrame muscular, BreathControlFrame breathControl, ShootingPressureFrame pressure, ShootingSwayFrame sway,
            ShootingHandlingFrame handling, WeaponAimState shotAim, ShotResolutionFrame shot)
        { Input = input; Aim = aim; Breathing = breathing; Heartbeat = heartbeat; Muscular = muscular; BreathControl = breathControl;
            Pressure = pressure; Sway = sway; Handling = handling; ShotAim = shotAim; Shot = shot; }
    }
    public sealed class ShootingSession
    {
        private readonly ShootingModelDefinitions model;
        private readonly ShootingAthleteDefinition athlete;
        private readonly ShootingStanceProfile stance;
        private readonly WeaponAimSystem aim;
        private readonly BreathingSystem breathing;
        private readonly HeartbeatSystem heartbeat;
        private readonly MuscularSwaySystem muscular;
        private readonly BreathControlSystem breathControl;
        private readonly ShootingPressureSystem pressure;
        private readonly ShootingHandlingSystem handling;
        private readonly ShotResolutionSystem shots;
        private readonly ShootingWindSystem windValidation;
        private readonly PhysiologyDefinition physiologyDefinition;
        private readonly bool manualBoltCycle;
        public SimulationTick InitialTick { get; }
        public AthleteId AthleteId { get; }
        public int Frequency { get; }
        public SimulationTick Tick { get; private set; }
        public ShootingPhase Phase => shots.Phase;
        public int ShotsFired => shots.ShotsFired;
        public bool NeedsBoltCycle => shots.NeedsBoltCycle;
        public int ClosedMask => shots.ClosedMask;
        public int PhaseDurationTicks => shots.PhaseDurationTicks;
        public int PhaseRemainingTicks => shots.PhaseRemainingTicks;
        public int PhaseElapsedTicks => shots.PhaseElapsedTicks;
        public string ContextJson { get; }
        public ShootingSessionFrame LastFrame { get; private set; }
        public WeaponAimState Sight => new WeaponAimState(Clip(LastFrame.Aim.YawRadians + LastFrame.Sway.HorizontalRadians + LastFrame.Handling.HorizontalRadians),
            Clip(LastFrame.Aim.PitchRadians + LastFrame.Sway.VerticalRadians + LastFrame.Handling.VerticalRadians));
        public ShootingSession(SimulationConfig config, ShootingModelDefinitions model, ShootingAthleteDefinition athlete,
            ShootingStance stance, string rangeId, IRaceEventSink events, SimulationTick initialTick = default,
            PhysiologyDefinition physiologyDefinition = null,bool manualBoltCycle=false)
        {
            if (config == null || model == null || athlete == null) throw new ArgumentNullException(nameof(config));
            this.manualBoltCycle=manualBoltCycle;
            this.model = model; this.athlete = athlete; this.stance = model.Stances.Get(stance); Frequency = config.TickFrequency; AthleteId = new AthleteId(athlete.Id);
            this.physiologyDefinition = physiologyDefinition ?? model.Physiology; InitialTick = initialTick; Tick = initialTick;
            aim = new WeaponAimSystem(model.Aim, AthleteId, Frequency, initialTick: initialTick, stance: this.stance);
            breathing = new BreathingSystem(model.Breathing, this.physiologyDefinition, Frequency, initialTick: initialTick);
            heartbeat = new HeartbeatSystem(model.Heartbeat, this.physiologyDefinition, Frequency, initialTick);
            muscular = new MuscularSwaySystem(model.Muscular, Frequency, new SeededRandomSource(config.Seed), initialTick);
            breathControl = new BreathControlSystem(model.BreathControl, AthleteId, Frequency, events, this.stance, initialTick);
            pressure = new ShootingPressureSystem(model.Pressure, Frequency, this.stance, initialTick);
            handling = new ShootingHandlingSystem(model.Handling, model.Equipment, athlete.ShootingSpeed, Frequency, initialTick);
            shots = new ShotResolutionSystem(model.Lifecycle, model.Targets, model.Wind, this.stance, AthleteId, rangeId, Frequency, events, initialTick, handling.Profile,manualBoltCycle);
            windValidation = new ShootingWindSystem(model.Wind);
            ContextJson = ShootingModelDefinitions.EncodeContext("biathlon_shooting_session_v1", config.Id,
                Frequency.ToString(CultureInfo.InvariantCulture), config.Seed.ToString(CultureInfo.InvariantCulture), model.ContextJson, athlete.ToJson(), this.stance.Id, rangeId);
            if (initialTick.Value != 0 || physiologyDefinition != null)
                ContextJson = ShootingModelDefinitions.EncodeContext("biathlon_shooting_session_v2", ContextJson,
                    initialTick.Value.ToString(CultureInfo.InvariantCulture), this.physiologyDefinition.ToJson());
            if(manualBoltCycle)ContextJson=ShootingModelDefinitions.EncodeContext("biathlon_manual_bolt_session_v2_reload_handling",ContextJson);
        }
        public ShootingSessionFrame Step(ShootingSessionInput input)
        {
            if (Phase == ShootingPhase.Complete) throw new InvalidOperationException("Shooting bout is complete.");
            if (!input.Aim.AthleteId.Equals(AthleteId) || input.Aim.Tick.Value != checked(Tick.Value + 1)) throw new ArgumentException("Invalid session command identity/tick.");
            shots.ValidateCommand(input.Aim.Tick, input.Action, input.Lane);
            PhysiologyNumbers.RequireRange(input.HeartRateBpm, physiologyDefinition.RestingHrBpm, physiologyDefinition.HrSafetyCeilingBpm, nameof(input.HeartRateBpm));
            PhysiologyNumbers.RequireRange(input.Fatigue, 0, 1, nameof(input.Fatigue)); PhysiologyNumbers.RequireRange(input.ArmFatigue, 0, 1, nameof(input.ArmFatigue));
            windValidation.Evaluate(input.Wind, input.HeadingRadians, input.Exposure, model.Targets.DistanceMeters);
            // Private subsystems share this clock; all external input is now validated. Their bounded arithmetic cannot reject the composed angles.
            var t = input.Aim.Tick;
            var a = aim.Step(input.Aim); var b = breathing.Step(t, input.HeartRateBpm, input.Fatigue, athlete.BreathControl);
            var h = heartbeat.Step(t, input.HeartRateBpm);
            var m = muscular.Step(t, input.Fatigue, input.ArmFatigue, stance.Stance == ShootingStance.Prone ? athlete.ProneStability : athlete.StandingStability);
            var bc = breathControl.Step(t, input.HoldBreath, athlete.BreathControl); var p = pressure.Step(t, input.Pressure, athlete.Composure);
            var sway = ShootingSwaySystem.Compose(stance, b, h, m, bc.BreathingMultiplier, p);
            var previousHandling = handling.LastFrame;
            var shotAim = new WeaponAimState(Clip(a.YawRadians + sway.HorizontalRadians + previousHandling.HorizontalRadians),
                Clip(a.PitchRadians + sway.VerticalRadians + previousHandling.VerticalRadians));
            bool reloadAccepted=input.Action==ShootingAction.CycleBolt&&shots.NeedsBoltCycle;
            var shot = shots.Step(t, input.Action, input.Lane, shotAim.Ray, input.Wind, input.HeadingRadians, input.Exposure);
            var recoil = handling.Step(t, manualBoltCycle?reloadAccepted:shot.Lifecycle.ShotAccepted);
            LastFrame = new ShootingSessionFrame(input, a, b, h, m, bc, p, sway, recoil, shotAim, shot); Tick = t; return LastFrame;
        }
        private static double Clip(double angle) => Math.Max(-0.5, Math.Min(0.5, PhysiologyNumbers.Quantize(angle)));
    }
}

using System;
using System.Collections;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class RaceStartPresentationTests
    {
        private GameObject root;
        [TearDown] public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);}
        private ProductionVenueHost Host()
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            root=new GameObject("Race start and pace acceptance");var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
            host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_profile")),
                RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_route")),
                RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),count:1);
            return host;
        }

        [Test] public void ThreeSecondPreparationFreezesRaceAndInputClocksIncludingTheGoBoundary()
        {
            var host=Host();host.ArmRaceStartCountdown();
            Assert.That(host.CountdownRemaining,Is.EqualTo(3));Assert.That(host.CountdownNumber,Is.EqualTo(3));
            for(int expected=2;expected>=0;expected--)
            {
                host.AdvanceSimulationFrame(1);
                Assert.That(host.CountdownRemaining,Is.EqualTo(expected));Assert.That(host.CountdownNumber,Is.EqualTo(expected));
                Assert.That(host.Runner.Field.Tick.Value,Is.Zero,"Countdown time must never enter the race accumulator.");
                Assert.That(host.Feed.Current.Tick,Is.Zero,"The presentation observes the same frozen authority.");
            }
            host.AdvanceSimulationFrame(.05);
            Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(1),"Exactly one normal20Hz tick follows preparation; there is no3s catch-up burst.");
            Assert.That(host.HasSimulationFault,Is.False,"The first input command must still target consecutive race tick1.");
            Assert.Throws<InvalidOperationException>(()=>host.ArmRaceStartCountdown(),"Preparation cannot be used to reset an already-running race.");
        }

        [Test] public void UntouchedPaceStartsAtZeroAndAcceptsTheSharedStopBrake()
        {
            var host=Host();Assert.That(host.SelectedPace,Is.Zero);
            for(int tick=0;tick<20;tick++)host.AdvanceSimulationFrame(.05);
            Assert.That(host.HasSimulationFault,Is.False);Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(20));
            Assert.That(host.SelectedPace,Is.Zero);Assert.That(host.Runner.GetLastManualBrake(host.PlayerIndex),Is.EqualTo(1),
                "Idle at the bottom of the pace dial requests the same explicit physics brake asS, not autonomous propulsion.");
        }

        [UnityTest] public IEnumerator CountdownIsVisibleInRetainedHudWhileAuthorityRemainsFrozen()
        {
            var host=Host();host.ArmRaceStartCountdown();host.GameplayHud.Refresh();yield return null;
            var overlay=host.GameplayHud.Root.Q("start_countdown");Assert.That(overlay,Is.Not.Null);
            Assert.That(overlay.style.display.value,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(host.Runner.Field.Tick.Value,Is.Zero);
            host.AdvanceSimulationFrame(1);host.GameplayHud.Refresh();yield return null;
            Assert.That(host.CountdownNumber,Is.EqualTo(2));Assert.That(overlay.style.display.value,Is.EqualTo(DisplayStyle.Flex));
            host.AdvanceSimulationFrame(1);host.AdvanceSimulationFrame(1);host.AdvanceSimulationFrame(.05);host.GameplayHud.Refresh();yield return null;
            yield return new WaitForSecondsRealtime(.65f);host.GameplayHud.Refresh();
            Assert.That(overlay.style.display.value,Is.EqualTo(DisplayStyle.None));Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(1));
            Assert.That(host.HasSimulationFault,Is.False);
        }

        [Test] public void ActualKeyboardRaisesHoldsAndLowersPaceAndCannotGoBelowZero()
        {
            using(var inputScope=new InputTestScope())
            {
                var keyboard=InputSystem.AddDevice<Keyboard>();
                root=new GameObject("Keyboard pace dial acceptance");var adapter=root.AddComponent<PlayerDeviceInput>();adapter.enabled=false;
                adapter.Initialize(new AthleteId("pace_player"),20);
                try
                {
                    long tick=1;
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputTestScope.Update();adapter.Capture(keyboard,null);
                    var initial=adapter.BuildManual(new SimulationTick(tick++));Assert.That(initial.AnalogEffort,Is.Zero);Assert.That(initial.BrakeFraction,Is.EqualTo(1));
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputTestScope.Update();adapter.Capture(keyboard,null);
                    ManualSkiCommand raised=default;
                    for(int i=0;i<12;i++)raised=adapter.BuildManual(new SimulationTick(tick++));
                    Assert.That(raised.AnalogEffort,Is.GreaterThan(0));Assert.That(raised.BrakeFraction,Is.Zero);
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputTestScope.Update();adapter.Capture(keyboard,null);
                    var held=adapter.BuildManual(new SimulationTick(tick++));Assert.That(held.AnalogEffort,Is.EqualTo(raised.AnalogEffort),"ReleasingW holds the selected pace instead of resetting it.");
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));InputTestScope.Update();adapter.Capture(keyboard,null);
                    var lowered=adapter.BuildManual(new SimulationTick(tick++));Assert.That(lowered.AnalogEffort,Is.LessThan(held.AnalogEffort));
                    Assert.That(lowered.BrakeFraction,Is.GreaterThan(0),"S requests physical deceleration while lowering the pace dial.");
                    ManualSkiCommand stopped=default;
                    for(int i=0;i<50;i++)stopped=adapter.BuildManual(new SimulationTick(tick++));
                    Assert.That(stopped.AnalogEffort,Is.Zero);Assert.That(stopped.BrakeFraction,Is.EqualTo(1));
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputTestScope.Update();adapter.Capture(keyboard,null);
                    var idle=adapter.BuildManual(new SimulationTick(tick));Assert.That(idle.AnalogEffort,Is.Zero);Assert.That(idle.BrakeFraction,Is.EqualTo(1));
                }
                finally{InputSystem.RemoveDevice(keyboard);}
            }
        }
    }
}

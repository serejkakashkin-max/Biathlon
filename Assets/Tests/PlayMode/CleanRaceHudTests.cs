using System.Collections;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace Biathlon.PlayModeTests
{
    public sealed class CleanRaceHudTests
    {
        [UnityTest] public IEnumerator NormalRaceKeepsOnlyRaceInformationInBothLocales()
        {
            var root=new GameObject("Clean race interface");
            try
            {
                string Read(string path)=>ProductionVenueHost.ReadResource(path);
                var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read("Biathlon/Venues/woodland_basin_v1/manifest")),Read);var binding=venue.Formats.Get(0);
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                host.Initialize(venue,binding.Profile,binding.Route,RaceSnowConditions.FromJson(Read(host.ConditionsResource)),count:30);
                for(int i=0;i<600;i++)host.Runner.Step();host.Feed.Publish(Biathlon.Simulation.RacePresentationSnapshot.Capture(host.Runner.Field));
                foreach(string locale in new[]{"ru","en"})
                {
                    var hud=host.GameplayHud;hud.SetPreferences(locale,0);hud.Refresh();yield return null;
                    foreach(string id in new[]{"minimap_title","minimap_legend","minimap_rival_count","pace_keys","burst_hint","help_hint"})
                        Assert.That(hud.Root.Q(id),Is.Null,"Remove explanatory chrome entirely, including its reserved space: "+id);
                    Assert.That(hud.MiniMapVisible,Is.True);Assert.That(hud.Root.Q<Label>("speed").text,Is.Not.Empty);
                    Assert.That(hud.Root.Q<Label>("pace_label").text,Is.Not.Empty);Assert.That(hud.Root.Q<Label>("heart_rate").text,Is.Not.Empty);
                    foreach(string id in new[]{"weapon","breath","readiness_hint"})
                        Assert.That(hud.Root.Q<Label>(id).text??"",Does.Not.Match("Shift|Ctrl|Space|ЛКМ|ПКМ|LMB|RMB"),"Race statuses are information, not key instructions.");
                    Assert.That(hud.HelpVisible,Is.False);hud.SetHelpVisible(true);Assert.That(hud.Root.Q<Label>("controls").text,Is.Not.Empty);hud.SetHelpVisible(false);
                }
            }
            finally{Object.DestroyImmediate(root);}yield return null;
        }
    }
}

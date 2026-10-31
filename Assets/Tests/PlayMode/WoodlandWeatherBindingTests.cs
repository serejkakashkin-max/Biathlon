using System;
using System.Collections.Generic;
using System.IO;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    /// <summary>
    /// Actual imported Woodland content, freshly bound for every weather row.
    /// This is adapter/lease coverage, not a race, readability or performance result.
    /// </summary>
    public sealed class WoodlandWeatherBindingTests
    {
        private const string Prefix = "Biathlon/Venues/woodland_basin_v1/";

        [TestCase("high", "clear", "clear_cold")]
        [TestCase("high", "overcast", "overcast")]
        [TestCase("high", "snowfall", "overcast")]
        [TestCase("balanced", "snowfall", "overcast")]
        [TestCase("low", "snowfall", "overcast")]
        public void ColdWoodlandWeatherBindingUsesCanonicalInputsAndRestoresItsLease(
            string tier, string presetId, string lightingId)
        {
            var sourceTexts = new Dictionary<string, string>(StringComparer.Ordinal);
            var woodlandTexts = new Dictionary<TextAsset, string>();
            foreach (var asset in Resources.LoadAll<TextAsset>(Prefix.TrimEnd('/')))
                woodlandTexts.Add(asset, asset.text);
            string Read(string id)
            {
                var asset = Resources.Load<TextAsset>(id);
                if (asset == null) throw new FileNotFoundException(id);
                string text = asset.text;
                if (sourceTexts.TryGetValue(id, out var first))
                    Assert.That(text, Is.EqualTo(first), "A repeated source read must be immutable: " + id);
                else sourceTexts.Add(id, text);
                return text;
            }

            var plan = new VenueAuthoringPlan(VenueManifest.FromJson(Read(Prefix + "manifest")), Read);
            var definition = ProductionEnvironmentDefinition.FromJson(Read(Prefix + "environment"));
            var weatherDefinition = WeatherVisualDefinition.FromJson(Read(definition.lighting.weather_resource));
            var weatherPreset = weatherDefinition.Get(presetId);
            var lighting = LightingVisualDefinition.FromJson(Read(definition.lighting.resource)).Get(lightingId);
            Assert.That(RenderQualityDefinition.FromJson(Read("Biathlon/render_quality_v1")).Get(tier).id, Is.EqualTo(tier));
            definition.lighting.weather_preset = presetId;
            definition.lighting.preset_id = lightingId;
            float fogScale = definition.lighting.fog_distance_scale;
            float expectedHorizon = weatherPreset.fog_end_m * fogScale;
            float expectedForestHorizon = presetId=="snowfall"?Mathf.Max(weatherPreset.fog_end_m,weatherDefinition.Get("light_snow").fog_end_m)*fogScale:expectedHorizon;
            string track = plan.Track.ToJson(), visual = plan.Visual.ToJson(), paths = plan.Paths.ToJson();
            var saved = new SceneWeatherState();
            var root = new GameObject("Detached Woodland weather row " + tier + " " + presetId);
            ProductionVenueEnvironment environment = null;
            try
            {
                var cameraObject = new GameObject("Explicit disabled weather-binding camera");
                cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.transform.position = definition.range.origin - definition.range.forward * 40 + Vector3.up * 8;
                camera.transform.LookAt(definition.range.origin + Vector3.up);
                camera.farClipPlane = expectedHorizon + 100;
                var cameraPosition = camera.transform.position;
                var cameraRotation = camera.transform.rotation;
                var cameraClear = camera.clearFlags;
                var cameraBackground = camera.backgroundColor;
                var feed = root.AddComponent<RacePresentationFeed>();
                environment = root.AddComponent<ProductionVenueEnvironment>();
                environment.Initialize(plan, definition, Read, feed, camera);

                Assert.That(environment.TerrainTileCount, Is.EqualTo(definition.terrain.tiles.Length));
                Assert.That(environment.ForestGroupCount, Is.EqualTo(definition.forest.groups.Length));
                Assert.That(environment.ForestGroupCount, Is.GreaterThan(0), "The actual Woodland binding must contain a forest witness.");
                var expectedCrowds = new List<SpectatorSeating>();
                var crowdSources = new Dictionary<SpectatorSeating, string>();
                foreach (var placement in definition.assets)
                {
                    var prefab = Resources.Load<GameObject>(placement.resource);
                    Assert.That(prefab, Is.Not.Null, "Every declared source prefab must be present.");
                    var seating = prefab.GetComponentInChildren<SpectatorSeating>();
                    if (seating == null) continue;
                    expectedCrowds.Add(seating);
                    crowdSources[seating] = JsonUtility.ToJson(seating);
                }
                Assert.That(environment.CrowdCount, Is.EqualTo(expectedCrowds.Count));
                for (int i = 0; i < expectedCrowds.Count; i++)
                {
                    Assert.That(environment.Crowd(i).Total, Is.EqualTo(expectedCrowds[i].Count));
                    Assert.That(environment.Crowd(i).Pose, Is.EqualTo(expectedCrowds[i].Pose));
                    Assert.That(environment.Crowd(i).UsesAuditedCrowd, Is.False);
                }
                int declaredTrees = 0;
                for (int i = 0; i < definition.forest.groups.Length; i++)
                {
                    var group = definition.forest.groups[i];
                    var source = JsonUtility.FromJson<ProductionForestInstances>(Read(group.instances_resource));
                    Assert.That(source.instances.Length, Is.EqualTo(group.count));
                    Assert.That(environment.Forest(i).TotalInstances, Is.EqualTo(source.instances.Length));
                    declaredTrees += group.count;
                    Assert.That(environment.Forest(i).ProductionHighFogEndMeters, Is.EqualTo(expectedForestHorizon));
                }
                Assert.That(environment.TreeCount, Is.EqualTo(declaredTrees));
                var firstTree = environment.Forest(0).InstanceWorldPosition(0);

                // These edits are intentionally only to the caller DTO, after the cold
                // initialization. The live environment must retain its detached binding.
                definition.lighting.weather_preset = presetId == "clear" ? "snowfall" : "clear";
                definition.lighting.preset_id = lightingId == "overcast" ? "clear_cold" : "overcast";
                definition.lighting.fog_distance_scale = fogScale == 1 ? 3 : 1;
                definition.forest.high_visibility.mode = "caller_mutation";
                definition.forest.groups[0].center = Vector3.zero;
                environment.Render(camera, tier, 12);

                var weather = root.GetComponentInChildren<WeatherPresentation>();
                Assert.That(weather, Is.Not.Null);
                Assert.That(weather.PresetId, Is.EqualTo(presetId));
                int ceiling = weatherDefinition.Count(tier, weatherPreset.snowfall);
                Assert.That(weather.SubmittedFlakes, Is.InRange(0, ceiling));
                if (weatherPreset.snowfall == 0) Assert.That(weather.SubmittedFlakes, Is.Zero);
                else Assert.That(weather.SubmittedFlakes, Is.GreaterThan(0), "This actual above-ground camera must submit snowfall.");
                Assert.That(RenderSettings.fog, Is.True);
                Assert.That(RenderSettings.fogMode, Is.EqualTo(FogMode.Linear));
                Assert.That(RenderSettings.fogStartDistance, Is.EqualTo(weatherPreset.fog_start_m * fogScale));
                Assert.That(RenderSettings.fogEndDistance, Is.EqualTo(expectedHorizon));
                Assert.That(RenderSettings.fogColor, Is.EqualTo(weatherPreset.fog_color * lighting.horizon_tint));
                Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(weatherPreset.fog_color * weatherPreset.ambient_intensity * lighting.ambient_tint * lighting.ambient_gain));
                Assert.That(RenderSettings.skybox, Is.Not.SameAs(saved.Skybox));
                Light key=null;int directionalCount=0;
                foreach(var light in root.GetComponentsInChildren<Light>())
                {
                    if(light.type==LightType.Directional){key=light;directionalCount++;}
                    else{Assert.That(light.type,Is.EqualTo(LightType.Point));Assert.That(light.shadows,Is.EqualTo(LightShadows.None));Assert.That(light.enabled,Is.False,"Target fill is off in daytime conditions.");}
                }
                Assert.That(directionalCount,Is.EqualTo(1));Assert.That(key,Is.Not.Null);
                Assert.That(key.type, Is.EqualTo(LightType.Directional));
                Assert.That(key.color, Is.EqualTo(weatherPreset.light_color * lighting.sun_tint));
                Assert.That(key.intensity, Is.EqualTo(weatherPreset.light_intensity * lighting.sun_gain));
                Assert.That(key.shadowStrength, Is.EqualTo(weatherPreset.shadow_strength));
                Assert.That(Vector3.Distance(key.transform.forward, Quaternion.Euler(lighting.sun_elevation, lighting.sun_azimuth, 0) * Vector3.forward), Is.LessThan(1e-5f));
                for (int i = 0; i < environment.ForestGroupCount; i++)
                    Assert.That(environment.Forest(i).ProductionHighFogEndMeters, Is.EqualTo(expectedForestHorizon));
                Assert.That(environment.Forest(0).InstanceWorldPosition(0), Is.EqualTo(firstTree));
                Assert.That(camera.transform.position, Is.EqualTo(cameraPosition));
                Assert.That(camera.transform.rotation, Is.EqualTo(cameraRotation));
                Assert.That(feed.Current, Is.Null);
                Assert.That(feed.Previous, Is.Null);
                Assert.That(plan.Track.ToJson(), Is.EqualTo(track));
                Assert.That(plan.Visual.ToJson(), Is.EqualTo(visual));
                Assert.That(plan.Paths.ToJson(), Is.EqualTo(paths));
                foreach (var source in sourceTexts)
                    Assert.That(Resources.Load<TextAsset>(source.Key).text, Is.EqualTo(source.Value), "Production source text changed: " + source.Key);
                foreach (var source in woodlandTexts)
                    Assert.That(source.Key.text, Is.EqualTo(source.Value), "An imported Woodland TextAsset changed.");
                foreach (var source in crowdSources)
                    Assert.That(JsonUtility.ToJson(source.Key), Is.EqualTo(source.Value), "An imported crowd source was changed.");
                foreach (var collider in root.GetComponentsInChildren<Collider>())
                    Assert.That(collider.enabled, Is.False, "The detached presentation has no enabled gameplay collision.");

                environment.Shutdown();
                Assert.That(environment.Initialized, Is.False);
                saved.RequireRestored();
                Assert.That(camera.clearFlags, Is.EqualTo(cameraClear));
                Assert.That(camera.backgroundColor, Is.EqualTo(cameraBackground));
            }
            finally
            {
                environment?.Shutdown();
                Object.DestroyImmediate(root);
            }
        }

        private sealed class SceneWeatherState
        {
            public readonly Material Skybox = RenderSettings.skybox;
            private readonly bool fog = RenderSettings.fog;
            private readonly FogMode mode = RenderSettings.fogMode;
            private readonly float start = RenderSettings.fogStartDistance, end = RenderSettings.fogEndDistance, density = RenderSettings.fogDensity;
            private readonly Color fogColor = RenderSettings.fogColor, sky = RenderSettings.ambientSkyColor, equator = RenderSettings.ambientEquatorColor, ground = RenderSettings.ambientGroundColor;
            private readonly AmbientMode ambientMode = RenderSettings.ambientMode;
            public void RequireRestored()
            {
                Assert.That(RenderSettings.skybox, Is.SameAs(Skybox));
                Assert.That(RenderSettings.fog, Is.EqualTo(fog));
                Assert.That(RenderSettings.fogMode, Is.EqualTo(mode));
                Assert.That(RenderSettings.fogStartDistance, Is.EqualTo(start));
                Assert.That(RenderSettings.fogEndDistance, Is.EqualTo(end));
                Assert.That(RenderSettings.fogDensity, Is.EqualTo(density));
                Assert.That(RenderSettings.fogColor, Is.EqualTo(fogColor));
                Assert.That(RenderSettings.ambientMode, Is.EqualTo(ambientMode));
                Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(sky));
                Assert.That(RenderSettings.ambientEquatorColor, Is.EqualTo(equator));
                Assert.That(RenderSettings.ambientGroundColor, Is.EqualTo(ground));
            }
        }
    }
}

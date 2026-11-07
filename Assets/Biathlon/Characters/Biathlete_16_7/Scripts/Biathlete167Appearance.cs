using System;
using System.Collections.Generic;
using Biathlon.Career;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Projects the existing career appearance IDs onto the isolated hero.</summary>
    [DisallowMultipleComponent]
    public sealed class Biathlete167Appearance : MonoBehaviour
    {
        [Serializable]
        public sealed class SurfacePalette
        {
            public string sourceMaterial;
            public string optionGroup;
            public Material[] materials;
        }

        private sealed class Surface
        {
            public Renderer renderer;
            public Material[] materials;
            public int[] palettes;
        }

        [SerializeField] private Biathlete167Rig rig;
        [SerializeField] private GameObject headband, crewHair, sweptHair;
        [SerializeField] private SurfacePalette[] palettes;
        [SerializeField] private string profileJson;
        private Surface[] surfaces;
        private AppearanceProfile current;
        private Biathlete167Outfit outfit;
        public AppearanceProfile Current => current;

        public void Configure(Biathlete167Rig athlete, GameObject band, GameObject crew, GameObject swept,
            SurfacePalette[] surfacePalettes)
        {
            if (athlete == null || band == null || crew == null || swept == null || surfacePalettes == null)
                throw new ArgumentException("Complete hero appearance bindings are required.");
            rig = athlete;
            headband = band;
            crewHair = crew;
            sweptHair = swept;
            palettes = surfacePalettes;
            surfaces = null;
            var faces = AthleteFaceCatalog.Load();
            ApplyProfile(AthleteLookCatalog.Load().Edit(
                AppearanceProfile.Create(faces.profiles[0].id, faces.skin_ids[0], "natural"), lensId:"amber"));
        }

        private void Awake()
        {
            if (rig == null) return;
            outfit = GetComponent<Biathlete167Outfit>();
            if (outfit != null) outfit.NationChanged += ApplyNationalHeadband;
            ApplyProfile(AppearanceProfile.FromJson(profileJson));
        }

        private void OnDestroy()
        {
            if (outfit != null) outfit.NationChanged -= ApplyNationalHeadband;
        }

        private void ApplyNationalHeadband(string nation)
        {
            surfaces = null;
            ApplyProfile(current);
        }

        public void ApplyProfile(AppearanceProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            profile.Validate();
            var faces = AthleteFaceCatalog.Load();
            if (profile.IsLegacy)
            {
                faces.Resolve(profile.LegacyIdentity, out int face, out int skin);
                int body = (int)(AthleteFaceCatalog.Hash(profile.LegacyIdentity) % 3);
                profile = AppearanceProfile.Create(faces.profiles[face].id, faces.skin_ids[skin], faces.body_ids[body]);
            }
            int faceIndex = Array.FindIndex(faces.profiles, face => face.id == profile.FaceId);
            if (faceIndex < 0) throw new ArgumentException("Unknown face ID.");
            int skinIndex = faces.SkinIndex(profile.SkinId);
            faces.BodyIndex(profile.BodyId);
            var definition = AthleteLookCatalog.Load();
            profile = definition.Edit(profile);
            var selections = new int[palettes.Length];
            for (int i = 0; i < palettes.Length; i++)
            {
                string group = palettes[i].optionGroup;
                string id = group switch
                {
                    "hair_color" => profile.HairColorId,
                    "eye_color" => profile.EyeColorId,
                    "lens" => profile.LensId,
                    "gloves" => profile.GlovesId,
                    "boots" => profile.BootsId,
                    _ => null
                };
                selections[i] = group == "skin" ? skinIndex : definition.Index(group, id);
                if (palettes[i].materials == null || selections[i] >= palettes[i].materials.Length ||
                    palettes[i].materials[selections[i]] == null)
                    throw new InvalidOperationException("Incomplete appearance material palette.");
            }
            if (surfaces == null) CacheSurfaces();
            foreach (var surface in surfaces)
            {
                for (int slot = 0; slot < surface.materials.Length; slot++)
                {
                    int palette = surface.palettes[slot];
                    if (palette >= 0) surface.materials[slot] = palettes[palette].materials[selections[palette]];
                }
                surface.renderer.sharedMaterials = surface.materials;
            }
            foreach (var level in rig.Lods.GetLODs())
                foreach (var renderer in level.renderers)
                {
                    if (!(renderer is SkinnedMeshRenderer skin)) continue;
                    for (int i = 0; i < faces.profiles.Length; i++)
                    {
                        int shape = skin.sharedMesh.GetBlendShapeIndex(faces.profiles[i].ShapeName);
                        if (shape < 0) throw new InvalidOperationException("Face blend shape is missing.");
                        skin.SetBlendShapeWeight(shape, i == faceIndex ? 100 : 0);
                    }
                    int hidden = skin.sharedMesh.GetBlendShapeIndex("HeadwearHidden");
                    if (hidden < 0) throw new InvalidOperationException("Headwear blend shape is missing.");
                    skin.SetBlendShapeWeight(hidden, profile.HeadwearId == "cap" ? 0 : 100);
                }
            headband.SetActive(profile.HeadwearId == "headband");
            crewHair.SetActive(profile.HeadwearId != "cap" && profile.HairStyleId == "crew");
            sweptHair.SetActive(profile.HeadwearId != "cap" && profile.HairStyleId == "short_swept");
            var national = outfit != null ? outfit : GetComponent<Biathlete167Outfit>();
            if (national != null) headband.GetComponent<Renderer>().sharedMaterial = national.HeadwearMaterial;
            rig.SetEyewearVisible(profile.GogglesId != "absent");
            rig.PreviewEyewearRaised = profile.GogglesId == "raised";
            current = profile;
            profileJson = profile.ToJson();
            GetComponent<Biathlete167Motion>()?.RefreshAppearance();
        }

        private void CacheSurfaces()
        {
            var result = new List<Surface>();
            foreach (var renderer in rig.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                var roles = new int[materials.Length];
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    roles[slot] = -1;
                    for (int i = 0; i < palettes.Length; i++)
                        if (materials[slot].name == palettes[i].sourceMaterial ||
                            Array.IndexOf(palettes[i].materials, materials[slot]) >= 0)
                        {
                            roles[slot] = i;
                            break;
                        }
                }
                result.Add(new Surface { renderer = renderer, materials = materials, palettes = roles });
            }
            surfaces = result.ToArray();
        }
    }
}

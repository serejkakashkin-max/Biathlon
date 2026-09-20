using System;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    [Serializable]
    public sealed class AthleteDetailSettings
    {
        public int schema_version;
        public string id;
        public float far_distance_m, hysteresis_m, far_pose_hz, mesh_cell_m;
        public static AthleteDetailSettings Load()
        {
            var d = JsonUtility.FromJson<AthleteDetailSettings>(Resources.Load<TextAsset>("Biathlon/athlete_detail_v1").text);
            if (d == null || d.schema_version != 1 || string.IsNullOrWhiteSpace(d.id) ||
                !(d.far_distance_m > 0 && d.far_distance_m < 1000) || !(d.hysteresis_m > 0 && d.hysteresis_m < d.far_distance_m) ||
                !(d.far_pose_hz >= 10 && d.far_pose_hz <= 60) || !(d.mesh_cell_m > 0 && d.mesh_cell_m < .1f))
                throw new ArgumentException("Invalid athlete detail definition.");
            return d;
        }
    }

    /// <summary>Presentation detail only. Placement still updates every frame. External rigs retain
    /// their own mesh; the project-authored distant mesh is used only with its matching source.</summary>
    public sealed class AthleteDetailPresentation
    {
        private readonly AthleteDetailSettings settings;
        private readonly SkinnedMeshRenderer body;
        private readonly Mesh nearMesh, farMesh;
        private readonly AthleteSkinDetailBinding skinDetail;
        private readonly Renderer[] renderers;
        private readonly ShadowCastingMode[] shadows;
        private double lastPoseTime = -1;
        private AthletePresentationState previous;
        public bool Distant { get; private set; }
        public int PoseUpdates { get; private set; }
        public AthleteDetailPresentation(AthleteRigBinding rig)
        {
            settings = AthleteDetailSettings.Load();
            skinDetail = rig.GetComponent<AthleteSkinDetailBinding>();
            body = rig.GetComponentInChildren<SkinnedMeshRenderer>();
            if (body != null)
            {
                nearMesh = body.sharedMesh;
                if (nearMesh == Resources.Load<Mesh>("Biathlon/Athletes/Meshes/AthleteBody"))
                    farMesh = Resources.Load<Mesh>("Biathlon/Athletes/Meshes/AthleteBodyDistant");
            }
            renderers = rig.GetComponentsInChildren<Renderer>(true);
            shadows = new ShadowCastingMode[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) shadows[i] = renderers[i].shadowCastingMode;
        }
        public bool Prepare(float distance, AthleteAnimationFrame frame)
        {
            if (!float.IsFinite(distance) || distance < 0) throw new ArgumentOutOfRangeException(nameof(distance));
            var a = frame.Athlete;
            bool reset = lastPoseTime < 0 || frame.TimeSeconds < lastPoseTime || a.AthleteId != previous.AthleteId;
            if (skinDetail != null) skinDetail.Prepare(distance, a.ShootingActive, reset);
            bool distant = !a.ShootingActive && (reset ? distance >= settings.far_distance_m : Distant ?
                distance > settings.far_distance_m - settings.hysteresis_m : distance >= settings.far_distance_m + settings.hysteresis_m);
            bool changed = distant != Distant;
            if (changed)
            {
                Distant = distant;
                if (body != null && farMesh != null) body.sharedMesh = Distant ? farMesh : nearMesh;
                for (int i = 0; i < renderers.Length; i++) renderers[i].shadowCastingMode = Distant ? ShadowCastingMode.Off : shadows[i];
            }
            bool stateChanged = a.Phase != previous.Phase || a.ShootingPhase != previous.ShootingPhase || a.TuckActive != previous.TuckActive || a.FinalSprintActive != previous.FinalSprintActive;
            previous = a;
            bool update = !Distant || reset || changed || stateChanged || frame.TimeSeconds - lastPoseTime + 1e-8 >= 1.0 / settings.far_pose_hz;
            if (update) { lastPoseTime = frame.TimeSeconds; PoseUpdates++; }
            return update;
        }
    }
}

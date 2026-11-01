using System;
using Biathlon.Domain;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Labels the launch portal and the retained lap timing portal from authored geometry.</summary>
    public sealed class StadiumPortalPresentation : MonoBehaviour
    {
        [Serializable] private sealed class Settings
        {
            public int schema_version;
            public string id, start_text, timing_text;
            public float height_m, face_offset_m, character_size_m;
        }
        public void Initialize(VenueAuthoringPlan venue)
        {
            if (venue?.StartApproach?.LaunchDistanceMeters == null) return;
            var data = JsonUtility.FromJson<Settings>(Resources.Load<TextAsset>("Biathlon/stadium_portal_labels_v1").text);
            if (data == null || data.schema_version != 1 || string.IsNullOrWhiteSpace(data.id) ||
                string.IsNullOrWhiteSpace(data.start_text) ||
                !float.IsFinite(data.height_m) || data.height_m <= 0 || !float.IsFinite(data.face_offset_m) || data.face_offset_m <= 0 ||
                !float.IsFinite(data.character_size_m) || data.character_size_m <= 0)
                throw new ArgumentException("Invalid stadium portal labels.");
            Label(venue.Visual.Get(venue.StartApproach.LoopId).Sample(venue.StartApproach.LaunchDistanceMeters.Value), data.start_text);
            // The retained gantry receives its cumulative kilometre label from CourseTimingPresentation.
            void Label(TrackVisualPoint sample, string value)
            {
                var forward = new Vector3((float)sample.ForwardX, 0, (float)sample.ForwardZ);
                var center = new Vector3((float)sample.X, (float)sample.Y + data.height_m, (float)sample.Z);
                // The label faces arrivals; a second transparent TextMesh would show
                // mirrored letters through the first one at grazing camera angles.
                foreach (int face in new[] { -1 })
                {
                    var go = new GameObject("Stadium portal label"); go.transform.SetParent(transform, false);
                    go.transform.position = center + forward * (face * data.face_offset_m);
                    go.transform.rotation = Quaternion.LookRotation(-face * forward, Vector3.up);
                    var text = go.AddComponent<TextMesh>(); text.text = value; text.fontSize = 64;
                    text.characterSize = data.character_size_m; text.anchor = TextAnchor.MiddleCenter;
                    text.alignment = TextAlignment.Center; text.color = Color.white;
                    var renderer = text.GetComponent<MeshRenderer>(); renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }
    }
}

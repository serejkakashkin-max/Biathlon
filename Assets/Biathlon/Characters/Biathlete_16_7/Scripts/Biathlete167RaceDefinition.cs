using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Player-safe bindings and cosmetic proportions. No race tuning.</summary>
    public sealed class Biathlete167RaceDefinition : ScriptableObject
    {
        [Serializable] public sealed class BodyScale { public string id; public float width, depth; }
        [Serializable] public sealed class SkiStyle { public string id; public Material material; }
        public int schema_version = 1;
        public string id = "m167_race_visual_v1";
        public Biathlete167Rig athlete;
        public Material bibMaterial;
        public BodyScale[] bodies;
        public SkiStyle[] skis;
        public float narrowShoulderScale = .96f, wideShoulderScale = 1.04f;
        public Vector4 bodyHeightBand = new Vector4(.65f,.98f,1.38f,1.52f);
        public Vector2 bodySideBand = new Vector2(.13f,.26f);
        public float maximumWidthMorph = .12f, maximumDepthMorph = .1f;
        public const string ResourcePath = "Biathlon/Athletes/M167/RaceVisual";

        public void Validate()
        {
            if(schema_version!=1||id!="m167_race_visual_v1"||athlete==null||bibMaterial==null||bodies==null||skis==null||
                !(narrowShoulderScale>.85f&&narrowShoulderScale<=1&&wideShoulderScale>=1&&wideShoulderScale<1.15f))
                throw new InvalidOperationException("Incomplete M16.7 race visual bindings.");
            if(!(bodyHeightBand.x<bodyHeightBand.y&&bodyHeightBand.y<bodyHeightBand.z&&bodyHeightBand.z<bodyHeightBand.w&&
                bodySideBand.x>0&&bodySideBand.x<bodySideBand.y&&maximumWidthMorph>0&&maximumWidthMorph<.2f&&maximumDepthMorph>0&&maximumDepthMorph<.2f))
                throw new InvalidOperationException("Invalid cosmetic surface morph envelope.");
            foreach(var body in bodies)if(string.IsNullOrWhiteSpace(body.id)||!(body.width>.8f&&body.width<1.2f&&body.depth>.8f&&body.depth<1.2f))
                throw new InvalidOperationException("Invalid cosmetic body proportions.");
            foreach(var ski in skis)if(string.IsNullOrWhiteSpace(ski.id)||ski.material==null)
                throw new InvalidOperationException("Incomplete saved ski appearance.");
        }
        public BodyScale Body(string value) => Array.Find(bodies,b=>b.id==value) ?? throw new ArgumentException("Unknown cosmetic body ID: "+value);
        public Material Ski(string value) => Array.Find(skis,s=>s.id==value)?.material ?? throw new ArgumentException("Unknown saved ski ID: "+value);
    }
}

using System;
using System.Collections.Generic;
using Biathlon.Career;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Bounded shared prop materials are selected before equipment instancing registration.</summary>
    public sealed class CareerSkiAppearance : MonoBehaviour
    {
        private static SkiAppearanceCatalog catalog;
        private static readonly Dictionary<(Material, string, bool), Material> materials = new Dictionary<(Material, string, bool), Material>();
        public const string ResourcePath = "Biathlon/UI/ski_appearance_v1";
        public static SkiAppearanceCatalog Catalog => catalog ?? (catalog = SkiAppearanceCatalog.FromJson(Resources.Load<TextAsset>(ResourcePath).text));
        public string SelectedEquipmentId { get; private set; }
        public void Apply(AthleteRigBinding rig, string equipmentId)
        {
            if (rig == null) throw new ArgumentNullException(nameof(rig));
            if (SelectedEquipmentId == equipmentId) return;
            var definition = Catalog.Get(equipmentId);
            ApplySocket(rig.Socket(AthleteSocket.LeftSkiBinding), definition);
            ApplySocket(rig.Socket(AthleteSocket.RightSkiBinding), definition);
            SelectedEquipmentId = equipmentId;
        }
        private static void ApplySocket(Transform socket, SkiAppearanceDefinition definition)
        {
            foreach (var renderer in socket.GetComponentsInChildren<MeshRenderer>(true))
            {
                var source = renderer.sharedMaterials;
                // SkiComplete's explicit material contract: deck, binding, tip graphic. Never identify props by scene names.
                if (source.Length != 3) throw new InvalidOperationException("Ski presentation expects deck, binding and tip graphic material slots.");
                source[0] = Variant(source[0], definition, false);
                source[2] = Variant(source[2], definition, true);
                renderer.sharedMaterials = source;
            }
        }
        private static Material Variant(Material source, SkiAppearanceDefinition definition, bool accent)
        {
            // A reused rig may be recolored; unwrap our own previous variant to avoid growing a clone chain.
            foreach (var entry in materials) if (entry.Value == source) { source = entry.Key.Item1; break; }
            var key = (source, definition.Id, accent);
            if (materials.TryGetValue(key, out var existing) && existing != null) return existing;
            if (!ColorUtility.TryParseHtmlString(accent ? definition.AccentColorHex : definition.BaseColorHex, out var color)) throw new ArgumentException("Invalid selected ski color.");
            var material = new Material(source) { name = source.name + " " + definition.Id, hideFlags = HideFlags.DontSave, enableInstancing = true };
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            materials[key] = material; return material;
        }
    }
}

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167GarmentTests
    {
        private const string Root="Assets/Biathlon/Characters/Biathlete_16_7";
        [Serializable] private sealed class PrintReceipt{public string method,texture;public int resolution,pages,standalone_bib_glyph_meshes;}
        [Serializable] private sealed class CoverageReceipt{public int covered_suit_triangles_removed;public bool reference_in_runtime;}
        [Serializable] private sealed class ContactReceipt{public float tuck_pelvis_clearance_lift_m;}
        [Serializable] private sealed class Receipt{public string garment_detail_definition_sha256,garment_skinning;public PrintReceipt bib_print;public CoverageReceipt garment_coverage;}
        [Test] public void BibArtworkUsesTheFabricTextureRatherThanFloatingGlyphs()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7_Natural.prefab");
            var rig=prefab.GetComponent<Biathlete167Rig>();
            var materials=rig.Lods.GetLODs()[0].renderers.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
            var bib=materials.SingleOrDefault(m=>m.name=="PULSE_Bib_Printed_Textile");
            Assert.That(bib,Is.Not.Null,"Bib ink must be on its fabric surface, not another intersecting skinned mesh.");
            var texture=bib.GetTexture("_BaseMap");Assert.That(texture,Is.Not.Null);
            Assert.That(texture.name,Is.EqualTo("Bib_PULSE_Print_BaseColor_4K"));
            Assert.That(texture.width,Is.EqualTo(4096));Assert.That(texture.height,Is.EqualTo(4096));
            Assert.That(materials.Any(m=>m.name=="Printed_Graphite"),Is.False,"Standalone bib glyph material must not remain in runtime geometry.");
        }
        [Test] public void BibAndSuitRetainUvAndBoundedSkinWeights()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7_Natural.prefab");
            var skin=prefab.GetComponent<Biathlete167Rig>().Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().Single();
            var mesh=skin.sharedMesh;var weights=mesh.boneWeights;var uv=mesh.uv;
            foreach(string name in new[]{"PULSE_Bib_Printed_Textile","PULSE_Natural_Tailored_Textile"})
            {
                int sub=Array.FindIndex(skin.sharedMaterials,m=>m.name==name);Assert.That(sub,Is.GreaterThanOrEqualTo(0));
                foreach(int vertex in mesh.GetTriangles(sub).Distinct())
                {
                    var weight=weights[vertex];Assert.That(weight.weight0+weight.weight1+weight.weight2+weight.weight3,Is.EqualTo(1).Within(.00001f));
                    Assert.That(float.IsFinite(uv[vertex].x)&&float.IsFinite(uv[vertex].y),Is.True);
                }
            }
        }
        [Test] public void GarmentExportMatchesItsVersionedDefinition()
        {
            var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(Root+"/Documentation/natural-asset-manifest.json"));
            Assert.That(receipt.bib_print,Is.Not.Null);Assert.That(receipt.bib_print.method,Is.EqualTo("shared_fabric_uv_atlas"));
            Assert.That(receipt.bib_print.pages,Is.EqualTo(2));Assert.That(receipt.bib_print.standalone_bib_glyph_meshes,Is.Zero);
            Assert.That(receipt.garment_skinning,Is.EqualTo("shared_partition_vertices_and_normalized_weights"));
            Assert.That(receipt.garment_coverage,Is.Not.Null);Assert.That(receipt.garment_coverage.covered_suit_triangles_removed,Is.GreaterThan(100));
            Assert.That(receipt.garment_coverage.reference_in_runtime,Is.False);
            using(var sha=SHA256.Create())Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Root+"/Documentation/garment-detail-v1.json"))).Replace("-","").ToLowerInvariant(),Is.EqualTo(receipt.garment_detail_definition_sha256));
        }
        [Test] public void AnatomicalTuckClearanceRaisesThePelvisWithPlantedSkiBindings()
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7_Natural.prefab"));
            try
            {
                var rig=go.GetComponent<Biathlete167Rig>();var position=go.transform.position;var rotation=go.transform.rotation;
                var definition=JsonUtility.FromJson<ContactReceipt>(rig.ContactDefinition.text);
                using(var motion=new Biathlete167MainMotion(rig))
                {
                    motion.State=SkiMotionState.Tuck;motion.ContactAdaptationEnabled=false;motion.Evaluate(.25f);
                    var pelvis=rig.Bone(HumanBodyBones.Hips).position;
                    motion.ContactAdaptationEnabled=true;motion.Evaluate(.25f);
                    Assert.That(Vector3.Dot(rig.Bone(HumanBodyBones.Hips).position-pelvis,go.transform.up),Is.EqualTo(definition.tuck_pelvis_clearance_lift_m).Within(.00001f));
                    Assert.That(motion.PelvisClearanceLiftMeters,Is.GreaterThan(.03f));
                    Assert.That(motion.MaximumBindingResidualMeters,Is.LessThan(.003f));
                    Assert.That(go.transform.position,Is.EqualTo(position));Assert.That(Quaternion.Angle(go.transform.rotation,rotation),Is.LessThan(.001f));
                }
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
#endif

using Biathlon.Career;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class CareerRaceAppearanceTests
    {
        [Test] public void SelectedRedSkisUseTheShopPaletteAndSharedInstancedMaterialsWithoutTouchingBindings()
        {
            var a = AthleteRigFactory.Create(null); var b = AthleteRigFactory.Create(null);
            try
            {
                var original = a.Socket(AthleteSocket.LeftSkiBinding).GetComponentInChildren<MeshRenderer>().sharedMaterials;
                var sourceColor = original[0].GetColor("_BaseColor");
                var aa = a.gameObject.AddComponent<CareerSkiAppearance>(); var bb = b.gameObject.AddComponent<CareerSkiAppearance>();
                aa.Apply(a, "ski_glide"); bb.Apply(b, "ski_glide");
                var left = a.Socket(AthleteSocket.LeftSkiBinding).GetComponentInChildren<MeshRenderer>().sharedMaterials;
                var right = a.Socket(AthleteSocket.RightSkiBinding).GetComponentInChildren<MeshRenderer>().sharedMaterials;
                var other = b.Socket(AthleteSocket.LeftSkiBinding).GetComponentInChildren<MeshRenderer>().sharedMaterials;
                ColorUtility.TryParseHtmlString(CareerSkiAppearance.Catalog.Get("ski_glide").BaseColorHex, out var red);
                // Material colour round-trips may differ by one float ULP from parsed sRGB.
                var actualRed = left[0].GetColor("_BaseColor");
                Assert.That(actualRed.r, Is.EqualTo(red.r).Within(1e-6f));
                Assert.That(actualRed.g, Is.EqualTo(red.g).Within(1e-6f));
                Assert.That(actualRed.b, Is.EqualTo(red.b).Within(1e-6f));
                Assert.That(actualRed.a, Is.EqualTo(red.a).Within(1e-6f));
                Assert.That(right[0], Is.SameAs(left[0])); Assert.That(other[0], Is.SameAs(left[0]));
                Assert.That(left[1], Is.SameAs(original[1]), "Binding/rubber is not painted over.");
                Assert.That(original[0].GetColor("_BaseColor"), Is.EqualTo(sourceColor), "Source asset must remain untouched.");
                aa.Apply(a, "ski_neutral"); aa.Apply(a, "ski_glide");
                Assert.That(a.Socket(AthleteSocket.LeftSkiBinding).GetComponentInChildren<MeshRenderer>().sharedMaterials[0], Is.SameAs(left[0]));
                long before = System.GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 100; i++) aa.Apply(a, "ski_glide");
                Assert.That(System.GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero, "Unchanged appearance does no per-frame work.");
            }
            finally { Object.DestroyImmediate(a.gameObject); Object.DestroyImmediate(b.gameObject); }
        }
        [Test] public void CombinedLeaderBibPreservesNumberAndDoesNotTintTheSuit()
        {
            var rig = AthleteRigFactory.Create(null);
            try
            {
                var bib = rig.GetComponent<AthleteBibPresentation>(); bib.SetNumber(12);
                var body = rig.GetComponentInChildren<SkinnedMeshRenderer>(); var suit = body.sharedMaterials[1];
                var block = new MaterialPropertyBlock(); bib.SetLeader(CareerLeaderBib.Total | CareerLeaderBib.Discipline);
                body.GetPropertyBlock(block, 2);
                Assert.That(block.GetFloat("_BibNumber"), Is.EqualTo(12)); Assert.That(block.GetFloat("_ColorSplit"), Is.EqualTo(1));
                Assert.That(block.GetColor("_BaseColor").r, Is.GreaterThan(.9f)); Assert.That(block.GetColor("_SecondaryColor").g, Is.LessThan(.4f));
                Assert.That(bib.LeaderRole, Is.EqualTo(CareerLeaderBib.Total | CareerLeaderBib.Discipline));
                body.GetPropertyBlock(block, 1); Assert.That(block.isEmpty, Is.True); Assert.That(body.sharedMaterials[1], Is.SameAs(suit));
                bib.SetLeader(CareerLeaderBib.None); body.GetPropertyBlock(block, 2);
                Assert.That(block.GetFloat("_ColorSplit"), Is.Zero); Assert.That(block.GetFloat("_BibNumber"), Is.EqualTo(12));
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }
    }
}

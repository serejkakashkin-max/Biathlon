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
        [Test] public void CountryKitIsSharedAcrossNumbersAndLeaderChangesWithoutAllocatingPerFrame()
        {
            var a=AthleteRigFactory.Create(null);var b=AthleteRigFactory.Create(null);
            try
            {
                var aa=a.GetComponent<AthleteAppearancePresentation>();var bb=b.GetComponent<AthleteAppearancePresentation>();
                a.GetComponent<AthleteBibPresentation>().SetNumber(2);b.GetComponent<AthleteBibPresentation>().SetNumber(30);
                aa.ApplyNation("NOR");bb.ApplyNation("NOR");
                var ar=a.GetComponentInChildren<SkinnedMeshRenderer>();var br=b.GetComponentInChildren<SkinnedMeshRenderer>();
                var suit=ar.sharedMaterials[1];Assert.That(br.sharedMaterials[1],Is.SameAs(suit));
                var bib=a.GetComponent<AthleteBibPresentation>();
                foreach(var role in new[]{CareerLeaderBib.Total,CareerLeaderBib.Discipline,CareerLeaderBib.Total|CareerLeaderBib.Discipline,CareerLeaderBib.None})
                {bib.SetLeader(role);bib.SetNumber(13);Assert.That(ar.sharedMaterials[1],Is.SameAs(suit));}
                var block=new MaterialPropertyBlock();ar.GetPropertyBlock(block,2);Assert.That(block.GetFloat("_BibNumber"),Is.EqualTo(13));
                ar.GetPropertyBlock(block,1);Assert.That(block.isEmpty,Is.True,"Keep shared suit materials and SRP batching.");
                long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++){aa.ApplyNation("NOR");bib.SetLeader(CareerLeaderBib.None);}
                Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                aa.ApplyNation("FRA");Assert.That(ar.sharedMaterials[1].GetColor("_KitColor"),Is.Not.EqualTo(suit.GetColor("_KitColor")));
                aa.ApplyNation("NOR");Assert.That(ar.sharedMaterials[1],Is.SameAs(suit));
            }
            finally{Object.DestroyImmediate(a.gameObject);Object.DestroyImmediate(b.gameObject);}
        }
        [TestCase("career_roster_v1")][TestCase("career_roster_v2")]
        public void CountryPaletteCoversEverySavedRosterNationAndKeepsUnknownCountriesNeutral(string rosterId)
        {
            var roster=CareerRosterDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/"+rosterId));
            var colors=new System.Collections.Generic.HashSet<Color>();
            Assert.That(AthleteAppearancePresentation.NationCount,Is.GreaterThanOrEqualTo(roster.NationCount),"The shared palette must cover both the legacy and expanded saved rosters.");
            var rig=AthleteRigFactory.Create(null);
            try
            {
                for(int i=0;i<roster.NationCount;i++)
                {
                    string nation=roster.Nation(i);Assert.That(colors.Add(AthleteAppearancePresentation.NationColor(nation)),Is.True);
                    rig.GetComponent<AthleteAppearancePresentation>().ApplyNation(nation);
                    var color=rig.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials[1].GetColor("_KitColor");
                    Assert.That(Vector4.Distance(color,AthleteAppearancePresentation.NationColor(nation)),Is.LessThan(1e-5));
                }
                Assert.That(colors.Contains(AthleteAppearancePresentation.NationColor("UNK")),Is.False);
                Assert.That(AthleteAppearancePresentation.NationColor("UNK"),Is.EqualTo(AthleteAppearancePresentation.NationColor("XYZ")));
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
    }
}

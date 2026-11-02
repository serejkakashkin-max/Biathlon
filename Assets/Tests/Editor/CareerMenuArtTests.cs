using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Tests
{
    public sealed class CareerMenuArtTests
    {
        [TestCase("training_0")][TestCase("training_1")][TestCase("training_2")]
        [TestCase("training_3")][TestCase("training_4")][TestCase("training_5")]
        [TestCase("ski_neutral")][TestCase("ski_glide")][TestCase("ski_soft_specialist")]
        [TestCase("ski_cold_race")][TestCase("ski_fresh_float")][TestCase("ski_wet_channel")]
        [TestCase("ski_ice_edge")][TestCase("ski_soft_control")]
        [TestCase("rifle_neutral_v1")][TestCase("rifle_precision")][TestCase("rifle_agile")][TestCase("rifle_endurance")]
        public void ShippingArtworkLoadsWithBoundedNonReadableUiTexture(string id)
        {
            var texture=Resources.Load<Texture2D>(CareerMenuArt.ResourceRoot+id);
            Assert.That(texture,Is.Not.Null,id);
            Assert.That(texture.width,Is.InRange(768,1024));
            Assert.That(texture.height/(float)texture.width,Is.EqualTo(2f/3).Within(.01));
            Assert.That(texture.isReadable,Is.False);
            Assert.That(texture.mipmapCount,Is.EqualTo(1));
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert.That(importer.maxTextureSize,Is.EqualTo(1024));
            Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.CompressedHQ));
            var image=CareerMenuArt.Create(id,"art");
            Assert.That(image.image,Is.SameAs(texture));
            Assert.That(image.scaleMode,Is.EqualTo(ScaleMode.ScaleToFit),"No clipped skis, rifle barrels or athletes.");
            Assert.That(image.pickingMode,Is.EqualTo(PickingMode.Ignore),"Artwork must not steal card input.");
        }
    }
}

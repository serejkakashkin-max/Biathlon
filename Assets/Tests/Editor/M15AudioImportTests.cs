using System;
using System.IO;
using System.Security.Cryptography;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
namespace Biathlon.Tests
{
    public sealed class M15AudioImportTests
    {
        [Serializable]private sealed class Source {public string id,sha256;}
        [Serializable]private sealed class Sources {public Source[] clips;}
        [Serializable]private sealed class CrowdSources {public int schema_version;public string id,license;public Source[] clips;}
        [Test]public void NativeMixerHasEveryBusAndVoiceBudgetFitsEngine()
        {
            var mixer=Resources.Load<AudioMixer>("Biathlon/Audio/Biathlon");Assert.That(mixer,Is.Not.Null);
            foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))Assert.That(mixer.FindMatchingGroups(bus.ToString()).Length,Is.EqualTo(1));
            var d=BiathlonAudioDefinition.Load();Assert.That(d.loop_voices+d.transient_voices,Is.LessThanOrEqualTo(AudioSettings.GetConfiguration().numRealVoices));
        }
        [Test]public void AllShortAndLoopClipsPreloadPcmWithoutRuntimeDecoding()
        {
            foreach(var c in BiathlonAudioDefinition.Load().cues)foreach(string clip in c.clips)
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath("Assets/Resources/Biathlon/Audio/"+clip+".wav");
                Assert.That(importer.defaultSampleSettings.preloadAudioData,Is.True,clip);
                Assert.That(importer.defaultSampleSettings.loadType,Is.EqualTo(AudioClipLoadType.DecompressOnLoad),clip);
                Assert.That(importer.defaultSampleSettings.compressionFormat,Is.EqualTo(AudioCompressionFormat.PCM),clip);
                Assert.That(importer.loadInBackground,Is.False);
            }
        }
        [Test]public void OriginalSoundSourcesMatchProvenanceHashes()
        {
            var sources=JsonUtility.FromJson<Sources>(Resources.Load<TextAsset>("Biathlon/Audio/sources").text);
            Assert.That(sources.clips.Length,Is.EqualTo(24));using(var hash=SHA256.Create())foreach(var clip in sources.clips)
            {
                string path="Assets/Resources/Biathlon/Audio/"+clip.id+".wav";
                Assert.That(BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant(),Is.EqualTo(clip.sha256));
            }
        }
        [Test]public void ImportedHumanCrowdReactionsHaveCc0ProvenanceAndMatchingPreparedHashes()
        {
            var sources=JsonUtility.FromJson<CrowdSources>(Resources.Load<TextAsset>("Biathlon/Audio/crowd_reactions_sources").text);
            Assert.That(sources.schema_version,Is.EqualTo(1));Assert.That(sources.id,Is.EqualTo("m16_2_player_crowd_sources"));Assert.That(sources.license,Is.EqualTo("CC0-1.0"));
            Assert.That(sources.clips.Length,Is.EqualTo(4));using(var hash=SHA256.Create())foreach(var clip in sources.clips)
                Assert.That(BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes("Assets/Resources/Biathlon/Audio/"+clip.id+".wav"))).Replace("-","").ToLowerInvariant(),Is.EqualTo(clip.sha256));
        }
    }
}

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
    }
}

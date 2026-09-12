#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class GreyboxVenueTests
    {
        [UnityTest] public IEnumerator GeneratedSceneHasTrackRangePenaltyAndCameraWithoutMissingMeshes()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/GreyboxVenue.unity",new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;int loops=0,banks=0;bool penalty=false,camera=false;
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>()){Assert.That(filter.sharedMesh,Is.Not.Null);if(filter.sharedMesh.name.StartsWith("stadium_visual_v1")){loops++;penalty|=filter.sharedMesh.name.EndsWith("penalty_150");}}
                foreach(var t in root.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Target bank "))banks++;
                camera|=root.GetComponentInChildren<Camera>()!=null;
            }
            Assert.That(loops,Is.EqualTo(32));Assert.That(banks,Is.EqualTo(30));Assert.That(penalty&&camera,Is.True);yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
#endif

using System.Collections;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class ChaseCameraTests
    {
        private static ChaseCameraSettings Settings()=>ChaseCameraSettings.FromJson(Resources.Load<TextAsset>("Biathlon/chase_camera_v1").text);
        [UnityTest] public IEnumerator CameraFollowsWithoutMovingTargetAndAvoidsVisualObstacles()
        {
            var athlete=new GameObject("camera_target");athlete.transform.position=new Vector3(0,1,0);var camera=new GameObject("camera_test").AddComponent<AthleteChaseCamera>();camera.enabled=false;camera.Follow(athlete.transform,Settings(),1<<29);camera.Render(0);
            Assert.That(camera.transform.position.z,Is.EqualTo(-9).Within(.001));Assert.That(athlete.transform.position,Is.EqualTo(new Vector3(0,1,0)));
            athlete.transform.position=new Vector3(10,1,0);camera.Render(.1f);Assert.That(camera.transform.position.x,Is.InRange(0.01f,9.99f));
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.layer=29;obstacle.transform.position=new Vector3(10,3,-4);obstacle.transform.localScale=new Vector3(10,10,1);Physics.SyncTransforms();camera.Follow(athlete.transform,Settings(),1<<29);camera.Render(0);Assert.That(camera.transform.position.z,Is.GreaterThan(-3.5f));
            Object.Destroy(obstacle);yield return null;var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.layer=29;ground.transform.position=new Vector3(10,4,-9);ground.transform.localScale=new Vector3(4,2,4);Physics.SyncTransforms();camera.Follow(athlete.transform,Settings(),1<<29);camera.Render(0);Assert.That(Vector3.Distance(camera.transform.position,ground.GetComponent<Collider>().ClosestPoint(camera.transform.position)),Is.GreaterThanOrEqualTo(Settings().collision_radius_m));Assert.That(camera.transform.position.y,Is.GreaterThan(athlete.transform.position.y));
            Assert.Catch(()=>camera.Render(float.NaN));Object.Destroy(ground);Object.Destroy(camera.gameObject);Object.Destroy(athlete);yield return null;
        }
    }
}

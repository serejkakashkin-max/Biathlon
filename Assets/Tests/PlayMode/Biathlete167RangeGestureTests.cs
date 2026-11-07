#if UNITY_EDITOR
using System.Collections;
using System.Globalization;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167RangeGestureTests
    {
        private const string Root="Assets/Biathlon/Characters/Biathlete_16_7";
        private const string Evidence="artifacts/m16-7-upper-reach-20261107";
        private Biathlete167Rig rig;private Biathlete167Motion motion;
        private readonly Vector3[] forearmInHand=new Vector3[2];
        [SetUp] public void Create(){rig=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7_Natural.prefab")).GetComponent<Biathlete167Rig>();motion=rig.gameObject.AddComponent<Biathlete167Motion>();motion.Initialize(rig,true);for(int side=0;side<2;side++){var hand=rig.Bone(side==0?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);forearmInHand[side]=hand.InverseTransformDirection(hand.position-hand.parent.position);}}
        [TearDown] public void Cleanup(){if(rig!=null)Object.DestroyImmediate(rig.gameObject);}
        [Test] public void GlidingElbowsRemainLowAndCloseInsteadOfHandsOnHips()
        {
            void Check(Biathlete167Action action,float phase)
            {
                motion.EvaluateAction(action,phase);
                foreach(bool right in new[]{false,true})
                {
                    var shoulder=rig.Bone(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm).position;var elbow=rig.Bone(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm).position;var delta=elbow-shoulder;
                    Assert.That(Vector3.Dot(delta,rig.transform.right)*(right?1:-1),Is.LessThan(.12f),action+" phase="+phase+" elbow="+right+": no raised side-wing upper arm.");
                    Assert.That(Vector3.Dot(delta,rig.transform.up),Is.LessThan(-.15f),"Coast with the elbow below the shoulder, rather than holding the upper arm horizontally.");
                }
            }
            for(int sample=0;sample<=64;sample++)Check(Biathlete167Action.RangeEntry,sample/64f*.3f);
            Check(Biathlete167Action.RangeEntry,1);Check(Biathlete167Action.EnterStanding,0);Check(Biathlete167Action.EnterProne,0);
        }
        [Test] public void ApproachWristsStayAlignedWhenTheElbowsMoveCloser()
        {
            for(int phase=0;phase<=128;phase++)
            {motion.EvaluateAction(Biathlete167Action.RangeEntry,phase/128f);for(int side=0;side<2;side++){var hand=rig.Bone(side==0?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);float bend=Vector3.Angle(hand.TransformDirection(forearmInHand[side]),hand.position-hand.parent.position);Assert.That(bend,Is.LessThanOrEqualTo(25.1f),"Keep the actual wrist aligned in the held/free handover too: phase="+phase+" side="+side);}}
        }
        [TestCase(Biathlete167Action.EnterStanding),TestCase(Biathlete167Action.EnterProne)]
        public void RemovalPalmRisesBeforeItPassesBehindTheShoulder(Biathlete167Action action)
        {
            bool crossed=false;float highest=float.NegativeInfinity;
            for(int sample=0;sample<=128;sample++)
            {
                motion.EvaluateAction(action,sample/128f*.45f);var palm=rig.GripContact(true).position;var shoulder=rig.Bone(HumanBodyBones.RightUpperArm).position;var head=rig.Bone(HumanBodyBones.Head).position;
                float height=Vector3.Dot(palm-head,rig.transform.up);highest=Mathf.Max(highest,height);
                if(!crossed&&Vector3.Dot(palm-shoulder,rig.transform.forward)<-.04f)
                {crossed=true;Assert.That(height,Is.GreaterThan(.015f),action+" phase="+sample+"/128: the hand first goes above the right shoulder/head, then back to the barrel, not up from behind the armpit.");}
            }
            Assert.That(crossed,Is.True,"Reach the carried barrel behind the shoulder.");Assert.That(highest,Is.GreaterThan(.06f),"The palm follows a visibly raised arc before settling on the barrel.");
        }
        [UnityTest] public IEnumerator CaptureActualUpperReachAndGlidingElbows()
        {
            var studio=Object.FindAnyObjectByType<Biathlete167Studio>();if(studio==null){yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Root+"/Preview/Biathlete_16_7_Preview.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;studio=Object.FindAnyObjectByType<Biathlete167Studio>();}
            if(rig==null)Create();studio.Rig.gameObject.SetActive(false);var go=new GameObject("Upper reach inspection camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(studio.View);camera.enabled=false;camera.fieldOfView=36;Directory.CreateDirectory(Evidence+"/native");
            try{using(var target=new BenchmarkRenderTarget(1200,1000))foreach(var action in new[]{Biathlete167Action.RangeEntry,Biathlete167Action.EnterStanding,Biathlete167Action.EnterProne})foreach(float phase in action==Biathlete167Action.RangeEntry?new[]{0,.125f,.25f,1f}:new[]{.08f,.14f,.20f,.26f,.32f,.38f,.45f,.70f,1f})
            {
                motion.EvaluateAction(action,phase);bool low=action==Biathlete167Action.EnterProne&&phase>.8f;var focus=new Vector3(0,low?.3f:1.0f,0);
                foreach(var view in new[]{("side",new Vector3(4.4f,.2f,0)),("front",new Vector3(.2f,.2f,4.4f)),("rear",new Vector3(1.6f,.25f,-4.1f))})
                {camera.transform.position=rig.transform.TransformPoint(focus+view.Item2);camera.transform.LookAt(rig.transform.TransformPoint(focus));yield return null;target.Render(camera);yield return new WaitForEndOfFrame();target.Render(camera);yield return new WaitForEndOfFrame();target.SavePng(Evidence+"/native/"+action+"-"+phase.ToString("F3",CultureInfo.InvariantCulture)+"-"+view.Item1+".png");}
            }}finally{studio.Rig.gameObject.SetActive(true);Object.Destroy(go);motion.Stop();}
        }
    }
}
#endif

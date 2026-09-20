using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public sealed class BroadcastBenchmarkPreview : MonoBehaviour
    {
        public Camera View; public Transform Subject;
        public string Shot="sector";public double SampleTime;
        public BroadcastCameraDirector Director {get;private set;}
        private void Start(){Director=gameObject.AddComponent<BroadcastCameraDirector>();Director.Initialize(View,BroadcastShotDefinition.Load());Director.Bind("athlete",Subject);}
        private void LateUpdate(){if(Director!=null)Director.Render(Shot,SampleTime);}
    }
}

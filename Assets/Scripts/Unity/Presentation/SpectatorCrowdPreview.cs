using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public sealed class SpectatorCrowdPreview : MonoBehaviour
    {
        public StadiumArtPreview Scene;public SpectatorSeating Seating;public bool Animate=true;public float FrozenTime=12;
        public SpectatorCrowd Crowd {get;private set;}
        private void Start(){Crowd=gameObject.AddComponent<SpectatorCrowd>();Crowd.Initialize(Seating);}
        private void LateUpdate()=>Crowd.Render(Scene.View,Scene.Tier,Animate?Time.time:FrozenTime);
    }
}

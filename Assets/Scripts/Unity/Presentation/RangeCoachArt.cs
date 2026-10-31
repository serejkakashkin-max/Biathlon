using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Offline-baked approved athlete mesh. No Animator, skinning or frame callbacks.</summary>
    public sealed class RangeCoachArt:MonoBehaviour
    {
        [SerializeField] private Vector3 eye;
        [SerializeField] private Vector3 notebook;
        public Vector3 Eye=>transform.TransformPoint(eye);
        public Vector3 Notebook=>transform.TransformPoint(notebook);
        public void Configure(Vector3 localEye,Vector3 localNotebook){eye=localEye;notebook=localNotebook;}
    }
}

using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Explicit replaceable prop references; all prop axes are asset-local.</summary>
    public sealed class AthleteEquipmentBinding : MonoBehaviour
    {
        [SerializeField] private Transform rifle, leftPole, rightPole;
        public Transform Rifle => rifle;
        public Transform LeftPole => leftPole;
        public Transform RightPole => rightPole;
        public void Configure(Transform weapon, Transform left, Transform right)
        {
            rifle = weapon; leftPole = left; rightPole = right; Validate();
        }
        public void Validate()
        {
            if (rifle == null || leftPole == null || rightPole == null || rifle == leftPole || rifle == rightPole || leftPole == rightPole ||
                !rifle.IsChildOf(transform) || !leftPole.IsChildOf(transform) || !rightPole.IsChildOf(transform))
                throw new InvalidOperationException("Equipment requires three distinct props belonging to the athlete.");
        }
    }
}

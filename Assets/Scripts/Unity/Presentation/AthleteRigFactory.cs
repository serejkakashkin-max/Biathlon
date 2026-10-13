using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    public static class AthleteRigFactory
    {
        public const string HeroResourcePath = "Biathlon/Athletes/G2Hero/HeroAthlete";
        public static AthleteRigBinding Create(Transform parent)
        {
            var hero = Resources.Load<AthleteRigBinding>(HeroResourcePath);
            var rig = hero != null ? Object.Instantiate(hero, parent, false) : OriginalAthleteRigBuilder.Create(parent);
            if (rig.GetComponent<AthleteHybridLocomotionDriver>() == null) rig.gameObject.AddComponent<AthleteHybridLocomotionDriver>();
            return rig;
        }
    }
}

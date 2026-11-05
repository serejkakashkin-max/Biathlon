using System.Collections;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public sealed class FirstRangeRevealProfileTests
    {
        [UnityTest,Timeout(240000)] public IEnumerator ActualFirstRangeBoardProfile(){yield return RuntimeRangeRevealReview.Collect("artifacts/m16-6/turn-board/native-profile");}
    }
}

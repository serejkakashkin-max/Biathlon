using Biathlon.Unity.Presentation;
using NUnit.Framework;

namespace Biathlon.PlayModeTests
{
    public class StandaloneDisplayStartupTests
    {
        [Test] public void DirectExecutableLaunchDefaultsToDesktopFullscreen()
        {
            Assert.That(StandaloneDisplayStartup.UseDesktopFullscreen(new[]{"Biathlon.exe"}),Is.True);
        }
        [Test] public void ReviewLauncherRequestsDesktopFullscreen()
        {
            Assert.That(StandaloneDisplayStartup.UseDesktopFullscreen(new[]{"Biathlon.exe","-force-d3d11","-screen-fullscreen","1","-window-mode","borderless"}),Is.True);
        }
        [Test] public void ExplicitWindowedQaResolutionIsPreserved()
        {
            Assert.That(StandaloneDisplayStartup.UseDesktopFullscreen(new[]{"Biathlon.exe","-screen-fullscreen","0","-screen-width","1280","-screen-height","720"}),Is.False);
        }
        [TestCase("-batchmode")]
        [TestCase("-nographics")]
        public void HeadlessLaunchDoesNotChangeDisplay(string flag)
        {
            Assert.That(StandaloneDisplayStartup.UseDesktopFullscreen(new[]{"Biathlon.exe",flag}),Is.False);
        }
        [Test] public void ExplicitExclusiveModeIsLeftToThePlayer()
        {
            Assert.That(StandaloneDisplayStartup.UseDesktopFullscreen(new[]{"Biathlon.exe","-screen-fullscreen","1","-window-mode","exclusive"}),Is.False);
        }
    }
}

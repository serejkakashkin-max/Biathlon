using System;
using System.Globalization;
using Biathlon.Presentation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class RaceGapFormatterTests
    {
        [TestCase(0,"+0.0")]
        [TestCase(12.34,"+12.3")]
        [TestCase(59.9,"+59.9")]
        [TestCase(59.96,"+1:00.0")]
        [TestCase(60,"+1:00.0")]
        [TestCase(70,"+1:10.0")]
        [TestCase(125.7,"+2:05.7")]
        [TestCase(3599.96,"+1:00:00.0")]
        [TestCase(3665.2,"+1:01:05.2")]
        public void GapsRollOverAtClockBoundaries(double seconds,string expected)
        {Assert.That(GameplayHudModel.FormatGap(seconds,CultureInfo.InvariantCulture),Is.EqualTo(expected));}

        [TestCase("ru-RU","+1:10,2")][TestCase("en-US","+1:10.2")]
        public void FractionUsesTheChosenLocale(string locale,string expected)
        {Assert.That(GameplayHudModel.FormatGap(70.2,CultureInfo.GetCultureInfo(locale)),Is.EqualTo(expected));}

        [Test]public void InvalidValuesAreRejected()
        {
            foreach(double value in new[]{-1d,double.NaN,double.PositiveInfinity})
                Assert.Throws<ArgumentOutOfRangeException>(()=>GameplayHudModel.FormatGap(value,CultureInfo.InvariantCulture));
            Assert.Throws<ArgumentNullException>(()=>GameplayHudModel.FormatGap(0,null));
        }
    }
}

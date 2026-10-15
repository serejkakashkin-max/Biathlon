using System;
using System.Runtime.Serialization;
using Biathlon.Presentation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class LocalizationCatalogTests
    {
        [Test]public void DataCatalogCanAddAnotherLocaleWithoutChangingItsConsumer()
        {
            var d=UiTextCatalog.FromJson("{\"schema_version\":2,\"id\":\"test\",\"locales\":[\"ru\",\"en\",\"de\"],\"entries\":[{\"key\":\"greeting\",\"source_ru\":\"Привет\",\"source_en\":\"Hello\",\"values\":[\"Привет {0}\",\"Hello {0}\",\"Hallo {0}\"]}]}");
            Assert.That(d.LocaleCount,Is.EqualTo(3));Assert.That(d.Text("greeting","de"),Is.EqualTo("Hallo {0}"));Assert.That(d.TextPair("Привет","Hello","de"),Is.EqualTo("Hallo {0}"));Assert.That(d.TextPair("новое","new","en"),Is.EqualTo("new"));
            Assert.Throws<ArgumentException>(()=>d.TextPair("новое","new","de"));Assert.Throws<ArgumentException>(()=>d.Text("greeting","xx"));
        }
        [TestCase("{0}","{1}")][TestCase("{0}","bad {")][TestCase("hello","")]
        public void MissingOrMismatchedPlaceholdersFailBeforeDisplay(string a,string b)
        {
            string json="{\"schema_version\":2,\"id\":\"test\",\"locales\":[\"ru\",\"en\"],\"entries\":[{\"key\":\"a\",\"values\":[\""+a+"\",\""+b+"\"]}]}";
            Assert.Throws<SerializationException>(()=>UiTextCatalog.FromJson(json));
        }
        [Test]public void ApprovedLabelsAreTranslatorEditableAndRetainBothLocales()
        {
            var d=UiTextCatalog.FromJson(PhysiologyContractTests.ProjectFile("Assets/Resources/Biathlon/UI/front_end_pairs_v2.json"));Assert.That(d.Count,Is.GreaterThanOrEqualTo(270));
            for(int i=0;i<d.Count;i++)foreach(string locale in new[]{"ru","en"})Assert.That(d.Text(d.KeyAt(i),locale),Is.Not.Empty);
            Assert.That(d.TextPair("Достижения","Milestones","ru"),Is.EqualTo("Достижения"));
            var v1=UiTextCatalog.FromJson(PhysiologyContractTests.ProjectFile("Assets/Resources/Biathlon/UI/front_end_text_v1.json"));Assert.That(v1.Text("new_career","ru"),Is.EqualTo("Новая карьера"));
        }
    }
}

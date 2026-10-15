using System;
using System.IO;
using System.Runtime.Serialization;
using Biathlon.Presentation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class PlayerSettingsTests
    {
        private string directory;
        private InputBindingCatalog catalog;
        private FilePlayerSettingsStore store;
        [SetUp] public void Setup()
        {directory=Path.Combine(Path.GetTempPath(),"biathlon_settings_tests_"+Guid.NewGuid().ToString("N"));catalog=InputBindingCatalog.FromJson(PhysiologyContractTests.DefinitionJson("input_bindings_v1.json"));store=new FilePlayerSettingsStore(directory,catalog);}
        [TearDown] public void Cleanup(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
        private string Primary=>Path.Combine(directory,"settings_v1.json");
        [Test] public void DefaultCatalogRetainsApprovedSkiAndShootingControlsAndRoundTrips()
        {
            var p=new PlayerSettings();Assert.That(catalog.Count,Is.EqualTo(22));
            Assert.That(p.Binding(catalog,"pace_up","keyboard"),Is.EqualTo("<Keyboard>/w"));
            Assert.That(p.Binding(catalog,"attack","gamepad"),Is.EqualTo("<Gamepad>/rightShoulder"));
            Assert.That(p.Binding(catalog,"bolt","keyboard"),Is.EqualTo("<Mouse>/rightButton"));
            Assert.That(p.Binding(catalog,"breath","keyboard"),Is.EqualTo("<Keyboard>/space"));
            Assert.That(PlayerSettings.FromJson(p.ToJson(catalog),catalog).ToJson(catalog),Is.EqualTo(p.ToJson(catalog)));
        }
        [Test] public void RebindingPersistsBothDevicesWithoutChangingOtherPreferencesOrDefaults()
        {
            var p=new PlayerSettings{Locale="en",TextSize=2,Quality="high",Music=123,AimSensitivity=1450,InvertAim=true,Device="gamepad"};
            p.Rebind(catalog,"attack","keyboard","<Keyboard>/r");p.Rebind(catalog,"attack","gamepad","<Gamepad>/rightStickPress");
            store.Save(p);var actual=store.Load(new PlayerSettings());Assert.That(actual.Status,Is.EqualTo(PlayerSettingsLoadStatus.Loaded));
            Assert.That(actual.Settings.ToJson(catalog),Is.EqualTo(p.ToJson(catalog)));
            Assert.That(actual.Settings.Binding(catalog,"shoot","keyboard"),Is.EqualTo("<Mouse>/leftButton"));
            actual.Settings.ResetBindings();Assert.That(actual.Settings.BindingOverrideCount,Is.Zero);Assert.That(actual.Settings.Binding(catalog,"attack","keyboard"),Is.EqualTo("<Keyboard>/leftShift"));
            Assert.That(actual.Settings.Music,Is.EqualTo(123));Assert.That(actual.Settings.AimSensitivity,Is.EqualTo(1450));
        }
        [Test] public void ConflictsReservedControlsAndWrongTypesAreRejectedWithoutMutation()
        {
            var p=new PlayerSettings();string before=p.ToJson(catalog);
            Assert.That(catalog.Conflict(p,"attack","keyboard","<Keyboard>/w"),Is.EqualTo("pace_up"));
            Assert.Throws<ArgumentException>(()=>p.Rebind(catalog,"attack","keyboard","<Keyboard>/w"));
            Assert.Throws<ArgumentException>(()=>p.Rebind(catalog,"attack","keyboard","<Keyboard>/escape"));
            Assert.Throws<ArgumentException>(()=>p.Rebind(catalog,"attack","gamepad","<Gamepad>/start"));
            Assert.Throws<ArgumentException>(()=>p.Rebind(catalog,"attack","keyboard","<Gamepad>/rightStickPress"));
            Assert.Throws<ArgumentException>(()=>p.Rebind(catalog,"aim_horizontal","gamepad","<Gamepad>/buttonWest"));
            Assert.That(p.ToJson(catalog),Is.EqualTo(before));
            // One key may serve distinct skiing/shooting contexts, but cannot steal a global shortcut.
            p.Rebind(catalog,"attack","keyboard","<Keyboard>/f");Assert.That(p.BindingOverrideCount,Is.EqualTo(1));
            Assert.Throws<ArgumentException>(()=>p.Rebind(catalog,"attack","keyboard","<Keyboard>/f2"));
        }
        [Test] public void CorruptPrimaryRecoversVerifiedBackupAndNeverCopiesCorruptionOverIt()
        {
            var first=new PlayerSettings{Locale="en",Music=321};store.Save(first);store.Save(new PlayerSettings{Music=654});
            string goodBackup=File.ReadAllText(Primary+".bak");File.WriteAllText(Primary,"{interrupted");
            var restored=store.Load(new PlayerSettings());Assert.That(restored.Status,Is.EqualTo(PlayerSettingsLoadStatus.BackupRecovered));
            Assert.That(restored.Settings.ToJson(catalog),Is.EqualTo(first.ToJson(catalog)));Assert.That(File.ReadAllText(Primary),Is.EqualTo("{interrupted"));
            restored.Settings.Music=111;store.Save(restored.Settings);
            Assert.That(File.ReadAllText(Primary+".bak"),Is.EqualTo(goodBackup));
            Assert.That(Directory.GetFiles(directory,"*.invalid-*").Length,Is.EqualTo(1));Assert.That(store.Load(new PlayerSettings()).Settings.Music,Is.EqualTo(111));
        }
        [Test] public void IncompleteTempAndFutureSchemaNeverBecomePreferencesOrDisappearDuringRead()
        {
            Directory.CreateDirectory(directory);File.WriteAllText(Primary+".aborted.tmp",new PlayerSettings{Music=999}.ToJson(catalog));
            var legacy=new PlayerSettings{Locale="en",Quality="balanced",Music=256};var first=store.Load(legacy);
            Assert.That(first.Status,Is.EqualTo(PlayerSettingsLoadStatus.New));Assert.That(first.Settings.Music,Is.EqualTo(256));
            string future=legacy.ToJson(catalog).Replace("\"schema_version\":1","\"schema_version\":2");File.WriteAllText(Primary,future);
            var unknown=store.Load(legacy);Assert.That(unknown.Status,Is.EqualTo(PlayerSettingsLoadStatus.Invalid));Assert.That(File.ReadAllText(Primary),Is.EqualTo(future));
            Assert.That(File.Exists(Primary+".aborted.tmp"),Is.True);Assert.That(unknown.Settings.Quality,Is.EqualTo("balanced"));
        }
        [Test] public void InvalidValuesAndDuplicateOverridesCannotReplaceGoodSettings()
        {
            var p=new PlayerSettings();store.Save(p);string original=File.ReadAllText(Primary);
            p.Master=1001;Assert.Throws<SerializationException>(()=>store.Save(p));Assert.That(File.ReadAllText(Primary),Is.EqualTo(original));
            p.Master=800;p.AimSensitivity=0;Assert.Throws<SerializationException>(()=>store.Save(p));
            string bad=original.Replace("\"bindings\":[]","\"bindings\":[{\"action\":\"attack\",\"device\":\"keyboard\",\"path\":\"<Keyboard>/r\"},{\"action\":\"attack\",\"device\":\"keyboard\",\"path\":\"<Keyboard>/t\"}]");
            Assert.That(bad,Is.Not.EqualTo(original));Assert.Throws<SerializationException>(()=>PlayerSettings.FromJson(bad,catalog));
        }
        [Test] public void LockedPublishLeavesGoodBytesAndNoPartialSettings()
        {
            store.Save(new PlayerSettings{Music=111});string before=File.ReadAllText(Primary);
            using(var held=new FileStream(Primary+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))Assert.Throws<IOException>(()=>store.Save(new PlayerSettings{Music=999}));
            Assert.That(File.ReadAllText(Primary),Is.EqualTo(before));Assert.That(Directory.GetFiles(directory,"*.tmp"),Is.Empty);
        }
        [Test] public void CatalogRejectsDuplicateActionAndUnsafeControlExpressions()
        {
            string json=PhysiologyContractTests.DefinitionJson("input_bindings_v1.json");
            Assert.Throws<SerializationException>(()=>InputBindingCatalog.FromJson(json.Replace("\"pace_down\"","\"pace_up\"")));
            Assert.Throws<SerializationException>(()=>InputBindingCatalog.FromJson(json.Replace("<Keyboard>/w","<Keyboard>/*")));
        }
        [Test] public void TruncatedTrailingDuplicateAndEscapedDuplicateJsonAreRejectedBeforeDeserialization()
        {
            string json=new PlayerSettings().ToJson(catalog);
            Assert.Throws<SerializationException>(()=>PlayerSettings.FromJson(json.Substring(0,json.Length-1),catalog));
            Assert.Throws<SerializationException>(()=>PlayerSettings.FromJson(json+"{}",catalog));
            Assert.Throws<SerializationException>(()=>PlayerSettings.FromJson(json.Substring(0,json.Length-1)+",\"music\":999}",catalog));
            Assert.Throws<SerializationException>(()=>PlayerSettings.FromJson(json.Substring(0,json.Length-1)+",\"m\\u0075sic\":999}",catalog));
            store.Save(new PlayerSettings{Music=222});store.Save(new PlayerSettings{Music=333});File.WriteAllText(Primary,json+"{}");
            Assert.That(store.Load(new PlayerSettings()).Status,Is.EqualTo(PlayerSettingsLoadStatus.BackupRecovered));
        }
    }
}

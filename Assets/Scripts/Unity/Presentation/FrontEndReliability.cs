using System;
using System.IO;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.UIElements;
namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private static UiTextCatalog reliabilityText;
        private ICareerProfileStore reliabilityProfiles;
        private ReliabilityPolicy reliabilityPolicy;
        private ReliabilityEventBuffer reliabilityEvents;
        private string reportDirectory;
        public string LastDiagnosticExport {get;private set;}
        public void SetDiagnosticDirectoryForReview(string directory)
        {if(PersistPreferences)throw new InvalidOperationException("Only isolated reviews can override diagnostic output.");reportDirectory=Path.GetFullPath(directory);}
        private string R(string key)
        {if(reliabilityText==null)reliabilityText=UiTextCatalog.FromJson(ProductionVenueHost.ReadResource("Biathlon/UI/reliability_text_v1"));return reliabilityText.Text(key,Locale);}
        private void InitializeReliability(ICareerProfileStore profiles)
        {reliabilityProfiles=profiles;reliabilityPolicy=ReliabilityPolicy.FromJson(ReadCore("reliability_policy_v1.json"));reliabilityEvents=new ReliabilityEventBuffer(reliabilityPolicy);reportDirectory=Path.Combine(Application.persistentDataPath,"Diagnostics");Application.logMessageReceived+=RecordRuntimeIssue;}
        private void RecordRuntimeIssue(string condition,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)RecordIssue(DiagnosticIssue.RuntimeError);}
        private void RecordIssue(DiagnosticIssue issue)
        {var runner=Race?.Host?.Runner;reliabilityEvents?.Record(issue,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),runner==null?-1:runner.Field.Tick.Value);}
        private void ReleaseReliability()=>Application.logMessageReceived-=RecordRuntimeIssue;
        private void AddReliabilitySettings(VisualElement parent)
        {
            AddLabel(parent,R("title"),theme.section_size,true);var card=Tile(parent,"save_health");AddLabel(card,R("safe_title"),theme.body_size,true).style.color=FrostlineStyle.Accent;
            var source=(reliabilityProfiles as FileCareerProfileStore)?.CurrentSource;
            string status=Session.Career==null?R("not_loaded"):source==CareerProfileSource.Backup?R("backup_recovered"):source==CareerProfileSource.BeforeRaceCheckpoint?R("checkpoint_recovered"):R("primary_ok");
            AddLabel(card,status,theme.caption_size).name="save_health_status";AddLabel(card,R("autosave"),theme.caption_size);
            var export=Tile(parent,"diagnostic_card");AddLabel(export,R("report_title"),theme.body_size,true);AddLabel(export,R("privacy"),theme.caption_size);AddButton(export,"export_diagnostic",R("export"),ExportDiagnostic,true);
            if(LastDiagnosticExport!=null)AddLabel(export,R("export_done"),theme.caption_size).style.color=FrostlineStyle.Accent;
        }
        private void ExportDiagnostic()
        {
            try{var config=new DiagnosticConfiguration(Application.unityVersion,Application.version,Locale,TextSizeIndex,HighContrast,ReducedMotion,Settings?.Quality??"high");LastDiagnosticExport=DiagnosticBundle.Export(reportDirectory,reliabilityPolicy,config,Session.Career,reliabilityEvents.Snapshot(),DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());LastMessage=R("export_done");Render();}
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException){RecordIssue(DiagnosticIssue.SaveWriteFailed);LastMessage=R("export_failed");Render();}
        }
        private void ContinueReliably()
        {
            try
            {
                Session.ContinueCareer();LastMessage=null;var receipt=(reliabilityProfiles as FileCareerProfileStore)?.LastRead;
                if(receipt?.WasRecovered==true){bool backup=receipt.Source==CareerProfileSource.Backup;RecordIssue(backup?DiagnosticIssue.SaveBackupRecovered:DiagnosticIssue.SaveCheckpointRecovered);LastMessage=R(backup?"backup_recovered":"checkpoint_recovered");}
                else if(receipt?.WasMigrated==true)LastMessage=R("migrated");Render();
            }
            catch(CareerProfileException e){RecordIssue(e.Failure==CareerProfileFailure.UnsupportedVersion?DiagnosticIssue.UnsupportedSaveVersion:DiagnosticIssue.SaveReadFailed);LastMessage=R(e.Failure==CareerProfileFailure.UnsupportedVersion?"future_version":e.Failure==CareerProfileFailure.AccessDenied?"access_denied":"load_failed");Render();}
            catch(Exception e)when(e is ArgumentException||e is System.Runtime.Serialization.SerializationException||e is IOException||e is UnauthorizedAccessException){RecordIssue(e is FileNotFoundException||e is ArgumentException?DiagnosticIssue.MissingContent:DiagnosticIssue.SaveReadFailed);LastMessage=R("load_failed");Render();}
            catch(InvalidOperationException){RecordIssue(DiagnosticIssue.InvalidConfiguration);LastMessage=R("load_failed");Render();}
        }
    }
}

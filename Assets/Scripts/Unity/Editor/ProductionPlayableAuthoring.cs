#if UNITY_EDITOR
using System;
using System.IO;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Biathlon.Unity
{
    /// <summary>Creates reviewable playable scenes from validated resources, with no manual Inspector setup.</summary>
    public static class ProductionPlayableAuthoring
    {
        public const string Venue01="Biathlon/Venues/woodland_basin_v1/";
        [MenuItem("BIATHLON/Venues/Generate Venue01 Playable Scenes")]
        public static void GenerateVenue01()
        {
            var review=ProductionVenueHost.ReadOptionalReview(PlayerBuildPipeline.ProductionReviewResource);
            foreach(var entry in review.entries)Generate(Venue01+"manifest",Venue01+"environment",entry.format_id,entry.field_count,entry.scene_path,PlayerBuildPipeline.ProductionReviewResource);
        }
        public static string Generate(string manifestResource,string environmentResource,string formatId,int count,string output,string reviewResource=null)
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Scene generation requires idle Edit Mode.");
            if(string.IsNullOrWhiteSpace(output)||!output.StartsWith("Assets/Scenes/",StringComparison.Ordinal)||!output.EndsWith(".unity",StringComparison.Ordinal)||output.Contains(".."))
                throw new ArgumentException("An explicit project scene output is required.");
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(manifestResource)),ProductionVenueHost.ReadResource);
            var policy=VenueValidationPolicy.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/production_venue_validation_v1"));
            var report=VenueValidator.Validate(plan,policy,id=>Resources.Load<GameObject>(id)!=null);
            if(!report.Passed)throw new ArgumentException("Production scene source failed static validation: "+report.ToJson());
            if(plan.Formats==null)throw new ArgumentException("Production scene requires an explicit format catalog.");
            VenueFormatBinding format=null;for(int i=0;i<plan.Formats.Count;i++)if(plan.Formats.Get(i).Id==formatId)format=plan.Formats.Get(i);
            if(format==null||format.Profile.Format==CompetitionFormat.Pursuit||count<1||count>format.Profile.FieldCapacity||
                (format.Profile.Format==CompetitionFormat.MassStart&&count>plan.LaneCount))
                throw new ArgumentException("Fresh scenes require a covered Sprint or Mass Start field; Pursuit needs a qualification snapshot.");
            var environment=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(environmentResource));
            if(environment.venue_id!=plan.Manifest.Id||environment.manifest_resource!=manifestResource)throw new ArgumentException("Environment belongs to a different authored venue.");
            var review=ProductionVenueHost.ReadOptionalReview(reviewResource);
            if(review!=null)
            {
                var entry=review.ForScene(output);
                if(review.venue_id!=plan.Manifest.Id||entry.format_id!=formatId||entry.field_count!=count)throw new ArgumentException("Review resource must bind the exact generated scene/format/roster.");
            }
            // Unity rejects additive NewScene while the caller has an unsaved untitled scene,
            // and preview scenes cannot be saved. Emit the small serialized bootstrap scene
            // directly, then verify its imported component in an isolated preview scene. This
            // preserves every caller scene, including an untitled dirty scene, without prompts.
            const string hostScript="Assets/Scripts/Unity/Presentation/ProductionVenueHost.cs";
            string guid=AssetDatabase.AssetPathToGUID(hostScript);
            if(string.IsNullOrEmpty(guid)||AssetDatabase.LoadAssetAtPath<MonoScript>(hostScript)==null)throw new FileNotFoundException("Production host script missing.");
            string yaml="%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"+
                "--- !u!1 &1\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n  - component: {fileID: 2}\n  - component: {fileID: 3}\n  m_Layer: 0\n  m_Name: Authored playable "+plan.Manifest.Id+"\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n"+
                "--- !u!4 &2\nTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 1}\n  serializedVersion: 2\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_ConstrainProportionsScale: 0\n  m_Children: []\n  m_Father: {fileID: 0}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n"+
                "--- !u!114 &3\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 1}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: "+guid+", type: 3}\n  m_Name:\n  m_EditorClassIdentifier:\n  VenueResource: "+manifestResource+"\n  ProfileResource: "+format.ProfileResource+"\n  RouteResource: "+format.RouteResource+"\n  ConditionsResource: Biathlon/race_snow_neutral_v1\n  EnvironmentResource: "+environmentResource+"\n"+(reviewResource==null?"":"  ReviewDefinitionResource: "+reviewResource+"\n")+"  AutoStart: 1\n  UseGamepad: 0\n  FieldCount: "+count+"\n"+
                "--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n  - {fileID: 2}\n";
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,yaml,new System.Text.UTF8Encoding(false));AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);
            var check=EditorSceneManager.OpenPreviewScene(output);
            try
            {
                var roots=check.GetRootGameObjects();var host=roots.Length==1?roots[0].GetComponent<ProductionVenueHost>():null;
                if(host==null||host.VenueResource!=manifestResource||host.ProfileResource!=format.ProfileResource||host.RouteResource!=format.RouteResource||host.EnvironmentResource!=environmentResource||host.FieldCount!=count||!host.AutoStart||(reviewResource==null?!string.IsNullOrEmpty(host.ReviewDefinitionResource):host.ReviewDefinitionResource!=reviewResource))
                    throw new IOException("Imported production scene did not preserve its explicit source binding.");
            }
            finally{EditorSceneManager.ClosePreviewScene(check);}
            return output;
        }
    }
}
#endif

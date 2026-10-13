using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Optional review controls. Gameplay commands still belong to the host.</summary>
    public sealed class ProductionReviewPresentation : MonoBehaviour
    {
        private ProductionReviewDefinition definition;
        private RenderQualityPresentation quality;
        private AsyncOperation transition;
        public bool Initialized=>definition!=null;
        public bool TransitionPending=>transition!=null;
        public string DefinitionId=>definition?.id;
        public string EntryId {get;private set;}
        public string TierId=>quality?.TierId;
        private static Rect PanelRect=>new Rect(12,Mathf.Max(12,Screen.height-138),560,95);
        public bool ContainsScreenPointer(Vector2 position)=>Initialized&&isActiveAndEnabled&&PanelRect.Contains(new Vector2(position.x,Screen.height-position.y));
        public void Initialize(ProductionVenueHost host,ProductionReviewDefinition source,string entryId)
        {
            if(Initialized)throw new InvalidOperationException("Initialize review controls once.");
            if(host==null||host.Runner==null||source==null)throw new ArgumentNullException();
            if(!isActiveAndEnabled)throw new InvalidOperationException("Enabled review adapter required.");
            var detached=ProductionReviewDefinition.FromJson(source.ToJson());var entry=detached.Get(entryId);
            if(detached.venue_id!=host.Venue.Manifest.Id||entry.field_count!=host.Runner.Field.Count||
                (entry.format_id=="sprint"?Biathlon.Domain.CompetitionFormat.Sprint:Biathlon.Domain.CompetitionFormat.MassStart)!=host.Runner.Field.Profile.Format)
                throw new ArgumentException("Review must bind this exact venue, format and scene roster.");
            var owned=host.GetComponent<RenderQualityPresentation>();
            if(owned==null||owned.Pipeline==null)throw new ArgumentException("Initialize owned gameplay render quality first.");
            // This is called before environment composition and its first LateUpdate render.
            RenderQualityDefinition.Load().Get(detached.initial_quality_tier);
            owned.Apply(detached.initial_quality_tier);
            quality=owned;EntryId=entry.id;definition=detached;
        }
        public void ApplyQuality(string tier)
        {
            if(!Initialized||TransitionPending||!isActiveAndEnabled)throw new InvalidOperationException("Active review controls required.");
            quality.Apply(tier);
        }
        public void RequestEntry(string entryId)
        {
            if(!Initialized||!isActiveAndEnabled)throw new InvalidOperationException("Active review controls required.");
            if(TransitionPending)throw new InvalidOperationException("A scene transition is already pending.");
            var entry=definition.Get(entryId);
            if(!Application.CanStreamedLevelBeLoaded(entry.scene_path))throw new InvalidOperationException("Review scene is missing from this player build.");
            transition=SceneManager.LoadSceneAsync(entry.scene_path,LoadSceneMode.Single);
            if(transition==null)throw new InvalidOperationException("Review scene did not begin loading.");
        }
        private void OnGUI()
        {
            if(!Initialized)return;bool previous=GUI.enabled;GUI.enabled=previous&&!TransitionPending;
            try
            {
                GUILayout.BeginArea(PanelRect,GUI.skin.box);
                GUILayout.Label(definition.Get(EntryId).label+" · "+TierId);
                GUILayout.BeginHorizontal();foreach(var entry in definition.entries)
                    if(GUILayout.Button("New "+entry.label))RequestEntry(entry.id);
                GUILayout.EndHorizontal();GUILayout.BeginHorizontal();
                foreach(var tier in new[]{"high","balanced","low"})if(GUILayout.Button(RenderQualityDefinition.DisplayName(tier)))ApplyQuality(tier);
                GUILayout.EndHorizontal();GUILayout.EndArea();
            }
            finally{GUI.enabled=previous;}
        }
    }
}

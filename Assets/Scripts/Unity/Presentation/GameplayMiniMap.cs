using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class GameplayHud
    {
        private RaceMiniMapSettings miniMapSettings;
        public RaceMiniMapGeometry MiniMapGeometry{get;private set;}
        public RaceMiniMapState MiniMapState{get;private set;}
        public RaceMiniMapGraphic MiniMapGraphic{get;private set;}
        public bool MiniMapVisible{get;private set;}
        private VisualElement miniMapPanel;
        private void BuildMiniMap()
        {
            MiniMapVisible=false;miniMapPanel=null;MiniMapGraphic=null;
            if(host?.Venue==null||host.Runner==null)return;
            miniMapSettings=miniMapSettings??RaceMiniMapSettings.Load();
            MiniMapGeometry=MiniMapGeometry??new RaceMiniMapGeometry(host.Venue,host.Runner.Field.Route,miniMapSettings);
            MiniMapState=MiniMapState??new RaceMiniMapState(miniMapSettings.max_athletes);
            miniMapPanel=Card("minimap_panel",true,true,miniMapSettings.width);
            // Text scales; the spatial diagram keeps its compact, legible footprint.
            miniMapPanel.style.width=miniMapSettings.width;miniMapPanel.style.paddingTop=miniMapPanel.style.paddingBottom=6;
            MiniMapGraphic=new RaceMiniMapGraphic(MiniMapGeometry,miniMapSettings,MiniMapState,HighContrast);MiniMapGraphic.style.height=miniMapSettings.map_height;miniMapPanel.Add(MiniMapGraphic);
            miniMapPanel.style.display=DisplayStyle.None;
            Root.UnregisterCallback<GeometryChangedEvent>(PlaceMiniMap);Root.RegisterCallback<GeometryChangedEvent>(PlaceMiniMap);
            timingPanel.RegisterCallback<GeometryChangedEvent>(PlaceMiniMap);
            motionPanel.RegisterCallback<GeometryChangedEvent>(PlaceMiniMap);shootingPanel.RegisterCallback<GeometryChangedEvent>(PlaceMiniMap);
        }
        private void RefreshMiniMap(RacePresentationSnapshot snapshot,int player)
        {
            if(miniMapPanel==null)return;
            // The fixed-position shooting view and full tables keep their approved unobstructed layout.
            MiniMapVisible=!Model.Shooting&&!CheckpointTableVisible&&!BroadcastVisible;
            miniMapPanel.style.display=MiniMapVisible?DisplayStyle.Flex:DisplayStyle.None;
            if(!MiniMapVisible)return;
            if(MiniMapState.Observe(snapshot,MiniMapGeometry,player))MiniMapGraphic.Observe();
            PlaceMiniMap(null);
        }
        private void PlaceMiniMap(GeometryChangedEvent _)
        {
            if(miniMapPanel==null||!MiniMapVisible||Root==null||!float.IsFinite(timingPanel.worldBound.yMax))return;
            float top=timingPanel.worldBound.yMax-Root.worldBound.yMin+miniMapSettings.gap;
            if(!float.IsFinite(miniMapPanel.resolvedStyle.top)||Mathf.Abs(miniMapPanel.resolvedStyle.top-top)>.1f)miniMapPanel.style.top=top;
        }
    }
}

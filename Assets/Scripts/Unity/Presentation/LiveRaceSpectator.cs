using System;
using System.Collections.Generic;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Observes the still-running field; fast completion uses the same authority and replay writer.</summary>
    public sealed class LiveRaceSpectator:MonoBehaviour
    {
        private ProductionVenueHost host;private FrontEndMenu menu;private Camera view;private RaceReviewSettings settings;
        private UIDocument document;private GameObject ui;private PanelSettings panel;private Label caption,status;private Button finishButton;
        private int selected,mode;private bool snap=true;private string L(string ru,string en)=>menu.Locale=="ru"?ru:en;
        public int SelectedIndex=>selected;public int CameraMode=>mode;public VisualElement Root=>document?.rootVisualElement;
        public void Initialize(ProductionVenueHost race,FrontEndMenu owner)
        {
            if(host!=null||race==null||owner==null)throw new ArgumentException("Fresh live spectator required.");
            host=race;menu=owner;settings=RaceReviewSettings.Load();
            var go=new GameObject("Live broadcast camera",typeof(Camera));go.transform.SetParent(transform,false);view=go.GetComponent<Camera>();view.enabled=false;
            host.EnterSpectatorView(view);view.enabled=true;selected=host.PlayerIndex;SelectNext(1);
            panel=FrontEndPanelSettings.Create();panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(1600,900);panel.match=1;
            ui=new GameObject("Live spectator interface");SceneManager.MoveGameObjectToScene(ui,gameObject.scene);document=ui.AddComponent<UIDocument>();document.panelSettings=panel;document.sortingOrder=30;
            var root=Root;root.style.flexGrow=1;root.pickingMode=PickingMode.Ignore;
            var card=new VisualElement{name="spectator_panel"};card.style.position=Position.Absolute;card.style.left=32;card.style.right=32;card.style.bottom=28;
            card.style.backgroundColor=FrontEndTheme.Color("#092534F2");card.style.paddingTop=card.style.paddingBottom=18;card.style.paddingLeft=card.style.paddingRight=24;root.Add(card);
            caption=new Label();caption.style.fontSize=24;caption.style.unityFontStyleAndWeight=FontStyle.Bold;caption.style.color=Color.white;card.Add(caption);
            status=new Label();status.style.fontSize=17;status.style.color=FrontEndTheme.Color("#BBDFDF");card.Add(status);
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.marginTop=12;row.style.flexWrap=Wrap.Wrap;card.Add(row);
            Add(row,"spectator_previous",L("← Предыдущий","← Previous"),()=>SelectNext(-1));Add(row,"spectator_next",L("Следующий →","Next →"),()=>SelectNext(1));
            var cameras=new DropdownField(new List<string>{L("За спортсменом","Follow athlete"),L("Камера сверху","Overhead camera"),L("Боковая камера","Trackside camera")},0){name="spectator_camera"};cameras.style.width=230;cameras.style.height=44;cameras.style.marginRight=12;row.Add(cameras);
            cameras.RegisterValueChangedCallback(e=>{mode=cameras.index;snap=true;});
            finishButton=Add(row,"simulate_finish",L("Завершить гонку  ↗","Complete race  ↗"),()=>host.RequestFastFinish());
        }
        private Button Add(VisualElement parent,string id,string label,Action action)
        {
            var b=new Button(action){name=id,text=label};b.style.height=44;b.style.fontSize=18;b.style.paddingLeft=b.style.paddingRight=16;b.style.marginRight=12;b.style.backgroundColor=FrontEndTheme.Color("#126F78");b.style.color=Color.white;parent.Add(b);return b;
        }
        public void SelectNext(int direction)
        {
            if(host==null)return;int count=host.Runner.Field.Count;
            for(int i=1;i<=count;i++){int candidate=(selected+direction*i+count*2)%count;if(!host.Runner.Field.GetAthlete(candidate).HasFinished){selected=candidate;snap=true;return;}}
        }
        private void LateUpdate()
        {
            if(host==null||host.Runner.Field.IsComplete)return;
            if(Keyboard.current?.rightBracketKey.wasPressedThisFrame==true)SelectNext(1);
            if(Keyboard.current?.leftBracketKey.wasPressedThisFrame==true)SelectNext(-1);
            if(Keyboard.current?.enterKey.wasPressedThisFrame==true)host.RequestFastFinish();
            if(Keyboard.current?.cKey.wasPressedThisFrame==true){mode=(mode+1)%3;Root.Q<DropdownField>("spectator_camera").SetValueWithoutNotify(Root.Q<DropdownField>("spectator_camera").choices[mode]);snap=true;}
            if(host.Runner.Field.GetAthlete(selected).HasFinished)SelectNext(1);
            host.GetComponent<RaceAudioPresentation>()?.SpectatorContext(view,selected);
            string id=host.Runner.Field.GetAthlete(selected).AthleteId.Value;var subject=host.Proxies.GetProxy(id);
            var forward=subject.forward;forward.y=0;forward.Normalize();var right=new Vector3(forward.z,0,-forward.x);
            var desired=mode==1?subject.position+Vector3.up*settings.overhead_height_m-forward*3:
                mode==2?subject.position+right*settings.spectator_side_m+Vector3.up*settings.spectator_height_m:
                subject.position-forward*settings.spectator_distance_m+Vector3.up*settings.spectator_height_m;
            view.transform.position=snap?desired:Vector3.Lerp(view.transform.position,desired,1-Mathf.Exp(-settings.spectator_response*Time.unscaledDeltaTime));snap=false;
            view.transform.rotation=Quaternion.LookRotation(subject.position+Vector3.up-view.transform.position,mode==1?forward:Vector3.up);
            string name=id;for(int i=0;i<menu.Session.Career.AthleteCount;i++)if(menu.Session.Career.Athlete(i).Id==id){name=menu.Session.Career.Athlete(i).Name;break;}
            caption.text=L("ФИНИШИРОВАЛИ · В ЭФИРЕ  ","FINISHED · LIVE  ")+name;
            int finished=0;for(int i=0;i<host.Runner.Field.Count;i++)if(host.Runner.Field.GetAthlete(i).HasFinished)finished++;
            status.text=(host.FastFinish?L("Рассчитываем оставшуюся гонку…  ","Simulating remaining race…  "):L("Наблюдение за гонкой  ","Watching the race  "))+finished+" / "+host.Runner.Field.Count+L(" финишировали · [ / ] спортсмен · C камера"," finished · [ / ] athlete · C camera");
            finishButton.SetEnabled(!host.FastFinish);
        }
        private void OnEnable(){if(ui!=null)ui.SetActive(true);}
        private void OnDisable(){if(ui!=null)ui.SetActive(false);}
        private void OnDestroy(){if(ui!=null){ui.SetActive(false);Destroy(ui);}if(panel!=null)Destroy(panel);}
    }
}

using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Lane-local target camera. Hit decisions belong exclusively to simulation.</summary>
    public sealed class RangePresentation : MonoBehaviour
    {
        private RacePresentationFeed feed;
        private string athleteId;
        private TargetGeometryDefinition geometry;
        private GameObject stage;
        private Camera aimCamera;
        private readonly Renderer[] targets=new Renderer[TargetGeometryDefinition.TargetCount];
        private Material openMaterial,closedMaterial;
        private long observedTick=-1;
        private int observedBout=-1;
        public bool Visible { get; private set; }
        public int ClosedMask { get; private set; }
        public string Status { get; private set; }="";
        public Camera AimCamera=>aimCamera;
        public void Initialize(RacePresentationFeed source,string selectedAthleteId,TargetGeometryDefinition definition)
        {
            if(source==null||definition==null)throw new ArgumentNullException();
            if(string.IsNullOrWhiteSpace(selectedAthleteId))throw new ArgumentException("Stable athlete ID required.");
            Clear();feed=source;athleteId=selectedAthleteId;geometry=definition;
            // A camera-local stage avoids changing the venue or using its schematic target spacing.
            stage=new GameObject("Range target stage");stage.transform.SetParent(transform,false);
            stage.transform.localPosition=new Vector3(0,-10000,0);
            openMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")){color=Color.black};
            closedMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")){color=Color.white};
            for(int i=0;i<targets.Length;i++)
            {
                var disk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);disk.transform.SetParent(stage.transform,false);
                disk.transform.localPosition=new Vector3((float)geometry.CenterX(i),0,(float)geometry.DistanceMeters);
                disk.transform.localRotation=Quaternion.Euler(90,0,0);Destroy(disk.GetComponent<Collider>());
                targets[i]=disk.GetComponent<Renderer>();targets[i].sharedMaterial=openMaterial;
            }
            var cameraObject=new GameObject("Range aim camera");cameraObject.transform.SetParent(stage.transform,false);
            aimCamera=cameraObject.AddComponent<Camera>();aimCamera.depth=200;aimCamera.nearClipPlane=.01f;
            aimCamera.farClipPlane=(float)geometry.DistanceMeters+1;aimCamera.clearFlags=CameraClearFlags.SolidColor;aimCamera.backgroundColor=new Color(.42f,.48f,.52f);
            // Frame the authored bank plus one spacing margin on both sides; vertical FOV follows aspect.
            aimCamera.fieldOfView=2*Mathf.Atan((float)(geometry.SpacingMeters*TargetGeometryDefinition.TargetCount/(2*geometry.DistanceMeters)))*Mathf.Rad2Deg;
            stage.SetActive(false);
        }
        public void Refresh()
        {
            var snapshot=feed==null?null:feed.Current;
            if(snapshot==null){SetVisible(false);observedTick=-1;observedBout=-1;Status="";return;}
            if(snapshot.Tick==observedTick)return;observedTick=snapshot.Tick;SetVisible(false);
            for(int i=0;i<snapshot.Count;i++)
            {
                var a=snapshot.Get(i);if(a.AthleteId!=athleteId)continue;
                SetVisible(a.ShootingActive&&a.Phase==CompetitionPhase.Range);
                if(!Visible)return;
                if(observedBout!=a.CompletedBouts){observedBout=a.CompletedBouts;Status="";}
                ClosedMask=a.ClosedTargetsMask;
                for(int j=0;j<targets.Length;j++)
                {
                    float diameter=(float)(2*geometry.Radius(a.Stance));
                    targets[j].transform.localScale=new Vector3(diameter,.001f,diameter);
                    targets[j].sharedMaterial=(ClosedMask&(1<<j))!=0?closedMaterial:openMaterial;
                }
                var ray=a.Sight.Ray;
                aimCamera.transform.localRotation=Quaternion.LookRotation(new Vector3((float)ray.DirectionX,(float)ray.DirectionY,(float)ray.DirectionZ),Vector3.up);
                if(a.ShotOutcome!=ShotOutcome.NoShot)Status=a.ShotOutcome.ToString();
                laneText="LANE "+a.RangeLane+"  |  "+a.Stance+"  |  SHOTS "+a.ShotsFired+"  "+Status;
                return;
            }
        }
        private string laneText="";
        private void SetVisible(bool value){Visible=value;if(stage!=null)stage.SetActive(value);}
        private void LateUpdate()=>Refresh();
        private void OnGUI()
        {
            if(!Visible)return;
            GUI.Box(new Rect(Screen.width/2f-240,Screen.height-52,480,34),laneText);
            var color=GUI.color;GUI.color=Color.red;
            GUI.DrawTexture(new Rect(Screen.width/2f-8,Screen.height/2f,16,1),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width/2f,Screen.height/2f-8,1,16),Texture2D.whiteTexture);GUI.color=color;
        }
        private void Clear(){if(stage!=null)Destroy(stage);if(openMaterial!=null)Destroy(openMaterial);if(closedMaterial!=null)Destroy(closedMaterial);observedTick=-1;observedBout=-1;Visible=false;}
        private void OnDestroy()=>Clear();
    }
}

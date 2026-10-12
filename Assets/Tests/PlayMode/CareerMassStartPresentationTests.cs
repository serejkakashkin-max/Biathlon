using System;
using System.Collections;
using System.IO;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Replay;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class CareerMassStartPresentationTests
    {
        private GameObject root;
        private string folder;
        private static string Read(string file)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(file));
        private static CareerSave Save(CareerDefinitions d)
        {
            var s=CareerFactory.Create(d.Roster,d.Calendar,"mass_start_ui","Alex","NOR","default",new SeededRandomSource(71000));
            for(int e=0;e<2;e++)
            {
                var rows=new CareerFinish[s.AthleteCount];var order=new int[s.AthleteCount];order[0]=1;order[1]=2;int n=2;
                for(int i=0;i<s.AthleteCount;i++)if(i!=1&&i!=2)order[n++]=i;
                for(int i=0;i<rows.Length;i++)rows[i]=new CareerFinish(s.Athlete(order[i]).Id,300000+i*1000,300000+i*1000);
                CareerStandings.Register(s,new CareerRaceRecord("mass_ui_"+e,s.SeasonId,d.Calendar.Get(e).Id,d.Calendar.Get(e).Format,123,new string('a',64),rows));
            }
            return CareerSave.FromJson(s.ToJson().Replace("\"week\":1,","\"week\":10,"));
        }
        private sealed class Store:ICareerProfileStore
        {
            public string Json;
            public bool HasProfile=>true;
            public string ReadLatest()=>Json;
            public void WriteNew(string id,string value)=>throw new InvalidOperationException("Read-only existing career fixture.");
            public void Replace(string id,string prior,string value)=>throw new InvalidOperationException("This review must not write a career.");
        }
        [TearDown] public void Cleanup()
        {if(root!=null)Object.DestroyImmediate(root);if(folder!=null&&Directory.Exists(folder)){foreach(var f in Directory.GetFiles(folder))File.Delete(f);Directory.Delete(folder,false);}}
        [UnityTest,Timeout(300000)] public IEnumerator ContinuedDevelopmentMassShowsRankedStartNumbersAndRenderedYellowLeader()
        {
            var d=new CareerDefinitions(Read,"woodland_development_calendar_v1.json");var save=Save(d);string before=save.ToJson();var store=new Store{Json=before};
            folder=Path.Combine(Path.GetTempPath(),"biathlon-mass-seeding-"+Guid.NewGuid().ToString("N"));root=new GameObject("Continued Mass Start UI");
            var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(store,new RaceReplayLibrary(folder));
            menu.Session.ContinueCareer();menu.Session.OpenPreRace();menu.RefreshFrontEnd();yield return null;
            Assert.That(menu.Root.Q<Label>("pre_race_start_position").text,Does.Contain("3"));
            menu.Race.Launch(menu);for(int i=0;i<60&&menu.Race.Loading;i++)yield return null;
            var host=menu.Race.Host;Assert.That(host,Is.Not.Null,menu.LastMessage);host.enabled=false;
            Assert.That(host.PlayerIndex,Is.EqualTo(2));
            Assert.That(host.ManualStepper.World.ContextJson,Does.Contain("manual_course_contact_ice_v1"));
            for(int i=0;i<host.Runner.Field.Count;i++)
            {
                var actor=host.Runner.Field.GetAthlete(i);var bib=host.Proxies.GetRig(actor.AthleteId.Value).GetComponent<AthleteBibPresentation>();
                Assert.That(bib.Number,Is.EqualTo(i+1));Assert.That(actor.AssignedShootingLane.Ordinal,Is.EqualTo(i+1));
                Assert.That(bib.LeaderRole,i==0?Is.EqualTo(CareerLeaderBib.Total):Is.EqualTo(CareerLeaderBib.None));
            }
            Assert.That(host.Runner.Field.Tick.Value,Is.Zero,"Review must leave the countdown and race clock at zero.");
            Assert.That(store.Json,Is.EqualTo(before));
            var cameraObject=new GameObject("Grid review camera",typeof(Camera));cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;
            var leader=host.Proxies.GetProxy(host.Runner.Field.GetAthlete(0).AthleteId.Value);var player=host.Proxies.GetProxy(save.PlayerId);
            Vector3 forward=player.forward;camera.transform.position=player.position-forward*13+Vector3.up*8;camera.transform.LookAt(player.position+forward*7+Vector3.up);
            camera.fieldOfView=58;host.Environment.SetViewCamera(camera);host.Environment.Render(camera,"high",12);
            yield return Capture(camera,"ranked-mass-start.png");
            camera.transform.position=leader.position-forward*3+Vector3.up*1.5f;camera.transform.LookAt(leader.position+Vector3.up*.8f);camera.fieldOfView=35;
            host.Environment.Render(camera,"high",12);yield return Capture(camera,"development-yellow-bib.png");
            menu.Race.Abandon();yield return null;
        }
        private static IEnumerator Capture(Camera camera,string name)
        {
            var target=new RenderTexture(1280,720,24);target.Create();camera.targetTexture=target;var previous=RenderTexture.active;Texture2D image=null;
            try
            {
                camera.enabled=true;for(int i=0;i<3;i++)yield return null;
                RenderTexture.active=target;image=new Texture2D(1280,720,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                string evidence=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/mass-seeding-20261012"));Directory.CreateDirectory(evidence);File.WriteAllBytes(Path.Combine(evidence,name),image.EncodeToPNG());
            }
            finally{camera.enabled=false;camera.targetTexture=null;RenderTexture.active=previous;if(image!=null)Object.DestroyImmediate(image);target.Release();Object.DestroyImmediate(target);}
        }
    }
}

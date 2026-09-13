#if UNITY_EDITOR
using System;
using System.Security.Cryptography;
using System.Text;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using UnityEngine;
namespace Biathlon.Unity
{
    public static class PlaytestDiagnostics
    {
        public static void Begin()
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Enter Play Mode before starting the harness.");
            if(UnityEngine.Object.FindFirstObjectByType<PlayableSprintHost>()!=null)throw new InvalidOperationException("Harness requires no existing playable host.");
            var go=new GameObject("Scripted playtest");var host=go.AddComponent<PlayableSprintHost>();host.AutoStart=false;host.enabled=false;host.Initialize(2);
        }
        public static string AdvanceTo(int targetTick)
        {
            var host=UnityEngine.Object.FindFirstObjectByType<PlayableSprintHost>();
            if(host==null||host.enabled||host.Runner==null)throw new InvalidOperationException("A disabled initialized harness host is required.");
            if(targetTick<host.Runner.Field.Tick.Value||targetTick>50000)throw new ArgumentOutOfRangeException(nameof(targetTick));
            var id=host.Runner.Field.GetAthlete(host.PlayerIndex).AthleteId;
            while(host.Runner.Field.Tick.Value<targetTick&&!host.Runner.Field.IsComplete)
            {
                var tick=host.Runner.Field.Tick.Next();host.Step(new PlayerCommand(tick,id,800,0),new ShootingDeviceCommand(new WeaponAimCommand(tick,id,1000,1000),true,false));
            }
            var proxy=host.GetComponent<AthleteProxyPresentation>();proxy.enabled=false;proxy.Render(1);
            var chase=host.GetComponentInChildren<AthleteChaseCamera>();chase.enabled=false;chase.Render(1000);
            return CaptureState();
        }
        public static string CaptureState()
        {
            var host=UnityEngine.Object.FindFirstObjectByType<PlayableSprintHost>();
            if(host==null||host.enabled||host.Feed.Current==null)throw new InvalidOperationException("Freeze the host before recording evidence.");
            var snapshot=host.Feed.Current;var player=snapshot.Get(host.PlayerIndex);var rows=new Actor[snapshot.Count];
            for(int i=0;i<rows.Length;i++){var a=snapshot.Get(i);rows[i]=new Actor{id=a.AthleteId,loop_id=a.LoopId,local_nm=a.LocalNanometers,lateral_m=a.LateralMeters,speed_mps=a.SpeedMetersPerSecond,phase=a.Phase.ToString(),laps=a.CompletedLaps,bouts=a.CompletedBouts,penalties=a.PendingPenaltyLoops,closed_mask=a.ClosedTargetsMask};}
            string context;using(var sha=SHA256.Create())context=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(host.Runner.ContextJson))).Replace("-","").ToLowerInvariant();
            return JsonUtility.ToJson(new Evidence{schema_version=1,id="biathlon_playtest_evidence_v1",tick=snapshot.Tick,frequency=snapshot.TickFrequency,track_id=snapshot.TrackId,profile_id=snapshot.ProfileId,player_id=player.AthleteId,complete=snapshot.Complete,context_sha256=context,command_policy="effort800_aim1000x1000_trigger_each_tick_v1",actors=rows},true);
        }
        [Serializable] private sealed class Evidence
        {
            public int schema_version,frequency;public long tick;public string id,track_id,profile_id,player_id,context_sha256,command_policy;public bool complete;public Actor[] actors;
        }
        [Serializable] private sealed class Actor
        {
            public string id,loop_id,phase;public long local_nm;public double lateral_m,speed_mps;public int laps,bouts,penalties,closed_mask;
        }
    }
}
#endif

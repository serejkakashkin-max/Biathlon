using System;
using System.Collections;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor=UnityEngine.Cursor;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class M15_6PlayerReview
    {
        [Serializable]private sealed class NavigationReceipt
        {
            public bool gameplay_cursor_hidden,tab_cursor_hidden,focus_released,pause_cursor_visible,resume_cursor_hidden,history_pinned,latest_restored,ui_did_not_advance_clock;
            public string cursor_method="Explicit focus gain/loss at the production host boundary; actual native Cursor properties and career pause/resume checked.";
            public string history_method="Actual 30-athlete production Sprint. Opt-in QA supplies ordinary player effort/aim/trigger/bolt commands with existing AI steering, through StepWithManualPlayer; opponents use normal AI. Accepted checkpoint trackers; no edited timing, pose, result or shot outcome. World/UI captures use the native compositor.";
            public int roster,initial_latest,archived_ordinal,new_latest,restored_latest,archived_rows;
            public long accepted_tick;
        }
        private IEnumerator VerifyCursorMenus(ProductionVenueHost host)
        {
            var r=receipt.navigation;
            host.SetInputFocus(true);r.gameplay_cursor_hidden=!Cursor.visible&&Cursor.lockState==CursorLockMode.Locked&&host.GameplayCursorCaptured;Require(r.gameplay_cursor_hidden,"Gameplay did not hide/lock the native cursor.");
            host.GameplayHud.SetCheckpointTableVisible(true);r.tab_cursor_hidden=!Cursor.visible;Require(r.tab_cursor_hidden,"TAB must retain gameplay cursor capture.");host.GameplayHud.SetCheckpointTableVisible(false);
            host.SetInputFocus(false);r.focus_released=Cursor.visible&&Cursor.lockState==CursorLockMode.None;Require(r.focus_released,"Focus loss must release the native cursor.");host.SetInputFocus(true);
            menu.Race.Pause();long tick=host.Runner.Field.Tick.Value;r.pause_cursor_visible=Cursor.visible&&Cursor.lockState==CursorLockMode.None;Require(r.pause_cursor_visible,"Pause menu cursor missing.");
            yield return Capture("08b-pause-menu");Require(host.Runner.Field.Tick.Value==tick,"Pause advanced the accepted clock.");
            menu.Race.Resume();host.SetInputFocus(true);r.resume_cursor_hidden=!Cursor.visible&&host.GameplayCursorCaptured;Require(r.resume_cursor_hidden,"Resume did not capture the cursor again.");
        }
        private int historyAimingFrames;
        private IEnumerator AdvanceHistoryTo(ProductionVenueHost host,int count)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+80;int steps=0;
            while(host.Runner.Field.GetAthlete(host.PlayerIndex).Checkpoints.Count<count&&!host.Runner.Field.IsComplete&&steps++<40000)
            {
                var actor=host.Runner.Field.GetAthlete(host.PlayerIndex);var next=host.Runner.Field.Tick.Next();var shooting=actor.ActiveShooting;
                historyAimingFrames=shooting?.Phase==ShootingPhase.Aiming?historyAimingFrames+1:0;
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(next,actor.AthleteId,1000),1,1,0),
                    new WeaponAimCommand(next,actor.AthleteId,0,1000),historyAimingFrames>=40,false,host.Runner.RaceWind,cycleBolt:shooting?.NeedsBoltCycle==true,manualBoltCycle:true);
                host.Runner.StepWithManualPlayer(host.PlayerIndex,input,0,true);
                if(steps%64==0){Require(Time.realtimeSinceStartupAsDouble<deadline,"History QA exceeded its bounded simulation wait.");yield return null;}
            }
            Require(host.Runner.Field.GetAthlete(host.PlayerIndex).Checkpoints.Count>=count,"Actual player did not reach the required timing line.");
            host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));host.GameplayHud.Refresh();
        }
        private IEnumerator VerifyCheckpointHistory(ProductionVenueHost host)
        {
            // Stop device sampling while explicit QA supplies ordinary player commands
            // and existing AI steering, then freeze each accepted view for a capture.
            host.enabled=false;yield return AdvanceHistoryTo(host,4);
            var hud=host.GameplayHud;var r=receipt.navigation;r.roster=host.Runner.Field.Count;hud.SetPreferences("ru",0);hud.SetCheckpointTableVisible(true);
            r.initial_latest=hud.SelectedCheckpointOrdinal;Require(r.initial_latest>=3,"Need multiple real passed splits for history.");yield return Capture("10-latest-split-ru");
            long tick=host.Runner.Field.Tick.Value;hud.MoveCheckpointHistory(-1);r.archived_ordinal=hud.SelectedCheckpointOrdinal;
            r.archived_rows=Biathlon.Presentation.CheckpointStandings.Capture(host.Runner.Field,r.archived_ordinal).Count;
            Require(r.archived_ordinal==r.initial_latest-1&&r.archived_rows>0&&!hud.FollowingLatestCheckpoint,"Previous accepted timing line not selected.");
            yield return Capture("11-history-split-ru");r.ui_did_not_advance_clock=host.Runner.Field.Tick.Value==tick;Require(r.ui_did_not_advance_clock,"Browsing advanced the race.");
            yield return AdvanceHistoryTo(host,r.initial_latest+2);r.new_latest=host.Feed.Current.Get(host.PlayerIndex).Hud.CheckpointOrdinal;
            r.history_pinned=hud.SelectedCheckpointOrdinal==r.archived_ordinal&&hud.CheckpointTableVisible;Require(r.history_pinned,"A new timing line replaced the selected history.");
            hud.SetPreferences("en",2);hud.SetAccessibility(true);Require(hud.SelectedCheckpointOrdinal==r.archived_ordinal,"Preferences replaced the selected history.");yield return Capture("12-history-split-en-large");
            hud.FollowLatestCheckpoint();r.restored_latest=hud.SelectedCheckpointOrdinal;r.latest_restored=hud.FollowingLatestCheckpoint&&r.restored_latest==r.new_latest;
            Require(r.latest_restored,"Latest split follow mode was not restored.");r.accepted_tick=host.Runner.Field.Tick.Value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class StatEquipmentCalibrationTests
    {
        private static readonly string[] Stats={"endurance","anaerobic_capacity","recovery","strength","ski_technique","climbing","downhill","cornering"};
        private static readonly string[] Tracks={"synthetic_flat_v1","synthetic_climb_v1","synthetic_rolling_v1","synthetic_technical_descent_v1"};
        private static TimeTrialAthlete Athlete(int baseline,string changed=null)
        {
            string json=TimeTrialTests.Athlete().ToJson();
            foreach(string stat in Stats)json=PhysiologyContractTests.ReplaceNumber(json,stat,(baseline+(changed==stat||changed=="all"?10:0)).ToString());
            return TimeTrialAthlete.FromJson(json);
        }
        private static double Trial(string track,TimeTrialAthlete athlete,string snow="packed_neutral",string ski="ski_neutral")
        {
            var session=TimeTrialTests.Create(new RaceEventLog(),track,3,snow,ski,athlete);
            while(!session.HasFinished&&session.Clock.Tick.Value<100000)
            {
                // Repeated 60 s work / 20 s recovery exercises reserve/recovery as well as sustainable power.
                int effort=session.Clock.Tick.Value%1600<1200?850:550;
                session.Step(TimeTrialTests.Input(session,effort,tuck:true,sprint:true));
            }
            Assert.That(session.HasFinished,Is.True);return session.FinishTime.Seconds;
        }
        [Test] public void TenPointSkiMarginalsDoNotOverwhelmAllOtherStatsCombined()
        {
            var text=new StringBuilder();var totals=new Dictionary<string,double>();foreach(string stat in Stats)totals[stat]=0;
            foreach(int baseline in new[]{40,60,80})foreach(string track in Tracks)
            {
                double original=Trial(track,Athlete(baseline));
                foreach(string stat in Stats)
                {
                    double changed=Trial(track,Athlete(baseline,stat));double benefit=original-changed;
                    var row=new BalanceMetrics();row.Values["baseline_stat"]=baseline;row.Values["track"]=track;row.Values["stat"]=stat;
                    row.Values["baseline_seconds"]=original;row.Values["changed_seconds"]=changed;
                    row.Values["seconds_saved_per_point"]=benefit/10;row.Values["fraction_saved"]=benefit/original;
                    text.Append(row.ToJson()).Append('\n');totals[stat]+=benefit/original;
                    Assert.That(Math.Abs(benefit/original),Is.LessThan(.1),stat+" single increment envelope");
                }
            }
            DraftCalibrationTests.Artifact("wp098-ski.jsonl",text.ToString());
            double sum=0,max=0;foreach(var pair in totals){sum+=Math.Max(0,pair.Value);max=Math.Max(max,pair.Value);TestContext.Out.WriteLine(pair.Key+" aggregate fraction="+pair.Value);}
            Assert.That(max,Is.LessThan(sum-max),"No single stat exceeds all other positive marginal effects combined across the terrain/baseline matrix.");
        }
        [Test] public void EquipmentProgressionRemainsBelowBroadAthleteSkillProgression()
        {
            var text=new StringBuilder();
            foreach(string track in Tracks)foreach(string snow in new[]{"packed_neutral","fresh_soft","icy_fast","wet_slow"})
            {
                var athlete=Athlete(50);double baseline=Trial(track,athlete,snow);double skilled=Trial(track,Athlete(50,"all"),snow);
                foreach(string ski in new[]{"ski_glide","ski_soft_specialist"})
                {
                    double equipped=Trial(track,athlete,snow,ski);var row=new BalanceMetrics();
                    row.Values["track"]=track;row.Values["snow"]=snow;row.Values["ski"]=ski;row.Values["baseline_seconds"]=baseline;
                    row.Values["equipment_seconds_saved"]=baseline-equipped;row.Values["all_stats_plus10_seconds_saved"]=baseline-skilled;
                    row.Values["equipment_fraction_saved"]=(baseline-equipped)/baseline;text.Append(row.ToJson()).Append('\n');
                    Assert.That(Math.Abs((baseline-equipped)/baseline),Is.LessThan(.03));
                    Assert.That(baseline-equipped,Is.LessThan(baseline-skilled),track+" / "+snow+" / "+ski);
                }
            }
            DraftCalibrationTests.Artifact("wp099.jsonl",text.ToString());
        }
        [Test] public void ShootingMarginalsReportBothAccuracyAndTimeWithoutHiddenHitRolls()
        {
            var suite=ShootingBalanceTests.Suite();var model=ShootingSessionTests.Model();var ai=ShootingAiTests.Definition();
            var text=new StringBuilder();
            foreach(int scenarioIndex in new[]{2,3})
            {
                var scenario=suite.Get(scenarioIndex);int baseHits=0;long baseTicks=0;
                for(int statIndex=-1;statIndex<5;statIndex++)
                {
                    string[] stats={"breath_control","prone_stability","standing_stability","shooting_speed","composure"};
                    string json=ShootingSessionTests.Athlete().ToJson();
                    if(statIndex>=0){int value=stats[statIndex].Contains("stability")?80:60;json=PhysiologyContractTests.ReplaceNumber(json,stats[statIndex],value.ToString());}
                    var athlete=ShootingAthleteDefinition.FromJson(json);int hits=0;long ticks=0;
                    for(ulong seed=51000;seed<51500;seed++){var result=ShootingBalanceRunner.Run(suite,scenario,model,athlete,ai,seed);hits+=result.Hits;ticks+=result.Ticks;}
                    if(statIndex<0){baseHits=hits;baseTicks=ticks;continue;}
                    var row=new BalanceMetrics();row.Values["stance"]=scenario.Stance.ToString();row.Values["stat"]=stats[statIndex];
                    row.Values["base_hits"]=baseHits;row.Values["changed_hits"]=hits;row.Values["shots"]=2500;
                    row.Values["hit_percentage_points_per_stat_point"]=(hits-baseHits)/2500.0*10;
                    row.Values["bout_seconds_saved_per_stat_point"]=(baseTicks-ticks)/(500.0*suite.Frequency*10);
                    text.Append(row.ToJson()).Append('\n');
                    if(stats[statIndex]=="composure"||(stats[statIndex]=="prone_stability"&&scenarioIndex==3)||(stats[statIndex]=="standing_stability"&&scenarioIndex==2))
                    {Assert.That(hits,Is.EqualTo(baseHits));Assert.That(ticks,Is.EqualTo(baseTicks));}
                }
            }
            DraftCalibrationTests.Artifact("wp098-shooting.jsonl",text.ToString());
        }
    }
}

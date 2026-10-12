using System;
using System.IO;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Optional replay owns files only; career completion never depends on successful recording.</summary>
    internal sealed class CareerReplayCapture:IManualRaceCommandObserver,IDisposable
    {
        private readonly RaceReplayLibrary library;
        private readonly Stream stream;
        private readonly ManualRaceJournalWriter writer;
        private readonly ManualScopedFieldStepper stepper;
        private readonly RaceReplayInfo info;
        private readonly RaceHighlightDetector highlights;
        private readonly int observationTicks;
        private bool published;
        internal CareerReplayCapture(RaceReplayLibrary library,ManualScopedFieldStepper stepper,RaceReplayInfo info,RaceHighlightDetector highlights,ReplayPresentationSettings settings)
        {
            this.library=library;this.stepper=stepper;this.info=info;this.highlights=highlights;
            observationTicks=Math.Max(1,(int)Math.Round(settings.tv_refresh_seconds*stepper.Field.TickFrequency));
            stream=library.CreatePending(info.Id);
            try{writer=new ManualRaceJournalWriter(stream,stepper,SkiTechniqueDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/ski_technique_provisional_v1")));}
            catch{stream.Dispose();library.DiscardPending(info.Id);throw;}
            stepper.CommandObserver=this;
        }
        public void RecordAccepted(SimulationTick tick,ReadOnlySpan<SprintAthleteInput> inputs,ReadOnlySpan<int> steering,ReadOnlySpan<double> brakes)
        {
            writer.RecordAccepted(tick,inputs,steering,brakes);
            if(info.Format==(int)CompetitionFormat.Sprint||tick.Value%observationTicks!=0)return;
            int leader=-1;long distance=-1;
            for(int i=0;i<stepper.Field.Count;i++){var a=stepper.Field.GetAthlete(i);if(a.RouteNanometers>distance){distance=a.RouteNanometers;leader=i;}}
            if(leader>=0&&distance>0)highlights.ObserveLeader(tick.Value,stepper.Field.GetAthlete(leader).AthleteId.Value);
        }
        internal void Publish(CareerRaceRecord result)
        {
            if(published)return;stepper.CommandObserver=null;writer.Complete();writer.Dispose();stream.Dispose();
            info.TickCount=writer.TickCount;info.ResultSha256=result.AuthoritySha256;
            if(result.Count>1)for(int i=1;i<result.Count;i++)highlights.ObserveRegisteredFinish(result.Get(i).AthleteId,(result.Get(i).RankTimeMilliseconds-result.Get(0).RankTimeMilliseconds)/1000.0);
            info.Events=highlights.Events();info.Highlights=highlights.Highlights();library.Publish(info);published=true;
        }
        public void Dispose(){if(stepper.CommandObserver==this)stepper.CommandObserver=null;writer.Dispose();stream.Dispose();if(!published)library.DiscardPending(info.Id);}
    }
}

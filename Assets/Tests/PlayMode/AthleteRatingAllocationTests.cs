using System;
using Biathlon.Career;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using Unity.Profiling;

namespace Biathlon.PlayModeTests
{
    public sealed class AthleteRatingAllocationTests
    {
        private static object allocationSink;
        private static string Read(string name)=>ProductionVenueHost.ReadResource("Biathlon/"+System.IO.Path.GetFileNameWithoutExtension(name));
        [Test] public void WarmRatingProjectionAllocatesNoManagedMemoryWithNativePositiveControl()
        {
            var d=CareerDefinitions.ForNewProductionCareer(Read);var a=CareerFactory.Create(d.Roster,d.Calendar,"rating_allocations","Alex","NOR","default",new SeededRandomSource(71000)).Athlete(0);
            var definition=BiathlonRatingDefinition.FromJson(Read("career_ratings_v1.json"));
            Measure(a,definition,10,false,out _);Measure(a,definition,10,true,out _);
            long bytes=Measure(a,definition,1000,false,out int sum);long control=Measure(a,definition,1000,true,out _);
            Assert.That(sum,Is.GreaterThan(0));Assert.That(bytes,Is.Zero);Assert.That(control,Is.GreaterThanOrEqualTo(16000),"Native counter must detect retained real allocations.");
            TestContext.Out.WriteLine("GC.Alloc native projection_bytes="+bytes+" positive_control_bytes="+control);
        }
        private static long Measure(CareerAthlete athlete,BiathlonRatingDefinition definition,int count,bool positiveControl,out int sum)
        {
            // Mono GetAllocatedBytesForCurrentThread was measured to return0 for
            // actual retained arrays on this Editor; record native allocation events.
            using(var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC.Alloc",2048,
                ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                if(!recorder.Valid)throw new InvalidOperationException("Native allocation recorder unavailable.");
                sum=0;for(int i=0;i<count;i++){sum+=BiathlonRatingEngine.Calculate(athlete,definition).Overall;if(positiveControl)allocationSink=new byte[16];}
                recorder.Stop();long bytes=0;for(int i=0;i<recorder.Count;i++)bytes+=recorder.GetSample(i).Value;
                GC.KeepAlive(allocationSink);allocationSink=null;return bytes;
            }
        }
    }
}

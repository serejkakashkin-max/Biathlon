using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct SkiContactMotion
    {
        public SkiPlanarVelocity Velocity {get;}
        public double YawRate {get;}
        public SkiContactMotion(SkiPlanarVelocity velocity,double yawRate)
        {Velocity=velocity;YawRate=PhysiologyNumbers.RequireRange(yawRate,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(yawRate));}
    }
    public enum SkiContactGroupStatus { Resolved, Penetrating, UnresolvedGeometry, WorkLimit }
    public readonly struct SkiContactGroupPreview
    {
        public bool IsValid {get;}
        public SkiContactGroupStatus Status {get;}
        public int Passes {get;}
        public int Impulses {get;}
        public double MaximumClosingSpeed {get;}
        internal SkiContactGroupPreview(SkiContactGroupStatus status,int passes,int impulses,double closing)
        {IsValid=true;Status=status;Passes=passes;Impulses=impulses;MaximumClosingSpeed=closing;}
    }

    /// <summary>Deterministic simultaneous-contact velocity preview at fixed reached
    /// poses. Caller-owned output is provisional even on failure; never commits motion.</summary>
    public sealed class ManualSkiContactGroup
    {
        private readonly ManualSkiContactSystem pair;
        private readonly SkiContactGroupDefinition definition;
        private double convergence;
        public string ContextJson {get;}
        public ManualSkiContactGroup(OccupancyDefinition body,SkiContactDefinition contact,SkiContactGroupDefinition definition)
        {
            if(definition==null)throw new ArgumentNullException(nameof(definition));
            pair=new ManualSkiContactSystem(body,contact);this.definition=definition;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_contact_group_v4_explicit_convergence_grounded_mat",pair.AngularContextJson,definition.ToJson()});
        }
        public SkiContactGroupPreview Preview(ManualSkiContactBody[] bodies,int count,SkiContactMotion[] output,int[] order)
            =>Preview(bodies,count,output,order,null);
        internal SkiContactGroupPreview Preview(ManualSkiContactBody[] bodies,int count,SkiContactMotion[] output,int[] order,bool[] participants)
        {
            if(bodies==null||output==null||order==null||count<1||count>bodies.Length||count>output.Length||count>order.Length)
                throw new ArgumentException("Valid bodies and caller-owned motion/order buffers required.");
            if(participants!=null&&participants.Length!=count)throw new ArgumentException("Exact attested contact presence required.");
            // Validate all actors before changing scratch/output, including duplicates.
            for(int i=0;i<count;i++)
            {
                if(!bodies[i].IsValid)throw new ArgumentException("Initialized bodies required.");
                for(int j=0;j<i;j++)if(bodies[i].Athlete.Equals(bodies[j].Athlete))throw new ArgumentException("Duplicate contact actor.");
            }
            for(int i=0;i<count;i++)
            {
                int at=i;
                while(at>0&&string.CompareOrdinal(bodies[order[at-1]].Athlete.Value,bodies[i].Athlete.Value)>0)
                {order[at]=order[at-1];at--;}
                order[at]=i;
                output[i]=new SkiContactMotion(bodies[i].Velocity,bodies[i].Pose.YawRateRadiansPerSecond);
            }
            double speedScale=1;
            for(int i=0;i<count;i++)speedScale=Math.Max(speedScale,Math.Abs(output[i].Velocity.X)+Math.Abs(output[i].Velocity.Z)+Math.Abs(output[i].YawRate));
            convergence=definition.ImpactClosingSpeedTolerance;
            for(int i=0;i<count;i++)for(int j=i+1;j<count;j++)
                if((participants==null||participants[order[i]]&&participants[order[j]])&&
                    pair.Preview(bodies[order[i]],bodies[order[j]]).Status==SkiContactPreviewStatus.Penetrating)
                    return new SkiContactGroupPreview(SkiContactGroupStatus.Penetrating,0,0,0);

            int impulses=0;
            for(int pass=1;pass<=definition.MaximumPasses;pass++)
            {
                for(int i=0;i<count;i++)for(int j=i+1;j<count;j++)
                {
                    int ai=order[i],bi=order[j];if(participants!=null&&(!participants[ai]||!participants[bi]))continue;
                    var a=WithVelocity(bodies[ai],output[ai]);var b=WithVelocity(bodies[bi],output[bi]);
                    var manifold=pair.PreviewManifold(a,b);
                    if(manifold.Contact.Status==SkiContactPreviewStatus.Separated)continue;
                    if(!manifold.HasSupport)
                        return new SkiContactGroupPreview(SkiContactGroupStatus.UnresolvedGeometry,pass,impulses,0);
                    // Preserve the exact symmetric face impact first; endpoint
                    // corrections are needed only for residual closing spin.
                    Apply(ai,bi,manifold.MiddleX,manifold.MiddleZ);
                    Apply(ai,bi,manifold.FirstX,manifold.FirstZ);
                    if(manifold.FirstX!=manifold.SecondX||manifold.FirstZ!=manifold.SecondZ)
                        Apply(ai,bi,manifold.SecondX,manifold.SecondZ);
                }
                // Later impulses can close an earlier pair again. Inspect the entire
                // final group, not just the last pair or the count of applied impulses.
                double remaining=0;
                for(int i=0;i<count;i++)for(int j=i+1;j<count;j++)
                {
                    int ai=order[i],bi=order[j];if(participants!=null&&(!participants[ai]||!participants[bi]))continue;
                    var a=WithVelocity(bodies[ai],output[ai]);var b=WithVelocity(bodies[bi],output[bi]);
                    var manifold=pair.PreviewManifold(a,b);
                    if(manifold.Contact.Status==SkiContactPreviewStatus.Separated)continue;
                    if(!manifold.HasSupport)
                        return new SkiContactGroupPreview(SkiContactGroupStatus.UnresolvedGeometry,pass,impulses,remaining);
                    Check(manifold.FirstX,manifold.FirstZ);
                    Check(manifold.SecondX,manifold.SecondZ);
                    void Check(double x,double z)
                    {
                        var hit=pair.PreviewSupportContact(a,output[ai].YawRate,b,output[bi].YawRate,x,z);
                        remaining=Math.Max(remaining,Closing(a,output[ai],b,output[bi],hit));
                    }
                }
                if(remaining<=convergence)
                    return new SkiContactGroupPreview(SkiContactGroupStatus.Resolved,pass,impulses,remaining);
                if(pass==definition.MaximumPasses)
                    return new SkiContactGroupPreview(SkiContactGroupStatus.WorkLimit,pass,impulses,remaining);
            }
            throw new InvalidOperationException("Validated contact pass limit required.");
            void Apply(int ai,int bi,double x,double z)
            {
                var a=WithVelocity(bodies[ai],output[ai]);var b=WithVelocity(bodies[bi],output[bi]);
                var hit=pair.PreviewSupportContact(a,output[ai].YawRate,b,output[bi].YawRate,x,z);
                if(hit.Contact.Status!=SkiContactPreviewStatus.Impulse||Closing(a,output[ai],b,output[bi],hit)<=convergence)return;
                output[ai]=new SkiContactMotion(hit.Contact.FirstVelocity,hit.FirstYawRate);
                output[bi]=new SkiContactMotion(hit.Contact.SecondVelocity,hit.SecondYawRate);impulses++;
            }
        }
        private static ManualSkiContactBody WithVelocity(ManualSkiContactBody body,SkiContactMotion motion)
            =>new ManualSkiContactBody(body.Athlete,body.Pose,motion.Velocity,body.MassKilograms,body.Grounded);
        private static double Closing(ManualSkiContactBody a,SkiContactMotion am,ManualSkiContactBody b,SkiContactMotion bm,SkiAngularContactPreview hit)
        {
            double x=am.Velocity.X+am.YawRate*(hit.ContactZ-a.Pose.Z)-bm.Velocity.X-bm.YawRate*(hit.ContactZ-b.Pose.Z);
            double z=am.Velocity.Z-am.YawRate*(hit.ContactX-a.Pose.X)-bm.Velocity.Z+bm.YawRate*(hit.ContactX-b.Pose.X);
            return -(x*hit.Contact.NormalX+z*hit.Contact.NormalZ);
        }
    }
}

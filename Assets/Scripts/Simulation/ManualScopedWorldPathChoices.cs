using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed partial class ManualScopedWorldRoster
    {
        private ManualRacePathCatalog[] pathFamily;
        private ManualRacePathProgression[] familyProgress;
        private ManualRouteTubeSweep[] familyCourses;
        private ManualRouteRibbonSweep[] familyRibbons;
        private ManualRacePathGateSweep[] familyGates;
        private ManualRacePathBoundaryScheduler[] familyBoundaries;
        private int[] proposedFamilyChoice;
        private ManualRacePathCursor[] reboundCursors;
        private ManualRacePathActorFrame[] reboundActors;
        public int PathChoiceCount=>pathFamily?.Length??0;
        public ManualRacePathCatalog GetPathChoice(int index)=>pathFamily==null?throw new InvalidOperationException("No attested reusable path family."):pathFamily[index];
        internal void BindPathFamily(PackSprintFieldSession field,ManualRacePathCatalog[] family,SkiContactSweepDefinition work)
        {
            if(!IsVerifiedInitialField(field)||!field.UsesReusableRangeMats||pathFamily!=null||family==null||
                family.Length!=field.RangeReservations.LaneCount||work==null||work.ToJson()!=WorkContextJson)
                throw new ArgumentException("Initial attested reusable field and exact path/work family required.");
            for(int p=0;p<family.Length;p++)
                if(family[p]==null||!catalogs[0].IsSamePathFamily(family[p])||family[p].SelectedPath.LaneOrdinal!=p+1||
                    !ReferenceEquals(family[p].SelectedPath,field.GetAthlete(0).GetRangePathChoice(p)))
                    throw new ArgumentException("Reusable catalogs must bind the exact field's cold path choices.");
            pathFamily=(ManualRacePathCatalog[])family.Clone();
            familyProgress=new ManualRacePathProgression[family.Length];familyCourses=new ManualRouteTubeSweep[family.Length];
            familyRibbons=new ManualRouteRibbonSweep[family.Length];familyGates=new ManualRacePathGateSweep[family.Length];
            familyBoundaries=new ManualRacePathBoundaryScheduler[family.Length];
            var context=new string[family.Length+2];context[0]="scoped_reusable_shared_prefix_catalog_family_v1";
            context[1]="cold_choices;paid_whole_tick_common_prefix;exact_pose_velocity_credit_and_surface;no_fork_switch;resource_authority_required";
            for(int p=0;p<family.Length;p++)
            {
                if(matCapture!=null)family[p].ProjectionGeometry.MatPositionTolerance=matCapture.PositionTolerance;
                var c=family[p];familyProgress[p]=new ManualRacePathProgression(c);
                familyCourses[p]=new ManualRouteTubeSweep(c.ProjectionGeometry,occupiedBody,work);
                familyRibbons[p]=new ManualRouteRibbonSweep(c.ProjectionGeometry,occupiedBody,work,numericalTolerance);
                familyGates[p]=new ManualRacePathGateSweep(c,work);familyBoundaries[p]=new ManualRacePathBoundaryScheduler(c,work);
                context[p+2]=ManualContextIdentity.Sha256(c.ContextJson);
            }
            proposedFamilyChoice=new int[Count];reboundCursors=new ManualRacePathCursor[Count];reboundActors=new ManualRacePathActorFrame[Count];
            ClearPathChoices();var extra=ShootingModelDefinitions.EncodeContext(context);
            ContextJson=ContextJson.Substring(0,ContextJson.Length-1)+","+extra.Substring(1);
        }

        internal void StagePaidPathChoice(ManualScopedWorldPreview proposal,int actor,int laneOrdinal,int bout)
        {
            ValidatePrepared(proposal);
            if(pathFamily==null||actor<0||actor>=Count||laneOrdinal<1||laneOrdinal>pathFamily.Length||proposedFamilyChoice[actor]>=0)
                throw new ArgumentException("A unique attested resource path choice is required.");
            var old=catalogs[actor];var next=pathFamily[laneOrdinal-1];var cursor=proposedCursors[actor];var frame=staged[actor];
            var row=old.Get(cursor.OccurrenceIndex);var target=next.Get(cursor.OccurrenceIndex);
            if(row.Kind!=ManualRacePathKind.RangeApproach||row.BoutIndex!=bout||target.BoutIndex!=bout||
                row.Kind!=target.Kind||row.BaseSectionIndex!=target.BaseSectionIndex||row.LoopId!=target.LoopId||
                row.LocalStartNanometers!=target.LocalStartNanometers||row.RouteStartNanometers!=target.RouteStartNanometers||
                cursor.CreditedRouteNanometers>=next.SelectedPath.FirstForkRouteNanometers(row.LapIndex)||
                !next.TryProject(frame.Pose,cursor.OccurrenceIndex,cursor.ObservedArcNanometers,0,out var observation)||
                observation.PhysicalArcNanometers!=cursor.ObservedArcNanometers||observation.MappedRouteNanometers!=frame.Observation.MappedRouteNanometers||
                observation.LateralMeters!=frame.Observation.LateralMeters||observation.ForwardX!=frame.Observation.ForwardX||
                observation.ForwardZ!=frame.Observation.ForwardZ||observation.HeightMeters!=frame.Observation.HeightMeters||
                next.SampleSurface(observation).SegmentId!=old.SampleSurface(frame.Observation).SegmentId)
                throw new ArgumentException("A selected catalog must preserve the exact paid common physical prefix.");
            reboundCursors[actor]=new ManualRacePathCursor(next,cursor.Athlete,cursor.OccurrenceIndex,cursor.ObservedArcNanometers,cursor.CreditedArcNanometers,cursor.CreditedRouteNanometers);
            reboundActors[actor]=new ManualRacePathActorFrame(frame.Athlete,frame.Pose,frame.Velocity,observation);
            proposedFamilyChoice[actor]=laneOrdinal-1;
        }
        private void ClearPathChoices()
        {if(proposedFamilyChoice!=null)for(int i=0;i<Count;i++)proposedFamilyChoice[i]=-1;}
        private void CommitPathChoices()
        {
            if(proposedFamilyChoice==null)return;
            for(int i=0;i<Count;i++)
            {
                int p=proposedFamilyChoice[i];if(p<0)continue;
                catalogs[i]=pathFamily[p];progress[i]=familyProgress[p];courses[i]=familyCourses[p];ribbons[i]=familyRibbons[p];
                gates[i]=familyGates[p];boundaries[i]=familyBoundaries[p];cursors[i]=reboundCursors[i];accepted[i]=reboundActors[i];
            }
            ClearPathChoices();
        }
    }
}

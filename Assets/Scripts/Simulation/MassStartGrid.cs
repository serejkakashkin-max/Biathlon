using System;
using System.Globalization;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct MassStartGridSlot
    {
        public AthleteId AthleteId { get; }
        public int StartOrdinal { get; }
        public int Row { get; }
        public int Column { get; }
        /// <summary>Signed position relative to the front-row reference; rear rows are negative.
        /// This is a start-area coordinate, never credited as completed route distance.</summary>
        public long LongitudinalNanometers { get; }
        public long LateralNanometers { get; }
        internal MassStartGridSlot(AthleteId athlete,int ordinal,int row,int column,long longitudinal,long lateral)
        { AthleteId=athlete;StartOrdinal=ordinal;Row=row;Column=column;LongitudinalNanometers=longitudinal;LateralNanometers=lateral; }
    }
    /// <summary>Immutable physical start-area layout. Roster order is supplied by competition
    /// qualification/seeding; no hidden reordering or capability-based assignment occurs here.</summary>
    public sealed class MassStartGrid
    {
        private readonly MassStartGridSlot[] slots;
        public int Count=>slots.Length;
        public string ContextJson { get; }
        /// <summary>Translation into the nonnegative coordinate space used by the pack kernel.
        /// This origin shift is position only, not distance travelled by an athlete.</summary>
        public long FrontReferenceNanometers { get; }
        public MassStartGridSlot Get(int index)=>slots[index];
        public SkiPlacement[] CreateLaunchPlacements(string corridorId)
        {
            if(string.IsNullOrWhiteSpace(corridorId))throw new ArgumentException("Stable start corridor ID required.");
            var result=new SkiPlacement[Count];
            for(int i=0;i<Count;i++)result[i]=new SkiPlacement(slots[i].AthleteId,corridorId,
                new SkiState(checked(FrontReferenceNanometers+slots[i].LongitudinalNanometers),0),slots[i].LateralNanometers/1e9);
            return result;
        }
        public MassStartGrid(CompetitionProfile profile,MassStartGridDefinition definition,
            OccupancyDefinition body,ReadOnlySpan<AthleteId> roster,double widthMeters,double depthMeters)
        {
            if(profile==null || definition==null || body==null) throw new ArgumentNullException();
            if(profile.Format!=CompetitionFormat.MassStart || profile.StartType!=CompetitionStartType.Simultaneous ||
                roster.Length==0 || roster.Length>profile.FieldCapacity) throw new ArgumentException("Valid Mass Start roster required.");
            PhysiologyNumbers.RequireRange(widthMeters,0.001,1000000,nameof(widthMeters));
            PhysiologyNumbers.RequireRange(depthMeters,0.001,1000000,nameof(depthMeters));
            int rows=(roster.Length+definition.Columns-1)/definition.Columns;
            if((roster.Length>1 && definition.Columns>1 && definition.ColumnSpacing.Meters<body.LateralSeparation) ||
                (rows>1 && definition.RowSpacing.Meters<body.LongitudinalSeparation))
                throw new ArgumentException("Grid spacing overlaps the configured physical bodies/clearance.");
            double halfSpan=(definition.Columns-1)*definition.ColumnSpacing.Meters/2;
            if(halfSpan+body.EdgeMargin>widthMeters/2 || (rows-1)*definition.RowSpacing.Meters+body.BodyLength/2>depthMeters)
                throw new ArgumentException("Authored start area cannot contain the grid and body margins.");
            slots=new MassStartGridSlot[roster.Length];
            FrontReferenceNanometers=checked((rows-1)*definition.RowSpacing.Millimeters*SkiState.NanometersPerMillimeter+
                (long)Math.Ceiling(body.BodyLength/2*SkiState.NanometersPerMeter));
            var context=new string[7+roster.Length];
            context[0]="mass_start_grid_front_reference_v1";context[1]=profile.ToJson();context[2]=definition.ToJson();context[3]=body.ToJson();
            context[4]=widthMeters.ToString("R",CultureInfo.InvariantCulture);context[5]=depthMeters.ToString("R",CultureInfo.InvariantCulture);
            context[6]="row_major_roster;front_row_center_zero;negative_rear_coordinates;no_route_credit";
            for(int i=0;i<roster.Length;i++)
            {
                if(!roster[i].IsValid)throw new ArgumentException("Valid stable grid identity required.");
                for(int j=0;j<i;j++)if(roster[j].Equals(roster[i]))throw new ArgumentException("Duplicate grid athlete.");
                int row=i/definition.Columns,column=i%definition.Columns;
                long longitudinal=checked(-row*definition.RowSpacing.Millimeters*SkiState.NanometersPerMillimeter);
                long lateral=checked((2L*column-(definition.Columns-1))*definition.ColumnSpacing.Millimeters*SkiState.NanometersPerMillimeter/2);
                slots[i]=new MassStartGridSlot(roster[i],i+1,row,column,longitudinal,lateral);context[7+i]=roster[i].Value;
            }
            ContextJson=ShootingModelDefinitions.EncodeContext(context);
        }
    }
}

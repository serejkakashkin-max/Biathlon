"""Pure serialized equivalent of TrackSampler width lookup at visual knots."""
import bisect

def authored_corridor_widths(track_loop,visual_loop):
    cumulative=0;ends=[]
    for segment in track_loop['segments']:
        length=round(segment['length_m']*1000)
        assert abs(length/1000-segment['length_m'])<1e-9
        cumulative+=length;ends.append(cumulative)
    result=[]
    for knot in visual_loop['knots']:
        coordinate=round(knot['distance_m']*1000)
        assert 0<=coordinate<=cumulative and abs(coordinate/1000-knot['distance_m'])<1e-9
        # Exact interior boundaries sample the next segment; the terminal knot
        # samples the last segment in Reject mode, including a closed loop.
        index=min(bisect.bisect_right(ends,coordinate),len(ends)-1)
        result.append(track_loop['segments'][index]['width_m'])
    return result

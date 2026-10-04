"""Bounded data transform for the existing cardinal range authoring recipes."""
import re

def reflect_range_family(value,lane_count):
    # Reflection is its own inverse; lane 1 stays on the right facing the bank.
    if isinstance(value,str):
        return re.sub(r'(?<![A-Za-z0-9])(?:mat|lane|fork|outbound_cross|merge|mat_turn)_(\d{2})(?!\d)',
                      lambda m:m.group(0)[:-2]+f'{lane_count+1-int(m.group(1)):02d}',value)
    if isinstance(value,list):return [reflect_range_family(x,lane_count) for x in value]
    if isinstance(value,dict):
        result={k:reflect_range_family(v,lane_count) for k,v in value.items()}
        if set(('x','y','z'))<=value.keys():result['x']=-value['x']
        if 'curvature_per_m' in value:result['curvature_per_m']=-value['curvature_per_m']
        if 'yaw_deg' in value:result['yaw_deg']=-value['yaw_deg']
        return result
    return value

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable]
    public sealed class WoodlandServicesDefinition
    {
        [Serializable]
        public sealed class Site : ProductionPrefabPlacement
        {
            // Includes the complete prop footprint and space around the roof.
            public float clearance_m;
            internal override void Validate(HashSet<string> ids)
            {
                base.Validate(ids);
                ProductionEnvironmentDefinition.Bound(clearance_m,3,12);
            }
        }

        public int schema_version;
        public string id;
        public Site[] sites;
        public float mountain_backdrop;

        public static WoodlandServicesDefinition FromJson(string json,ProductionEnvironmentDefinition venue)
        {
            var definition=JsonUtility.FromJson<WoodlandServicesDefinition>(json);
            if(definition?.schema_version!=1)throw new ArgumentException("Invalid service scenery schema.");
            ProductionEnvironmentDefinition.Identity(definition.id);
            ProductionEnvironmentDefinition.Collection(definition.sites,1,20);
            ProductionEnvironmentDefinition.Bound(definition.mountain_backdrop,0,1);
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var site in definition.sites)
            {
                if(site==null)throw new ArgumentException("A service site is missing.");
                site.Validate(ids);
                float radius=site.clearance_m;
                if(CourseFloodlightLayout.Clearance(venue,site.position)<radius+1)
                    throw new ArgumentException("Service scenery must clear every race corridor: "+site.id);
                var origin=venue.range.origin;
                var direction=venue.range.forward;
                var side=Vector3.Cross(Vector3.up,direction);
                var fromOrigin=site.position-origin;
                float depth=Vector3.Dot(fromOrigin,direction);
                float lateral=Vector3.Dot(fromOrigin,side);
                var banks=venue.range.banks;
                float first=Vector3.Dot(banks[0].mat_position-origin,side);
                float last=Vector3.Dot(banks[banks.Length-1].mat_position-origin,side);
                if(depth>-radius-5&&depth<venue.range.target_distance_m+radius+5&&
                    lateral>Mathf.Min(first,last)-radius-4&&lateral<Mathf.Max(first,last)+radius+4)
                    throw new ArgumentException("Service scenery must clear the firing range: "+site.id);
                foreach(var other in definition.sites)
                    if(other!=null&&other!=site&&DistanceSquared(site.position,other.position)<Mathf.Pow(radius+other.clearance_m,2))
                        throw new ArgumentException("Service scenery footprints overlap: "+site.id);
            }
            return definition;
        }

        internal bool Clears(Vector3 point,float margin)
        {
            foreach(var site in sites)
            {
                float radius=site.clearance_m+margin;
                if(DistanceSquared(point,site.position)<radius*radius)return false;
            }
            return true;
        }

        private static float DistanceSquared(Vector3 a,Vector3 b)
        {
            float x=a.x-b.x,z=a.z-b.z;
            return x*x+z*z;
        }
    }
}

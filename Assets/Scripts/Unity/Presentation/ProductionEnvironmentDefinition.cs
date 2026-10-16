using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class ProductionEnvironmentDefinition
    {
        public int schema_version;
        public string id,venue_id,manifest_resource,track_resource,visual_resource;
        public ProductionTerrainRecipe terrain;
        public ProductionRouteCorridor[] route_corridors;
        public ProductionForestRecipe forest;
        public ProductionRangeRecipe range;
        public ProductionPrefabPlacement[] assets;
        public ProductionMeshPlacement[] mesh_assets;
        public ProductionCrowdRecipe crowd;
        public ProductionLightingRecipe lighting;
        public static ProductionEnvironmentDefinition FromJson(string json)
        {
            // Overwrite explicit defaults so legacy JSON omitting the optional atmosphere
            // scale retains 1, while an authored zero still fails validation.
            var value=new ProductionEnvironmentDefinition{lighting=new ProductionLightingRecipe()};
            JsonUtility.FromJsonOverwrite(json,value);
            // Unity's inline class serializer materializes absent/null nested objects.
            // Read this optional member with the project's existing JSON serializer
            // family so omission stays distinct from an explicitly invalid object.
            if(value.forest!=null)value.forest.high_visibility=ReadHighVisibility(json);
            if(value==null||value.schema_version!=1)throw new ArgumentException("Invalid production environment schema.");
            value.Validate();return value;
        }
        public string ToJson()
        {
            string json=JsonUtility.ToJson(this);
            if(forest!=null&&forest.high_visibility==null)
                json=json.Replace("\"high_visibility\":"+JsonUtility.ToJson(new ProductionForestHighVisibility()),"\"high_visibility\":null");
            return json;
        }
        [DataContract] private sealed class PolicyEnvelope {[DataMember] public PolicyForest forest=null;}
        [DataContract] private sealed class PolicyForest {[DataMember] public ProductionForestHighVisibility high_visibility=null;}
        private static ProductionForestHighVisibility ReadHighVisibility(string json)
        {
            try
            {
                using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    return ((PolicyEnvelope)new DataContractJsonSerializer(typeof(PolicyEnvelope)).ReadObject(stream))?.forest?.high_visibility;
            }
            catch(SerializationException error){throw new ArgumentException("Invalid production High forest policy JSON.",nameof(json),error);}
            catch(InvalidDataContractException error){throw new ArgumentException("Invalid production High forest policy contract.",nameof(json),error);}
        }
        internal void Validate()
        {
            Identity(id);Identity(venue_id);Resource(manifest_resource);Resource(track_resource);Resource(visual_resource);
            if(terrain==null||forest==null||range==null||crowd==null||lighting==null)throw new ArgumentException("Complete production environment required.");
            var ids=new HashSet<string>(StringComparer.Ordinal);terrain.Validate(ids);forest.Validate(ids);range.Validate(ids);lighting.Validate();
            Collection(route_corridors,1,1024);Collection(assets,0,10000);Collection(mesh_assets,0,10000);
            foreach(var corridor in route_corridors){if(corridor==null)throw new ArgumentException("Null route corridor.");corridor.Validate();}
            foreach(var asset in assets){if(asset==null)throw new ArgumentException("Null prefab placement.");asset.Validate(ids);}
            foreach(var asset in mesh_assets){if(asset==null)throw new ArgumentException("Null mesh placement.");asset.Validate(ids);}
            if(crowd.mode!="owner_approved_simplified"||crowd.resource_root!="Biathlon/Crowd"||crowd.restricted_resources_allowed)
                throw new ArgumentException("Production crowd requires the approved public simplified assets.");
        }
        internal static void Resource(string value)
        {
            VenueManifest.ResourceId(value);
            if(value.IndexOf("Restricted",StringComparison.OrdinalIgnoreCase)>=0)throw new ArgumentException("Restricted resources are not part of this environment contract.");
        }
        internal static void Identity(string value){Resource(value);if(value.IndexOf('/')>=0)throw new ArgumentException("Single-component stable identity required.");}
        internal static void Unique(string value,HashSet<string> ids){Identity(value);if(!ids.Add(value))throw new ArgumentException("Duplicate environment identity: "+value);}
        internal static void Bound(float value,float min,float max){if(!float.IsFinite(value)||value<min||value>max)throw new ArgumentException("Environment value out of bounds.");}
        internal static void Position(Vector3 value){Bound(value.x,-1000000,1000000);Bound(value.y,-1000000,1000000);Bound(value.z,-1000000,1000000);}
        internal static void Collection<T>(T[] value,int min,int max){if(value==null||value.Length<min||value.Length>max)throw new ArgumentException("Environment collection out of bounds.");}
    }
    [Serializable] public sealed class ProductionBounds {public Vector3 center,size;}
    [Serializable] public sealed class ProductionTerrainRecipe
    {
        public ProductionBounds bounds;public float tile_size_m,grid_spacing_m;
        public string blend_resource,material_resource,snow_shoulders_resource,snow_material_preset,snow_surface_profile;public ProductionTerrainTile[] tiles;
        internal void Validate(HashSet<string> ids)
        {
            if(bounds==null)throw new ArgumentException("Terrain bounds required.");ProductionEnvironmentDefinition.Position(bounds.center);ProductionEnvironmentDefinition.Position(bounds.size);
            ProductionEnvironmentDefinition.Bound(bounds.size.x,.1f,100000);ProductionEnvironmentDefinition.Bound(bounds.size.z,.1f,100000);
            ProductionEnvironmentDefinition.Bound(tile_size_m,.1f,10000);ProductionEnvironmentDefinition.Bound(grid_spacing_m,.1f,100);
            ProductionEnvironmentDefinition.Resource(blend_resource);ProductionEnvironmentDefinition.Resource(material_resource);ProductionEnvironmentDefinition.Collection(tiles,1,4096);
            if(!string.IsNullOrEmpty(snow_material_preset)&&snow_material_preset!="cold"&&snow_material_preset!="wet")throw new ArgumentException("Unknown production snow material.");
            if(!string.IsNullOrEmpty(snow_surface_profile)&&snow_surface_profile!="packed_neutral"&&snow_surface_profile!="fresh_soft"&&snow_surface_profile!="icy_fast"&&snow_surface_profile!="wet_slow")throw new ArgumentException("Unknown production snow surface.");
            if(!string.IsNullOrEmpty(snow_shoulders_resource))ProductionEnvironmentDefinition.Resource(snow_shoulders_resource);
            foreach(var tile in tiles)
            {
                if(tile==null)throw new ArgumentException("Null terrain tile.");ProductionEnvironmentDefinition.Unique(tile.id,ids);ProductionEnvironmentDefinition.Position(tile.center);ProductionEnvironmentDefinition.Position(tile.size);
                if(tile.size.y!=0||tile.size.x!=tile_size_m||tile.size.z!=tile_size_m||tile.columns!=(int)Math.Ceiling(tile.size.x/grid_spacing_m)+1||tile.rows!=(int)Math.Ceiling(tile.size.z/grid_spacing_m)+1||
                    tile.columns<2||tile.rows<2||(long)tile.columns*tile.rows>100000)throw new ArgumentException("Terrain grid dimensions do not match the declared spacing.");
                ProductionEnvironmentDefinition.Resource(tile.height_resource);
            }
        }
    }
    [Serializable] public sealed class ProductionTerrainTile {public string id,height_resource;public Vector3 center,size;public int columns,rows;}
    [Serializable] public sealed class ProductionTerrainHeights
    {
        public int schema_version,columns,rows;public string id,venue_id,tile_id;public float[] heights_m;
        internal static ProductionTerrainHeights Read(string json,ProductionTerrainTile tile,string venue)
        {
            var value=JsonUtility.FromJson<ProductionTerrainHeights>(json);
            if(value==null||value.schema_version!=1||value.venue_id!=venue||value.tile_id!=tile.id||value.columns!=tile.columns||value.rows!=tile.rows||value.heights_m==null||value.heights_m.Length!=(long)tile.columns*tile.rows)
                throw new ArgumentException("Terrain height file does not match its tile.");
            ProductionEnvironmentDefinition.Identity(value.id);foreach(var height in value.heights_m)ProductionEnvironmentDefinition.Bound(height,-1000000,1000000);return value;
        }
    }
    [Serializable] public sealed class ProductionRouteKnot {public float distance_m,x,y,z;public Vector3 Position=>new Vector3(x,y,z);}
    [Serializable] public sealed class ProductionRouteCorridor
    {
        public string loop_id,source_visual_resource;public ProductionRouteKnot[] knots;public float[] widths_m;
        internal void Validate()
        {
            ProductionEnvironmentDefinition.Identity(loop_id);ProductionEnvironmentDefinition.Resource(source_visual_resource);ProductionEnvironmentDefinition.Collection(knots,2,10000);
            if(widths_m==null||widths_m.Length!=knots.Length)throw new ArgumentException("Every corridor knot requires its authored width.");
            float prior=-1;foreach(var knot in knots){if(knot==null||!float.IsFinite(knot.distance_m)||knot.distance_m<=prior)throw new ArgumentException("Corridor knots must advance.");prior=knot.distance_m;ProductionEnvironmentDefinition.Position(knot.Position);}
            foreach(var width in widths_m)ProductionEnvironmentDefinition.Bound(width,.1f,200);
        }
    }
    [Serializable] public sealed class ProductionForestRecipe
    {
        public string definition_resource,composition_resource,instance_space;public int max_instances_per_group;public ProductionForestGroup[] groups;
        public ProductionForestHighVisibility high_visibility;
        internal void Validate(HashSet<string> ids)
        {
            ProductionEnvironmentDefinition.Resource(definition_resource);ProductionEnvironmentDefinition.Resource(composition_resource);
            high_visibility?.Validate();
            if(instance_space!="world"||max_instances_per_group<1||max_instances_per_group>1023)throw new ArgumentException("Explicit bounded world forest required.");
            ProductionEnvironmentDefinition.Collection(groups,0,4096);
            foreach(var group in groups){if(group==null||group.count<1||group.count>max_instances_per_group)throw new ArgumentException("Invalid forest group capacity.");ProductionEnvironmentDefinition.Unique(group.id,ids);ProductionEnvironmentDefinition.Position(group.center);ProductionEnvironmentDefinition.Position(group.size);ProductionEnvironmentDefinition.Resource(group.instances_resource);}
        }
    }
    /// <summary>Explicit production High opt-in; the bound weather owns its visibility horizon.</summary>
    [Serializable,DataContract] public sealed class ProductionForestHighVisibility
    {
        [DataMember] public int schema_version;
        [DataMember] public string id;
        [DataMember] public string mode;
        internal void Validate()
        {
            if(schema_version!=1||mode!="fog_frustum_v1")throw new ArgumentException("Unsupported production High forest visibility policy.");
            ProductionEnvironmentDefinition.Identity(id);
        }
    }
    [Serializable] public sealed class ProductionForestGroup {public string id,instances_resource;public Vector3 center,size;public int count;}
    [Serializable] public sealed class ProductionTree {public string id,species;public Vector3 position;public float scale,yaw_deg,density_rank;}
    [Serializable] public sealed class ProductionForestInstances
    {
        public int schema_version;public string id,venue_id,group_id;public ProductionTree[] instances;
        internal static ProductionForestInstances Read(string json,ProductionForestGroup group,string venue)
        {
            var value=JsonUtility.FromJson<ProductionForestInstances>(json);
            if(value==null||value.schema_version!=1||value.venue_id!=venue||value.group_id!=group.id||value.instances==null||value.instances.Length!=group.count)throw new ArgumentException("Forest instance file does not match its group.");
            ProductionEnvironmentDefinition.Identity(value.id);var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var tree in value.instances)
            {
                if(tree==null||(tree.species!="fir"&&tree.species!="pine"))throw new ArgumentException("Explicit fir/pine species required.");ProductionEnvironmentDefinition.Unique(tree.id,ids);ProductionEnvironmentDefinition.Position(tree.position);
                ProductionEnvironmentDefinition.Bound(tree.scale,.5f,1.5f);ProductionEnvironmentDefinition.Bound(tree.yaw_deg,-360,360);ProductionEnvironmentDefinition.Bound(tree.density_rank,0,1);
                if(tree.density_rank>=1)throw new ArgumentException("Density rank must be below one.");
            }
            return value;
        }
    }
    [Serializable] public sealed class ProductionRangeRecipe
    {
        public string geometry_resource,stadium_art_resource,bank_resource,flags_resource;public Vector3 forward,origin;public float target_distance_m,target_height_m;
        public ProductionTargetBank[] banks;public ProductionBackstop backstop;
        internal void Validate(HashSet<string> ids)
        {
            if(!string.IsNullOrEmpty(flags_resource))ProductionEnvironmentDefinition.Resource(flags_resource);
            ProductionEnvironmentDefinition.Resource(geometry_resource);ProductionEnvironmentDefinition.Resource(stadium_art_resource);ProductionEnvironmentDefinition.Resource(bank_resource);
            ProductionEnvironmentDefinition.Position(origin);ProductionEnvironmentDefinition.Position(forward);
            if(Mathf.Abs(forward.y)>1e-6f||Mathf.Abs(forward.sqrMagnitude-1)>1e-5f)throw new ArgumentException("Horizontal unit range direction required.");
            ProductionEnvironmentDefinition.Bound(target_distance_m,1,1000);ProductionEnvironmentDefinition.Bound(target_height_m,0,10);ProductionEnvironmentDefinition.Collection(banks,1,128);
            foreach(var bank in banks)
            {
                if(bank==null||bank.mat_anchor==null||bank.initial_stance!="Standing"||bank.initial_closed_mask!=0||bank.scale!=1)throw new ArgumentException("Canonical open standing bank required.");
                ProductionEnvironmentDefinition.Unique(bank.id,ids);ProductionEnvironmentDefinition.Identity(bank.lane_id);ProductionEnvironmentDefinition.Identity(bank.mat_anchor.loop_id);
                ProductionEnvironmentDefinition.Bound(bank.mat_anchor.distance_m,0,1000000);ProductionEnvironmentDefinition.Position(bank.mat_position);ProductionEnvironmentDefinition.Position(bank.position);ProductionEnvironmentDefinition.Bound(bank.yaw_deg,-360,360);
            }
            if(backstop!=null){ProductionEnvironmentDefinition.Position(backstop.position);ProductionEnvironmentDefinition.Position(backstop.size);if(backstop.size.x<=0||backstop.size.y<=0||backstop.size.z<=0||!backstop.generate_original_mesh)throw new ArgumentException("Original finite backstop required.");ProductionEnvironmentDefinition.Resource(backstop.material_resource);}
        }
    }
    [Serializable] public sealed class ProductionAnchor {public string loop_id;public float distance_m;}
    [Serializable] public sealed class ProductionTargetBank
    {public string id,lane_id,initial_stance;public ProductionAnchor mat_anchor;public Vector3 mat_position,position;public float yaw_deg,scale;public int initial_closed_mask;}
    [Serializable] public sealed class ProductionBackstop {public Vector3 position,size;public string material_resource;public bool generate_original_mesh;}
    [Serializable] public class ProductionPrefabPlacement
    {
        public string id,resource;public Vector3 position;public float yaw_deg,scale;
        internal virtual void Validate(HashSet<string> ids)
        {ProductionEnvironmentDefinition.Unique(id,ids);ProductionEnvironmentDefinition.Resource(resource);ProductionEnvironmentDefinition.Position(position);ProductionEnvironmentDefinition.Bound(yaw_deg,-360,360);if(scale!=1)throw new ArgumentException("Production prefab placement requires unit scale.");}
    }
    [Serializable] public sealed class ProductionMeshPlacement
    {
        public string id,mesh_resource,material_resource;public Vector3 position;public float yaw_deg,scale;
        // Width-only adjustment preserves portal height, footings and ground-paint elevations.
        // Zero is the legacy JSON omission sentinel and means the existing uniform width.
        public float lateral_scale;
        public float EffectiveLateralScale=>lateral_scale==0?1:lateral_scale;
        internal void Validate(HashSet<string> ids)
        {ProductionEnvironmentDefinition.Unique(id,ids);ProductionEnvironmentDefinition.Resource(mesh_resource);ProductionEnvironmentDefinition.Resource(material_resource);ProductionEnvironmentDefinition.Position(position);ProductionEnvironmentDefinition.Bound(yaw_deg,-360,360);ProductionEnvironmentDefinition.Bound(scale,.01f,100);ProductionEnvironmentDefinition.Bound(EffectiveLateralScale,.01f,100);}
    }
    [Serializable] public sealed class ProductionCrowdRecipe {public string mode,resource_root,seat_binding;public bool restricted_resources_allowed;}
    [Serializable] public sealed class ProductionLightingRecipe
    {
        public string resource,preset_id,weather_resource,weather_preset,sky_material_resource;public Vector2 visual_wind;public float temperature_c;
        public float fog_distance_scale=1;
        internal void Validate()
        {
            ProductionEnvironmentDefinition.Resource(resource);ProductionEnvironmentDefinition.Resource(weather_resource);ProductionEnvironmentDefinition.Resource(sky_material_resource);ProductionEnvironmentDefinition.Identity(preset_id);ProductionEnvironmentDefinition.Identity(weather_preset);
            ProductionEnvironmentDefinition.Bound(visual_wind.x,-40,40);ProductionEnvironmentDefinition.Bound(visual_wind.y,-40,40);if(visual_wind.sqrMagnitude>1600)throw new ArgumentException("Visual wind exceeds weather capacity.");ProductionEnvironmentDefinition.Bound(temperature_c,-100,100);
            ProductionEnvironmentDefinition.Bound(fog_distance_scale,1,3);
        }
    }
}

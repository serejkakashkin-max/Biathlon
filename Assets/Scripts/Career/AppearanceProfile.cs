using System;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    /// <summary>Versioned cosmetic IDs only. Never participates in physical ability or RNG.</summary>
    [DataContract] public sealed class AppearanceProfile
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="legacy_identity",EmitDefaultValue=false)] public string LegacyIdentity {get;private set;}
        [DataMember(Name="face_id",EmitDefaultValue=false)] public string FaceId {get;private set;}
        [DataMember(Name="skin_id",EmitDefaultValue=false)] public string SkinId {get;private set;}
        [DataMember(Name="body_id",EmitDefaultValue=false)] public string BodyId {get;private set;}
        [DataMember(Name="height_mm",EmitDefaultValue=false)] public int HeightMillimeters {get;private set;}
        [DataMember(Name="shoulder_width_bp",EmitDefaultValue=false)] public int ShoulderWidthBasis {get;private set;}
        [DataMember(Name="hair_style_id",EmitDefaultValue=false)] public string HairStyleId {get;private set;}
        [DataMember(Name="hair_color_id",EmitDefaultValue=false)] public string HairColorId {get;private set;}
        [DataMember(Name="eye_color_id",EmitDefaultValue=false)] public string EyeColorId {get;private set;}
        [DataMember(Name="headwear_id",EmitDefaultValue=false)] public string HeadwearId {get;private set;}
        [DataMember(Name="goggles_id",EmitDefaultValue=false)] public string GogglesId {get;private set;}
        [DataMember(Name="lens_id",EmitDefaultValue=false)] public string LensId {get;private set;}
        [DataMember(Name="gloves_id",EmitDefaultValue=false)] public string GlovesId {get;private set;}
        [DataMember(Name="boots_id",EmitDefaultValue=false)] public string BootsId {get;private set;}
        [DataMember(Name="training_accent_id",EmitDefaultValue=false)] public string TrainingAccentId {get;private set;}
        public bool HasDetails=>SchemaVersion==2;
        public bool IsLegacy=>LegacyIdentity!=null;
        private AppearanceProfile(){}
        public static AppearanceProfile Legacy(string stableIdentity)
        {
            CareerUnits.Id(stableIdentity);return new AppearanceProfile{SchemaVersion=1,Id="g2_identity_v1",LegacyIdentity=stableIdentity};
        }
        public static AppearanceProfile Create(string faceId,string skinId,string bodyId)
        {
            var p=new AppearanceProfile{SchemaVersion=1,Id="g2_personal_v1",FaceId=faceId,SkinId=skinId,BodyId=bodyId};p.Validate();return p;
        }
        public static AppearanceProfile CreateDetailed(string faceId,string skinId,string bodyId,int heightMillimeters,int shoulderWidthBasis,
            string hairStyleId,string hairColorId,string eyeColorId,string headwearId,string gogglesId,string lensId,string glovesId,string bootsId,string trainingAccentId)
        {
            var p=new AppearanceProfile{SchemaVersion=2,Id="g2_personal_v2",FaceId=faceId,SkinId=skinId,BodyId=bodyId,HeightMillimeters=heightMillimeters,
                ShoulderWidthBasis=shoulderWidthBasis,HairStyleId=hairStyleId,HairColorId=hairColorId,EyeColorId=eyeColorId,HeadwearId=headwearId,
                GogglesId=gogglesId,LensId=lensId,GlovesId=glovesId,BootsId=bootsId,TrainingAccentId=trainingAccentId};p.Validate();return p;
        }
        public void Validate()
        {
            if(SchemaVersion!=1&&SchemaVersion!=2)throw new SerializationException("Unsupported appearance schema.");CareerUnits.Id(Id);
            bool anyDetails=HeightMillimeters!=0||ShoulderWidthBasis!=0||HairStyleId!=null||HairColorId!=null||EyeColorId!=null||HeadwearId!=null||GogglesId!=null||LensId!=null||GlovesId!=null||BootsId!=null||TrainingAccentId!=null;
            if(SchemaVersion==1&&anyDetails)throw new SerializationException("Appearance v1 cannot contain v2 controls.");
            if(IsLegacy)
            {
                if(SchemaVersion!=1||Id!="g2_identity_v1"||FaceId!=null||SkinId!=null||BodyId!=null)throw new SerializationException("Mixed legacy and explicit appearance.");
                CareerUnits.Id(LegacyIdentity);
            }
            else
            {
                if(Id!=(SchemaVersion==1?"g2_personal_v1":"g2_personal_v2"))throw new SerializationException("Unsupported appearance resource.");
                CareerUnits.Id(FaceId);CareerUnits.Id(SkinId);CareerUnits.Id(BodyId);
                if(SchemaVersion==2)
                {
                    CareerUnits.Range(HeightMillimeters,1000,2500,"cosmetic height");CareerUnits.Range(ShoulderWidthBasis,0,10000,"cosmetic shoulder width");
                    foreach(var value in new[]{HairStyleId,HairColorId,EyeColorId,HeadwearId,GogglesId,LensId,GlovesId,BootsId,TrainingAccentId})CareerUnits.Id(value);
                }
            }
        }
        public string ToJson(){Validate();return CareerJson.Write(this);}
        public static AppearanceProfile FromJson(string json){var p=CareerJson.Read<AppearanceProfile>(json);if(p==null)throw new SerializationException("Missing appearance.");p.Validate();return p;}
    }
    [DataContract] public sealed class CareerCreationRecord
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="mode",IsRequired=true)] public CareerCreationMode Mode {get;private set;}
        [DataMember(Name="policy_id",IsRequired=true)] public string PolicyId {get;private set;}
        [DataMember(Name="start_profile_id",IsRequired=true)] public string StartProfileId {get;private set;}
        [DataMember(Name="specialization_id",IsRequired=true)] public string SpecializationId {get;private set;}
        [DataMember(Name="origin_story_id",IsRequired=true)] public string OriginStoryId {get;private set;}
        [DataMember(Name="initial_stats",IsRequired=true)] private int[] initialStats {get;set;}
        public int InitialStat(CareerStat stat){CareerUnits.Range((int)stat,0,12,"initial stat");return initialStats[(int)stat];}
        internal static CareerCreationRecord FromDraft(CareerCreationDraft d)
        {
            var stats=new int[13];for(int i=0;i<13;i++)stats[i]=d.Stat((CareerStat)i);
            return new CareerCreationRecord{SchemaVersion=1,Id="career_creation_record_v1",Mode=d.Mode,PolicyId=d.PolicyId,StartProfileId=d.StartProfileId,SpecializationId=d.SpecializationId,OriginStoryId=d.OriginStoryId,initialStats=stats};
        }
        internal static CareerCreationRecord FromExisting(CareerAthlete a,string startProfileId,string rosterVersion,string specialization)
        {
            var stats=new int[13];for(int i=0;i<13;i++)stats[i]=a.Stat((CareerStat)i);
            return new CareerCreationRecord{SchemaVersion=1,Id="career_creation_record_v1",Mode=CareerCreationMode.ExistingAthlete,PolicyId=rosterVersion,StartProfileId=startProfileId,SpecializationId=specialization,OriginStoryId="existing_athlete",initialStats=stats};
        }
        internal void Validate()
        {
            if(SchemaVersion!=1||Id!="career_creation_record_v1")throw new SerializationException("Invalid creation provenance.");
            CareerUnits.Range((int)Mode,0,1,"creation mode");CareerUnits.Id(PolicyId);CareerUnits.Id(StartProfileId);CareerUnits.Id(SpecializationId);CareerUnits.Id(OriginStoryId);
            if(initialStats==null||initialStats.Length!=13)throw new SerializationException("Missing initial abilities.");foreach(int stat in initialStats)CareerUnits.Range(stat,0,100000,"initial ability");
        }
    }
}

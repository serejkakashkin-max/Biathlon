using System;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    [DataContract] public sealed class AppearanceOption
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="color",EmitDefaultValue=false)] public string Color {get;private set;}
        internal void Validate(){CareerUnits.Id(Id);if(Color!=null&&(Color.Length!=7||Color[0]!='#'||!int.TryParse(Color.Substring(1),System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture,out _)))throw new SerializationException("Invalid appearance color.");}
    }
    /// <summary>One authored cosmetic policy. It grants no equipment or athletic ability.</summary>
    [DataContract] public sealed class AppearanceDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="base_height_mm",IsRequired=true)] public int BaseHeightMillimeters {get;private set;}
        [DataMember(Name="minimum_height_mm",IsRequired=true)] public int MinimumHeightMillimeters {get;private set;}
        [DataMember(Name="maximum_height_mm",IsRequired=true)] public int MaximumHeightMillimeters {get;private set;}
        [DataMember(Name="default_shoulder_width_bp",IsRequired=true)] public int DefaultShoulderWidthBasis {get;private set;}
        [DataMember(Name="hair_styles",IsRequired=true)] private AppearanceOption[] hairStyles {get;set;}
        [DataMember(Name="hair_colors",IsRequired=true)] private AppearanceOption[] hairColors {get;set;}
        [DataMember(Name="eye_colors",IsRequired=true)] private AppearanceOption[] eyeColors {get;set;}
        [DataMember(Name="headwear",IsRequired=true)] private AppearanceOption[] headwear {get;set;}
        [DataMember(Name="goggles",IsRequired=true)] private AppearanceOption[] goggles {get;set;}
        [DataMember(Name="lenses",IsRequired=true)] private AppearanceOption[] lenses {get;set;}
        [DataMember(Name="gloves",IsRequired=true)] private AppearanceOption[] gloves {get;set;}
        [DataMember(Name="boots",IsRequired=true)] private AppearanceOption[] boots {get;set;}
        [DataMember(Name="training_accents",IsRequired=true)] private AppearanceOption[] trainingAccents {get;set;}
        private AppearanceDefinition(){}
        public int OptionCount(string group)=>Options(group).Length;
        public AppearanceOption Option(string group,int index){var options=Options(group);CareerUnits.Range(index,0,options.Length-1,"appearance option");return options[index];}
        public int Index(string group,string id){var a=Options(group);for(int i=0;i<a.Length;i++)if(a[i].Id==id)return i;throw new ArgumentException("Unknown "+group+" appearance ID: "+id);}
        private AppearanceOption[] Options(string group)
        {
            switch(group){case "hair_style":return hairStyles;case "hair_color":return hairColors;case "eye_color":return eyeColors;case "headwear":return headwear;case "goggles":return goggles;case "lens":return lenses;case "gloves":return gloves;case "boots":return boots;case "training_accent":return trainingAccents;default:throw new ArgumentException("Unknown appearance group.");}
        }
        public AppearanceProfile Edit(AppearanceProfile source,string faceId=null,string skinId=null,string bodyId=null,int? heightMillimeters=null,int? shoulderWidthBasis=null,
            string hairStyleId=null,string hairColorId=null,string eyeColorId=null,string headwearId=null,string gogglesId=null,string lensId=null,string glovesId=null,string bootsId=null,string trainingAccentId=null)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));source.Validate();if(source.IsLegacy)throw new ArgumentException("Resolve a legacy face before editing.");
            bool detailed=source.HasDetails;
            var p=AppearanceProfile.CreateDetailed(faceId??source.FaceId,skinId??source.SkinId,bodyId??source.BodyId,
                heightMillimeters??(detailed?source.HeightMillimeters:BaseHeightMillimeters),shoulderWidthBasis??(detailed?source.ShoulderWidthBasis:DefaultShoulderWidthBasis),
                hairStyleId??(detailed?source.HairStyleId:hairStyles[0].Id),hairColorId??(detailed?source.HairColorId:hairColors[0].Id),eyeColorId??(detailed?source.EyeColorId:eyeColors[0].Id),
                headwearId??(detailed?source.HeadwearId:headwear[0].Id),gogglesId??(detailed?source.GogglesId:goggles[0].Id),lensId??(detailed?source.LensId:lenses[0].Id),
                glovesId??(detailed?source.GlovesId:gloves[0].Id),bootsId??(detailed?source.BootsId:boots[0].Id),trainingAccentId??(detailed?source.TrainingAccentId:trainingAccents[0].Id));
            ValidateProfile(p);return p;
        }
        public void ValidateProfile(AppearanceProfile p)
        {
            p.Validate();if(!p.HasDetails)return;
            CareerUnits.Range(p.HeightMillimeters,MinimumHeightMillimeters,MaximumHeightMillimeters,"authored athlete height");
            Index("hair_style",p.HairStyleId);Index("hair_color",p.HairColorId);Index("eye_color",p.EyeColorId);Index("headwear",p.HeadwearId);Index("goggles",p.GogglesId);Index("lens",p.LensId);Index("gloves",p.GlovesId);Index("boots",p.BootsId);Index("training_accent",p.TrainingAccentId);
        }
        public static AppearanceDefinition FromJson(string json)
        {
            var d=CareerJson.Read<AppearanceDefinition>(json);if(d==null||d.SchemaVersion!=1)throw new SerializationException("Unsupported cosmetic policy.");CareerUnits.Id(d.Id);
            CareerUnits.Range(d.BaseHeightMillimeters,1700,1950,"base height");CareerUnits.Range(d.MinimumHeightMillimeters,1600,d.BaseHeightMillimeters,"minimum height");CareerUnits.Range(d.MaximumHeightMillimeters,d.BaseHeightMillimeters,2050,"maximum height");CareerUnits.Range(d.DefaultShoulderWidthBasis,0,10000,"default shoulders");
            foreach(var group in new[]{"hair_style","hair_color","eye_color","headwear","goggles","lens","gloves","boots","training_accent"})
            {var options=d.Options(group);if(options==null||options.Length<2||options.Length>8)throw new SerializationException("Unbounded appearance options.");for(int i=0;i<options.Length;i++){if(options[i]==null)throw new SerializationException("Missing cosmetic.");options[i].Validate();for(int j=0;j<i;j++)if(options[i].Id==options[j].Id)throw new SerializationException("Duplicate cosmetic ID.");}}
            return d;
        }
    }
}

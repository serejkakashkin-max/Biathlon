using System;
using System.Runtime.Serialization;
using Biathlon.Domain;

namespace Biathlon.Career
{
    /// <summary>Cold, identity-seeded cosmetics. Reads the same authored options as the creator;
    /// never receives, advances or persists the gameplay random stream.</summary>
    public sealed class CareerRivalAppearance
    {
        [DataContract] private sealed class Face { [DataMember(Name="id",IsRequired=true)] public string Id {get;set;} }
        [DataContract] private sealed class Faces
        {
            [DataMember(Name="schema_version",IsRequired=true)] public int Version {get;set;}
            [DataMember(Name="id",IsRequired=true)] public string Id {get;set;}
            [DataMember(Name="profiles",IsRequired=true)] public Face[] Profiles {get;set;}
            [DataMember(Name="skin_ids",IsRequired=true)] public string[] Skins {get;set;}
            [DataMember(Name="body_ids",IsRequired=true)] public string[] Bodies {get;set;}
        }
        private readonly AppearanceDefinition options;
        private readonly Faces faces;
        public CareerRivalAppearance(Func<string,string> read)
        {
            if(read==null)throw new ArgumentNullException(nameof(read));
            options=AppearanceDefinition.FromJson(read("athlete_creator_appearance_v1.json"));
            faces=CareerJson.Read<Faces>(read("athlete_faces_v1.json"));
            if(faces==null||faces.Version!=1||faces.Profiles==null||faces.Profiles.Length==0||faces.Skins==null||faces.Skins.Length==0||faces.Bodies==null||faces.Bodies.Length==0)throw new SerializationException("Missing authored appearance identities.");
            CareerUnits.Id(faces.Id);foreach(var f in faces.Profiles)CareerUnits.Id(f.Id);
            foreach(string id in faces.Skins)CareerUnits.Id(id);foreach(string id in faces.Bodies)CareerUnits.Id(id);
        }
        public AppearanceProfile Generate(string identity)
        {
            CareerUnits.Id(identity);
            int Pick(string part,int count)
            {
                ulong hash=14695981039346656037UL;
                unchecked{foreach(char c in identity+":"+part)hash=(hash^c)*1099511628211UL;}
                return (int)(hash%(ulong)count);
            }
            string Option(string group)=>options.Option(group,Pick(group,options.OptionCount(group))).Id;
            var basic=AppearanceProfile.Create(faces.Profiles[Pick("face",faces.Profiles.Length)].Id,faces.Skins[Pick("skin",faces.Skins.Length)],faces.Bodies[Pick("body",faces.Bodies.Length)]);
            return options.Edit(basic,heightMillimeters:(options.MinimumHeightMillimeters/10+Pick("height",(options.MaximumHeightMillimeters-options.MinimumHeightMillimeters)/10+1))*10,
                shoulderWidthBasis:Pick("shoulders",101)*100,hairStyleId:Option("hair_style"),hairColorId:Option("hair_color"),eyeColorId:Option("eye_color"),
                headwearId:Option("headwear"),gogglesId:Option("goggles"),lensId:Option("lens"),glovesId:Option("gloves"),bootsId:Option("boots"),trainingAccentId:Option("training_accent"));
        }
        /// <summary>Only called while constructing a NEW world/catalog; loading never upgrades old looks.</summary>
        public void InitializeNewWorld(CareerSave save,bool includePlayer=false)
        {
            if(save==null)throw new ArgumentNullException(nameof(save));
            if(save.Season!=1||save.Week!=1||save.RaceCount!=0)throw new InvalidOperationException("Cosmetic initialization requires an unstarted world.");
            for(int i=0;i<save.WorldAthleteCount;i++)
            {var a=save.WorldAthlete(i);if((includePlayer||a.Id!=save.PlayerId)&&a.Appearance.IsLegacy)a.Appearance=Generate(a.Id);}
        }
    }
}

using System;
using System.IO;
using System.Text.Json;
using Biathlon.Domain;
internal static class VenueValidationLab
{
    public static int Execute(string root,string manifestId,string policyId,string output)
    {
        try
        {
            string resources=Path.GetFullPath(root);
            string Resolve(string id,string extension){VenueManifest.ResourceId(id);return Path.Combine(resources,id.Replace('/',Path.DirectorySeparatorChar)+extension);}
            string Read(string id)=>File.ReadAllText(Resolve(id,".json"));
            var manifest=VenueManifest.FromJson(Read(manifestId));var plan=new VenueAuthoringPlan(manifest,Read);
            var report=VenueValidator.Validate(plan,VenueValidationPolicy.FromJson(Read(policyId)),id=>File.Exists(Resolve(id,".prefab")));
            Write(report.ToJson());Console.WriteLine(manifest.Id+": "+(report.Passed?"PASS":"FAIL")+" ("+report.Count+" static diagnostics)");return report.Passed?0:1;
        }
        catch(Exception ex)
        {
            Write(JsonSerializer.Serialize(new{schema_version=1,id="venue_validation_input_failure",static_checks_pass=false,issues=new[]{new{code="input.invalid",path=manifestId,detail=ex.Message}}}));
            Console.Error.WriteLine("Venue validation input failed: "+ex.Message);return 2;
        }
        void Write(string json){string path=Path.GetFullPath(output);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,json);}
    }
}

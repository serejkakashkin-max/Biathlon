using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    public sealed class ShootingTuningEvaluator
    {
        private readonly ShootingBalanceSuite suite;
        private readonly ShootingModelDefinitions model;
        private readonly ShootingAthleteDefinition[] athletes;
        private readonly string baseline;
        public ShootingTuningEvaluator(ShootingBalanceSuite suite,Func<string,string> read)
        {
            this.suite=suite??throw new ArgumentNullException(nameof(suite));
            if(read==null)throw new ArgumentNullException(nameof(read));
            model=new ShootingModelDefinitions(read);baseline=read("shooting_ai_v1.json");
            athletes=new ShootingAthleteDefinition[suite.Count];
            for(int i=0;i<suite.Count;i++)athletes[i]=ShootingAthleteDefinition.FromJson(read((suite.Get(i).AthleteDefinition??"shooting_athlete_v1")+".json"));
        }
        public IReadOnlyDictionary<string,double> Evaluate(IReadOnlyDictionary<string,double> values)
        {
            string json=baseline;
            foreach(var pair in values)
            {
                if(pair.Key!="reaction_seconds"&&pair.Key!="tracking_error_rad")throw new ArgumentException("Coefficient is not allowed.");
                json=Regex.Replace(json,"(\""+pair.Key+"\"\\s*:\\s*)[-+0-9.eE]+","${1}"+pair.Value.ToString("R",CultureInfo.InvariantCulture));
            }
            var profile=ShootingAiDefinition.FromJson(json);var metrics=new Dictionary<string,double>(StringComparer.Ordinal);
            for(int i=0;i<suite.Count;i++)
            {
                int hits=0,shots=0;var scenario=suite.Get(i);
                for(int sample=0;sample<suite.Samples;sample++)
                {var r=ShootingBalanceRunner.Run(suite,scenario,model,athletes[i],profile,suite.SeedStart+(ulong)sample);hits+=r.Hits;shots+=r.Shots;}
                metrics.Add(scenario.Id,hits/(double)shots);
            }
            return metrics;
        }
    }
}

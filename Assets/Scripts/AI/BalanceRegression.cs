using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Biathlon.Domain;

namespace Biathlon.AI
{
    public static class BalanceRegression
    {
        public static string Hash(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
        public static string[] Compare(BalanceBatchDefinition suite,BalanceRegressionSnapshot snapshot,IReadOnlyList<BalanceMetrics> rows)
        {
            if(suite==null||snapshot==null||rows==null)throw new ArgumentNullException();var errors=new List<string>();
            if(Hash(suite.ToJson())!=snapshot.SuiteSha256)errors.Add("suite_sha256: canonical suite/configuration differs");
            if(rows.Count!=suite.RunCount||rows.Count!=snapshot.RecordCount)errors.Add("records.count: expected "+snapshot.RecordCount+", actual "+rows.Count);
            var map=new Dictionary<string,BalanceMetrics>(StringComparer.Ordinal);
            foreach(var row in rows){string id=(string)row.Values["id"];if(map.ContainsKey(id))errors.Add("duplicate row: "+id);else map.Add(id,row);}
            for(int i=0;i<snapshot.RecordCount;i++)
            {var expected=snapshot.GetRecord(i);if(!map.TryGetValue(expected.Id,out var actual))errors.Add(expected.Id+": missing");else{string hash=Hash(actual.ToJson());if(hash!=expected.Sha256)errors.Add(expected.Id+".sha256: expected "+expected.Sha256+", actual "+hash);}}
            for(int i=0;i<snapshot.WindowCount;i++)
            {
                var w=snapshot.GetWindow(i);double sum=0;int count=0;
                foreach(var r in rows)if((string)r.Values["case_id"]==w.CaseId)
                {if(!r.Values.TryGetValue(w.Metric,out var value)||value==null||value is string){errors.Add(w.CaseId+"."+w.Metric+": missing numeric metric");continue;}double n=Convert.ToDouble(value,CultureInfo.InvariantCulture);if(double.IsNaN(n)||double.IsInfinity(n)){errors.Add(w.CaseId+"."+w.Metric+": nonfinite");continue;}sum+=n;count++;}
                double mean=count==0?double.NaN:sum/count;
                if(count==0||mean<w.Minimum||mean>w.Maximum)errors.Add(w.CaseId+"."+w.Metric+".mean: expected ["+w.Minimum.ToString("R",CultureInfo.InvariantCulture)+","+w.Maximum.ToString("R",CultureInfo.InvariantCulture)+"], actual "+mean.ToString("R",CultureInfo.InvariantCulture));
            }
            return errors.ToArray();
        }
    }
}

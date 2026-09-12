using System;
using System.Collections.Generic;
using Biathlon.Domain;

namespace Biathlon.AI
{
    public static class TuningLockVerification
    {
        // Text lock tolerates Git's LF/CRLF checkout policy and UTF-8 BOM only.
        public static string HashText(string text)=>BalanceRegression.Hash(text.TrimStart('\uFEFF').Replace("\r\n","\n"));
        public static string[] Verify(BalanceTuningLock definition,Func<string,string> read)
        {
            if(definition==null||read==null)throw new ArgumentNullException();var failures=new List<string>();
            for(int i=0;i<definition.Count;i++)
            {
                var entry=definition.Get(i);
                try{if(HashText(read(entry.Path))!=entry.Sha256)failures.Add("Changed: "+entry.Path);}
                catch(System.IO.IOException){failures.Add("Missing/unreadable: "+entry.Path);}
            }
            return failures.ToArray();
        }
    }
}

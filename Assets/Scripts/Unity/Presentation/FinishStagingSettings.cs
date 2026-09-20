using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class FinishStagingSettings
    {
        public int schema_version,columns;public string id;public float forward_m,lateral_spacing_m,row_spacing_m;
        public static FinishStagingSettings Load()
        {
            var d=JsonUtility.FromJson<FinishStagingSettings>(Resources.Load<TextAsset>("Biathlon/finish_staging_v1").text);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.columns<1||d.columns>10)throw new ArgumentException("Invalid finish staging schema.");
            foreach(float v in new[]{d.forward_m,d.lateral_spacing_m,d.row_spacing_m})if(!float.IsFinite(v)||v<1||v>20)throw new ArgumentException("Invalid finish staging spacing.");return d;
        }
        public Vector3 Offset(int rosterIndex)=>new Vector3((rosterIndex%columns-(columns-1)*.5f)*lateral_spacing_m,0,forward_m+(rosterIndex/columns)*row_spacing_m);
    }
}

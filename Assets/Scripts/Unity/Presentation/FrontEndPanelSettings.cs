using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
namespace Biathlon.Unity.Presentation
{
    public static class FrontEndPanelSettings
    {
        public const string Resource="Biathlon/UI/front_end_panel_v1";
        public static PanelSettings Create()
        {
            var template=Resources.Load<PanelSettings>(Resource);
            if(template==null)throw new FileNotFoundException("Missing UI panel template: "+Resource);
            return Object.Instantiate(template);
        }
    }
}

using System;
using Biathlon.Domain;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Biathlon.Unity.Presentation
{
    public readonly struct ShootingDeviceCommand
    {
        public WeaponAimCommand Aim { get; }
        public bool Trigger { get; }
        public bool HoldBreath { get; }
        public ShootingDeviceCommand(WeaponAimCommand aim,bool trigger,bool holdBreath){Aim=aim;Trigger=trigger;HoldBreath=holdBreath;}
    }
    [Serializable] public sealed class ShootingDeviceSettings
    {
        public int schema_version;
        public string id;
        public double mouse_radians_per_pixel,stick_deadzone,stick_exponent;
        public static ShootingDeviceSettings FromJson(string json)
        {
            var s=JsonUtility.FromJson<ShootingDeviceSettings>(json);
            if(s==null||s.schema_version!=1||string.IsNullOrWhiteSpace(s.id))throw new ArgumentException("Invalid shooting device schema.");
            if(!Finite(s.mouse_radians_per_pixel)||s.mouse_radians_per_pixel<=0||s.mouse_radians_per_pixel>0.01||
               !Finite(s.stick_deadzone)||s.stick_deadzone<0||s.stick_deadzone>=1||
               !Finite(s.stick_exponent)||s.stick_exponent<=0||s.stick_exponent>10)throw new ArgumentException("Invalid shooting device tuning.");
            return s;
        }
        private static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
    }
    /// <summary>Frame samples become bounded fixed-tick intent; no hit logic or simulation stepping.</summary>
    public sealed class ShootingDeviceInput : MonoBehaviour
    {
        public bool UseGamepad { get; set; }
        private ShootingDeviceSettings settings;
        private AthleteId athlete;
        private long tick;
        private double mouseUnitsPerPixel,pendingX,pendingY,stickX,stickY;
        private bool pad,focused=true,previousTrigger,trigger,breath,fine;
        public void Initialize(AthleteId id,int frequency,SimulationTick initialTick=default)
        {
            if(!id.IsValid||frequency<=0)throw new ArgumentException("Invalid input clock/athlete.");
            settings=ShootingDeviceSettings.FromJson(Resources.Load<TextAsset>("Biathlon/shooting_device_v1").text);
            var aim=WeaponAimDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/weapon_aim_v1").text);
            mouseUnitsPerPixel=settings.mouse_radians_per_pixel*frequency*1000/aim.RateRadiansPerSecond;
            athlete=id;tick=initialTick.Value;ReleaseHeldInput();
        }
        private void Update(){if(settings!=null)Capture(UseGamepad?null:Mouse.current,UseGamepad?null:Keyboard.current,UseGamepad?Gamepad.current:null);}
        // Call once per Input System frame: mouse.delta is already accumulated by Input System.
        public void Capture(Mouse mouse,Keyboard keyboard,Gamepad gamepad)
        {
            if(settings==null)throw new InvalidOperationException("Initialize input first.");
            if(!focused||(mouse==null&&gamepad==null)){ReleaseHeldInput();return;}
            bool nextPad=gamepad!=null;if(nextPad!=pad)ReleaseHeldInput();pad=nextPad;
            bool down;
            if(pad)
            {
                var stick=gamepad.rightStick.ReadUnprocessedValue();double length=Math.Sqrt(stick.x*stick.x+stick.y*stick.y);
                double strength=length<=settings.stick_deadzone?0:Math.Pow((Math.Min(1,length)-settings.stick_deadzone)/(1-settings.stick_deadzone),settings.stick_exponent);
                stickX=length>0?stick.x/length*strength:0;stickY=length>0?stick.y/length*strength:0;
                down=gamepad.rightTrigger.isPressed;breath=gamepad.leftShoulder.isPressed;fine=gamepad.leftTrigger.isPressed;
            }
            else
            {
                var delta=mouse.delta.ReadValue();pendingX+=delta.x*mouseUnitsPerPixel;pendingY+=delta.y*mouseUnitsPerPixel;
                down=mouse.leftButton.isPressed;breath=keyboard?.spaceKey.isPressed==true;fine=mouse.rightButton.isPressed;
            }
            trigger|=down&&!previousTrigger;previousTrigger=down;
        }
        public ShootingDeviceCommand Build(SimulationTick target)
        {
            if(settings==null)throw new InvalidOperationException("Initialize input first.");
            if(target.Value!=checked(tick+1))throw new ArgumentException("Consecutive target tick required.");
            double x=pad?stickX*1000:pendingX,y=pad?stickY*1000:pendingY;
            double length=Math.Sqrt(x*x+y*y);if(length>1000){x*=1000/length;y*=1000/length;}
            int horizontal=(int)Math.Round(x,MidpointRounding.ToEven),vertical=(int)Math.Round(y,MidpointRounding.ToEven);
            var result=new ShootingDeviceCommand(new WeaponAimCommand(target,athlete,horizontal,vertical,fine),trigger,breath);
            if(!pad){pendingX-=horizontal;pendingY-=vertical;}tick=target.Value;trigger=false;return result;
        }
        public void SetInputFocus(bool value){focused=value;if(!value)ReleaseHeldInput();}
        public void ReleaseHeldInput(){pendingX=pendingY=stickX=stickY=0;trigger=previousTrigger=breath=fine=false;}
        private void OnApplicationFocus(bool value)=>SetInputFocus(value);
        private void OnDisable()=>ReleaseHeldInput();
    }
}

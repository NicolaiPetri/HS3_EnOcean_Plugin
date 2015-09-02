using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Scheduler;
using HSCF;
using HomeSeerAPI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;


namespace EnOcean
{
    public enum EDeviceTypes { UNKNOWN=0, PUSHBUTTON_4x=1 }
    public interface IEnOceanDevice
    {
        String DeviceId { get; }
        bool ProcessPacket(EnOceanPacket packet);
        void SaveConfiguration();
    }
    abstract class EnOceanDeviceBase : IEnOceanDevice {
        EDeviceTypes deviceType;
        protected Scheduler.Classes.DeviceClass hsDevice;
        JObject deviceConfig;
        protected IHSApplication HS;
        EnOceanController Controller;
        public String DeviceId { get; set; }

        public abstract void AddOrUpdateHSDeviceProperties();
        public EnOceanDeviceBase(IHSApplication Hs, EnOceanController Ctrl, String deviceId, JObject config )
        {
            HS = Hs;
            DeviceId = deviceId;
            //            hsDevice = Hs;
            deviceConfig = config;
            Controller = Ctrl;
            deviceType = (EDeviceTypes)(int)config["device_type"];
 
            GetHSDevice(); // Fetch or allocate new hs device
        }
        public abstract bool ProcessPacket(EnOceanPacket packet);

        protected void GetHSDevice()
        {
            hsDevice =  Controller.getHSDevice(EnOceanController.EnOceanDeviceType.SimpleDevice, DeviceId);
            if (hsDevice == null)
            {
                deviceConfig["name"] = DeviceId;
                hsDevice = Controller.createHSDevice((string)deviceConfig["name"], EnOceanController.EnOceanDeviceType.SimpleDevice, DeviceId);
                hsDevice.set_Device_Type_String(HS, "EnOcean " + EnOceanController.EnOceanDeviceType.SimpleDevice.ToString());

                AddOrUpdateHSDeviceProperties();
                Controller.SaveConfiguration();
            }
        }
        public void SaveConfiguration()
        {
            var extraData = hsDevice.get_PlugExtraData_Get(HS);
            //extraData.AddNamed("EnOcean Type", "Controller");
            //hsDev.set_PlugExtraData_Set(HS, extraData);
            //var dataStr = (string)extraData.GetNamed("EnOcean Cfg");
            //if (dataStr == null) {
            //  Console.WriteLine("No json data on device - adding");
            extraData.AddNamed("EnOcean Cfg", deviceConfig.ToString());
            hsDevice.set_PlugExtraData_Set(HS, extraData);
//            hsDev
            //FIXME: Save config to homeseer
            HS.SaveEventsDevices();

        }
    }
    class EnOceanButtonDevice : EnOceanDeviceBase
    {
        public EnOceanButtonDevice(IHSApplication HS, EnOceanController Ctrl, String deviceId, JObject config) :
            base(HS, Ctrl, deviceId, config)
        {
//            EnOceanDeviceBase(HS,  )
            
            Console.WriteLine("Init of EnOceanButtonDevice : {0}", deviceId);

        }
        public override bool ProcessPacket(EnOceanPacket packet)
        {
            Console.WriteLine("Specific handler for packet!! FIXME:");
            return true;
        }
        public override void AddOrUpdateHSDeviceProperties()
        {
            Console.WriteLine("FIXME: Adding HS Device control status");
            /*                VSVGPairs.VSPair v = new VSVGPairs.VSPair(ePairStatusControl.Both);
                v.PairType = VSVGPairs.VSVGPairType.SingleValue;
                v.Value = 0;
                v.Status = "Off";
                v.Render = Enums.CAPIControlType.Button;
                v.Render_Location.Row = 1;
                v.Render_Location.Column = 1;
                v.ControlUse = ePairControlUse._Off;
                HS.DeviceVSP_AddPair(hsRootDevRefId, v);

                VSVGPairs.VSPair v2 = new VSVGPairs.VSPair(ePairStatusControl.Both);
                v2.PairType = VSVGPairs.VSVGPairType.SingleValue;
                v2.Value = 1;
                v2.Status = "On";
                v2.Render = Enums.CAPIControlType.Button;
                v2.Render_Location.Row = 1;
                v2.Render_Location.Column = 2;
                v2.ControlUse = ePairControlUse._On;
                HS.DeviceVSP_AddPair(hsRootDevRefId, v2);
 */
                /*         v = new VSVGPairs.VSPair(ePairStatusControl.Status);
                         v.PairType = VSVGPairs.VSVGPairType.SingleValue;
                         v.Value = 2;
                         v.Status = "Offline";
                         v.Render = Enums.CAPIControlType.Values;
                         HS.DeviceVSP_AddPair(hsRootDevRefId, v);
                         v = new VSVGPairs.VSPair(ePairStatusControl.Status);
                         v.PairType = VSVGPairs.VSVGPairType.SingleValue;
                         v.Value = 3;
                         v.Status = "Unknown";
                         v.Render = Enums.CAPIControlType.Values;
                         newDev.DeviceVSP_AddPair(hsRootDevRefId, v);*/
                hsDevice.MISC_Set(HS, Enums.dvMISC.NO_LOG);
                hsDevice.MISC_Set(HS, Enums.dvMISC.SHOW_VALUES);
//                hsDevice.set_DeviceType_Set(HS, DT);
//                newDev.set_Address(HS, "NA");
               // HS.SaveEventsDevices();
             //   return newDev;
          //  }

        }
    }
    public class DeviceTypes
    {
        static public IEnOceanDevice CreateDeviceInstance(IHSApplication HS, EnOceanController Controller, String deviceId, String deviceType, JObject config)
        {
            EDeviceTypes DeviceType;
            if (Enum.TryParse<EDeviceTypes>(deviceType, out DeviceType))
            {
                config["device_type"] = (int)DeviceType;
                switch (DeviceType)
                {
                    case EDeviceTypes.PUSHBUTTON_4x:
                        {
                        Console.WriteLine("BUTTON THING");
                        var newDev =  new EnOceanButtonDevice(HS, Controller, deviceId, config);
                        Controller.RegisterDevice(newDev);
                        }
                        break;

                }

            }
            else
            {
                Console.WriteLine("Error getting type: {0}", deviceType);
            }
            return null;
        }
        static public bool ProcessCommand(EnOceanPacket packet, Scheduler.Classes.DeviceClass hsDev)
        {
            Console.WriteLine("Processing command : {0}", packet.getType().ToString());
            return true;
        }
    }
}

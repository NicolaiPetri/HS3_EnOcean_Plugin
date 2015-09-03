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
    public enum EDeviceTypes { UNKNOWN = 0, PUSHBUTTON_4x = 1 }
    public interface IEnOceanDevice
    {
        String DeviceId { get; }
        bool ProcessPacket(EnOceanPacket packet);
        void SaveConfiguration();
    }
    abstract class EnOceanDeviceBase : IEnOceanDevice
    {
        EDeviceTypes deviceType;
        protected Scheduler.Classes.DeviceClass hsDevice;
        JObject deviceConfig;
        protected IHSApplication HS;
        protected EnOceanController Controller;
        public String DeviceId { get; set; }

        public abstract void AddOrUpdateHSDeviceProperties();
        public EnOceanDeviceBase(IHSApplication Hs, EnOceanController Ctrl, String deviceId, JObject config)
        {
            HS = Hs;
            DeviceId = deviceId;
            deviceConfig = config;
            Controller = Ctrl;
            deviceType = (EDeviceTypes)(int)config["device_type"];

            GetHSDevice(); // Fetch or allocate new hs device
        }
        public abstract bool ProcessPacket(EnOceanPacket packet);

        protected void GetHSDevice()
        {
            hsDevice = Controller.getHSDevice(EnOceanController.EnOceanDeviceType.SimpleDevice, DeviceId);
            if (hsDevice == null)
            {
                if (deviceConfig["node_name"] == null)
                    deviceConfig["node_name"] = DeviceId;
                hsDevice = Controller.createHSDevice((string)deviceConfig["node_name"], EnOceanController.EnOceanDeviceType.SimpleDevice, DeviceId);
                hsDevice.set_Device_Type_String(HS, "EnOcean " + EnOceanController.EnOceanDeviceType.SimpleDevice.ToString());
                hsDevice.set_Relationship(HS, Enums.eRelationship.Parent_Root);
                hsDevice.MISC_Set(HS, Enums.dvMISC.NO_LOG);
                hsDevice.MISC_Set(HS, Enums.dvMISC.SHOW_VALUES);
                AddOrUpdateHSDeviceProperties();
                SaveConfiguration();
            }
        }
        public void SaveConfiguration()
        {
            var extraData = hsDevice.get_PlugExtraData_Get(HS);
            if (extraData == null)
                extraData = new PlugExtraData.clsPlugExtraData();
            extraData.AddNamed("EnOcean Cfg", deviceConfig.ToString());
            hsDevice.set_PlugExtraData_Set(HS, extraData);
            Controller.SaveConfiguration();
        }
    }
    class EnOceanButtonDevice : EnOceanDeviceBase
    {
        public EnOceanButtonDevice(IHSApplication HS, EnOceanController Ctrl, String deviceId, JObject config) :
            base(HS, Ctrl, deviceId, config)
        {
            Console.WriteLine("Init of EnOceanButtonDevice : {0}", deviceId);
        }
        public bool SetButtonState(int button, Boolean pressed)
        {
            String btnDeviceId = DeviceId + ":" + button;
            Console.WriteLine("Button {0} pressed: {1}", button, pressed);
            var btnDevice = Controller.getHSDevice(EnOceanController.EnOceanDeviceType.ChildDevice, btnDeviceId);
            if (btnDevice == null)
            {

                Console.WriteLine("No button device for btn {0} - creating");
                btnDevice = Controller.createHSDevice(hsDevice.get_Name(null) + " Button " + button, EnOceanController.EnOceanDeviceType.ChildDevice, btnDeviceId);
                btnDevice.set_Device_Type_String(HS, "EnOcean " + EnOceanController.EnOceanDeviceType.ChildDevice.ToString());
                btnDevice.set_Relationship(HS, Enums.eRelationship.Child);
                btnDevice.AssociatedDevice_Add(HS, hsDevice.get_Ref(null));

                VSVGPairs.VSPair v = new VSVGPairs.VSPair(ePairStatusControl.Status);
                v.PairType = VSVGPairs.VSVGPairType.SingleValue;
                v.Value = 0;
                v.Status = "Released";
                v.Render = Enums.CAPIControlType.Button;
                v.Render_Location.Row = 1;
                v.Render_Location.Column = 1;
                v.ControlUse = ePairControlUse._Off;
                HS.DeviceVSP_AddPair(btnDevice.get_Ref(null), v);

                VSVGPairs.VSPair v2 = new VSVGPairs.VSPair(ePairStatusControl.Status);
                v2.PairType = VSVGPairs.VSVGPairType.SingleValue;
                v2.Value = 1;
                v2.Status = "Pressed";
                v2.Render = Enums.CAPIControlType.Button;
                v2.Render_Location.Row = 1;
                v2.Render_Location.Column = 2;
                v2.ControlUse = ePairControlUse._On;
                HS.DeviceVSP_AddPair(btnDevice.get_Ref(null), v2);
                btnDevice.MISC_Set(HS, Enums.dvMISC.NO_LOG);
                btnDevice.MISC_Set(HS, Enums.dvMISC.SHOW_VALUES);

                Controller.SaveConfiguration();
            }
            HS.SetDeviceValueByRef(btnDevice.get_Ref(null), pressed ? 1 : 0, true);
            return true;
        }
        public override bool ProcessPacket(EnOceanPacket packet)
        {
            Console.WriteLine("Specific handler for packet!! FIXME: dta5={0}", packet.GetData()[5]);
            int button = 0xff;
            byte cmd = packet.GetData()[1];
            // FIXME: 0x10 is spring up/down and rest is button down map
            // Adjust button detection to handle correctly.
            // This will allow us to register that 2 buttons are down at the same time
            // Above is probably incorrect still .. check docs!!!
            switch (cmd)
            {
                case 0x10:
                    button = 2;
                    break;
                case 0x30:
                    button = 1;
                    break;
                case 0x50:
                    button = 4;
                    break;
                case 0x70:
                    button = 3;
                    break;
                //                
                default:
                    Console.WriteLine("Unknown button: {0}", cmd);
                    button = 0; // Released
                    for (int bc = 1; bc < 5; bc++)
                    {
                        SetButtonState(bc, false);
                    }
                    return true;
                case 0x0:
                    button = 0; // Released
                    for (int bc = 1; bc < 5; bc++)
                    {
                        SetButtonState(bc, false);
                    }
                    return true;
            }
            SetButtonState(button, true);
            return true;
        }
        public override void AddOrUpdateHSDeviceProperties()
        {
            Console.WriteLine("FIXME: Adding HS Device control status");
            hsDevice.MISC_Set(HS, Enums.dvMISC.NO_LOG);
            hsDevice.MISC_Set(HS, Enums.dvMISC.SHOW_VALUES);

            SaveConfiguration();
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
                            var newDev = new EnOceanButtonDevice(HS, Controller, deviceId, config);
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
        public bool ProcessCommand(EnOceanPacket packet, Scheduler.Classes.DeviceClass hsDev)
        {
            Console.WriteLine("Processing command : {0}", packet.getType().ToString());
            return true;
        }
    }
}

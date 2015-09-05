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
    public enum EDeviceTypes { UNKNOWN = 0, PUSHBUTTON_4x, DOORCONTACT, TEMPERATURE_SENSOR }
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
//                hsDevice.MISC_Set(HS, Enums.dvMISC.NO_LOG);
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
        Dictionary<int, Boolean> buttonState = new Dictionary<int, bool>();

        public bool SetButtonState(int button, Boolean pressed)
        {
            String btnDeviceId = DeviceId + ":" + button;
        //    Console.WriteLine("Button {0} pressed: {1}", button, pressed);
            var btnDevice = Controller.getHSDevice(EnOceanController.EnOceanDeviceType.ChildDevice, btnDeviceId);
            buttonState[button] = pressed;
            if (btnDevice == null)
            {
                Console.WriteLine("No button device for btn {0} - creating");
                btnDevice = Controller.createHSDevice(hsDevice.get_Name(null) + " Button " + button, EnOceanController.EnOceanDeviceType.ChildDevice, btnDeviceId);
                btnDevice.set_Device_Type_String(HS, "EnOcean " + EnOceanController.EnOceanDeviceType.ChildDevice.ToString());
                btnDevice.set_Relationship(HS, Enums.eRelationship.Child);
                btnDevice.AssociatedDevice_Add(HS, hsDevice.get_Ref(null));
                hsDevice.AssociatedDevice_Add(HS, btnDevice.get_Ref(null));

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

//                btnDevice.MISC_Set(HS, Enums.dvMISC.NO_LOG);
                btnDevice.MISC_Set(HS, Enums.dvMISC.SHOW_VALUES);

                Controller.SaveConfiguration();
            }
            HS.SetDeviceValueByRef(btnDevice.get_Ref(null), pressed ? 1 : 0, true);
            return true;
        }
        public override bool ProcessPacket(EnOceanPacket packet)
        {
            Console.WriteLine("Specific handler for packet, dta1={0}", packet.GetData()[1]);
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
                    Console.WriteLine("Unknown button, releasing: {0}", cmd);
                    button = 0; // Released
                    for (int bc = 1; bc < 5; bc++)
                    {
                        if (buttonState.ContainsKey(bc) && buttonState[bc])
                            SetButtonState(bc, false);
                    }
                    return true;
                case 0x0:
                    Console.WriteLine("Buttons released!");
                    button = 0; // Released
                    for (int bc = 1; bc < 5; bc++)
                    {
                        if (buttonState.ContainsKey(bc) && buttonState[bc])
                            SetButtonState(bc, false);
                    }
                    return true;
            }
            Console.WriteLine("Button {0} pressed", button);
            SetButtonState(button, true);
            return true;
        }
        public override void AddOrUpdateHSDeviceProperties()
        {
            Console.WriteLine("FIXME: Adding HS Device control status");
//            hsDevice.MISC_Set(HS, Enums.dvMISC.NO_LOG);
            hsDevice.MISC_Set(HS, Enums.dvMISC.SHOW_VALUES);

            SaveConfiguration();
        }
    }
    class EnOceanDoorContactDevice : EnOceanDeviceBase
    {
        public EnOceanDoorContactDevice(IHSApplication HS, EnOceanController Ctrl, String deviceId, JObject config) :
            base(HS, Ctrl, deviceId, config)
        {
            Console.WriteLine("Init of EnOceanDoorContactDevice : {0}", deviceId);
        }
        Dictionary<int, Boolean> buttonState = new Dictionary<int, bool>();

        public override bool ProcessPacket(EnOceanPacket packet)
        {
            Console.WriteLine("Specific handler for packet, dta1={0}", packet.GetData()[1]);
            // Check correct type
//            if (packet.getType() == PacketType.)
            byte cmd = packet.GetData()[1];
            switch (cmd)
            {
                case 0x08:
                    HS.SetDeviceValueByRef(hsDevice.get_Ref(null), 0x1, true);
                    break;
                case 0x09:
                    HS.SetDeviceValueByRef(hsDevice.get_Ref(null), 0x0, true);
                    break;
                //                
                default:
                    Console.WriteLine("Unknown state, fixme: {0}", cmd);
                    return true;
            }
            return true;
        }
        public override void AddOrUpdateHSDeviceProperties()
        {
            Console.WriteLine("FIXME: Adding HS Device control status");
//            hsDevice.MISC_Set(HS, Enums.dvMISC.NO_LOG);
            hsDevice.MISC_Set(HS, Enums.dvMISC.SHOW_VALUES);

            hsDevice.set_Relationship(HS, Enums.eRelationship.Standalone);

            // Clear existing VSP
            HS.DeviceVSP_ClearAll(hsDevice.get_Ref(null), true);

            VSVGPairs.VSPair v = new VSVGPairs.VSPair(ePairStatusControl.Status);
            v.PairType = VSVGPairs.VSVGPairType.SingleValue;
            v.Value = 0;
            v.Status = "Closed";
            v.Render = Enums.CAPIControlType.Button;
            v.Render_Location.Row = 1;
            v.Render_Location.Column = 1;
            v.ControlUse = ePairControlUse._Off;
            HS.DeviceVSP_AddPair(hsDevice.get_Ref(null), v);

            VSVGPairs.VSPair v2 = new VSVGPairs.VSPair(ePairStatusControl.Status);
            v2.PairType = VSVGPairs.VSVGPairType.SingleValue;
            v2.Value = 1;
            v2.Status = "Open";
            v2.Render = Enums.CAPIControlType.Button;
            v2.Render_Location.Row = 1;
            v2.Render_Location.Column = 2;
            v2.ControlUse = ePairControlUse._On;
            HS.DeviceVSP_AddPair(hsDevice.get_Ref(null), v2);

            SaveConfiguration();
        }
    }
    class EnOceanTempSensorDevice : EnOceanDeviceBase
    {
        public EnOceanTempSensorDevice(IHSApplication HS, EnOceanController Ctrl, String deviceId, JObject config) :
            base(HS, Ctrl, deviceId, config)
        {
            Console.WriteLine("Init of EnOceanTempSensorDevice : {0}", deviceId);
        }
        public override bool ProcessPacket(EnOceanPacket packet)
        {
            Double dta = packet.GetData()[3];
            Console.WriteLine("Specific handler for packet, dta1={0}", dta);

            Double tempBase = 0;
            Double tempRange = 40;
            Double tempResolution = 256;

            Double temperature = tempRange - (tempBase + ( (tempRange / tempResolution) * dta)); 
            HS.SetDeviceValueByRef(hsDevice.get_Ref(null), temperature, true);

            return true;
        }


        public override void AddOrUpdateHSDeviceProperties()
        {
            Console.WriteLine("FIXME: Adding HS Device control status");
//            hsDevice.MISC_Set(HS, Enums.dvMISC.NO_LOG);
            hsDevice.MISC_Set(HS, Enums.dvMISC.SHOW_VALUES);

            hsDevice.set_Relationship(HS, Enums.eRelationship.Standalone);

            // Clear existing VSP
            HS.DeviceVSP_ClearAll(hsDevice.get_Ref(null), true);

            VSVGPairs.VSPair v = new VSVGPairs.VSPair(ePairStatusControl.Status);
            v.PairType = VSVGPairs.VSVGPairType.Range;
            var vg = new HomeSeerAPI.VSVGPairs.VGPair();
            vg.PairType = VSVGPairs.VSVGPairType.Range;
            //v.ControlStatus = ePairStatusControl.Status;
            //v.ControlUse = ePairControlUse.
            v.RangeStatusSuffix = " C";


            v.RangeStart = -50;
            v.RangeEnd = 100;
            vg.Graphic = "/images/HomeSeer/status/Thermometer-50.png";
            v.RangeStatusDecimals = 2;
 
            v.Render = Enums.CAPIControlType.ValuesRange;

            hsDevice.MISC_Clear(HS, Enums.dvMISC.SHOW_VALUES); // Should be set or not ?
            HS.DeviceVSP_AddPair(hsDevice.get_Ref(null), v);
            HS.DeviceVGP_AddPair(hsDevice.get_Ref(null), vg);

/*
            VSVGPairs.VSPair v = new VSVGPairs.VSPair(ePairStatusControl.Status);
            v.PairType = VSVGPairs.VSVGPairType.SingleValue;
            v.Value = 0;
            v.Status = "Closed";
            v.Render = Enums.CAPIControlType.Button;
            v.Render_Location.Row = 1;
            v.Render_Location.Column = 1;
            v.ControlUse = ePairControlUse._Off;
            HS.DeviceVSP_AddPair(hsDevice.get_Ref(null), v);

            VSVGPairs.VSPair v2 = new VSVGPairs.VSPair(ePairStatusControl.Status);
            v2.PairType = VSVGPairs.VSVGPairType.SingleValue;
            v2.Value = 1;
            v2.Status = "Open";
            v2.Render = Enums.CAPIControlType.Button;
            v2.Render_Location.Row = 1;
            v2.Render_Location.Column = 2;
            v2.ControlUse = ePairControlUse._On;
            HS.DeviceVSP_AddPair(hsDevice.get_Ref(null), v2);
*/
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
                    case EDeviceTypes.DOORCONTACT:
                        {
                            Console.WriteLine("DOOR THING");
                            var newDev = new EnOceanDoorContactDevice(HS, Controller, deviceId, config);
                            Controller.RegisterDevice(newDev);
                        }
                        break;
                    case EDeviceTypes.TEMPERATURE_SENSOR:
                        {
                            Console.WriteLine("TEMPERATURE THING");
                            var newDev = new EnOceanTempSensorDevice(HS, Controller, deviceId, config);
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

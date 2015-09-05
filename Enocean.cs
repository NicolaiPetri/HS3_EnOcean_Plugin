using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Scheduler;
using HSCF;
using HomeSeerAPI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using HSPI_EnOcean;

namespace EnOcean
{
    class EnOceanManager
    {
        IHSApplication HS;
        IAppCallbackAPI HSCB;
        HSPI hspiInst;
        Dictionary<String, EnOceanController> interfaceList = new Dictionary<String, EnOceanController>();
        public void Stop()
        {
            Console.WriteLine("Stopping all interfaces");
            // Shutdown thing
            foreach (var iface in this.GetInterfaces())
            {
                iface.Close();
            }
            Console.WriteLine(" - DONE");

        }
        public EnOceanManager(IHSApplication pHsHost, IAppCallbackAPI pHsCB, HSPI pHspiInst)
        {
            hspiInst = pHspiInst;
            HS = pHsHost;
            HSCB = pHsCB;
        }
        public void AddInterface(EnOceanController newInterface)
        {
            interfaceList.Add(newInterface.ControllerId, newInterface);
        }
        public EnOceanController GetInterfaceById(String interfaceId)
        {
            return interfaceList[interfaceId];
        }
        public IEnumerable<EnOceanController> GetInterfaces()
        {
            return interfaceList.Values;
        }
        public void Initialize()
        {
            Dictionary<int, Scheduler.Classes.DeviceClass> device_map = new Dictionary<int, Scheduler.Classes.DeviceClass>();
            Dictionary<int, bool> processed = new Dictionary<int, bool>();

            Scheduler.Classes.clsDeviceEnumeration devenum = HS.GetDeviceEnumerator() as Scheduler.Classes.clsDeviceEnumeration;

            while (!devenum.Finished)
            {
                Scheduler.Classes.DeviceClass dev = devenum.GetNext();
                if (dev.get_Interface(null) != Constants.PLUGIN_STRING_NAME)
                    continue; // Not ours!
                var extraData = dev.get_PlugExtraData_Get(HS);
                if (extraData == null)
                    extraData = new PlugExtraData.clsPlugExtraData();
                var typeStr = (string)extraData.GetNamed("EnOcean Type");
                if (typeStr == null)
                    continue; // No type - continue
                if (typeStr != "Controller")
                    continue; // Not a controller
                var dataStr = (string)extraData.GetNamed("EnOcean Cfg");
                if (dataStr == null)
                {
                    Console.WriteLine("No json data on device - skipping");
                    //                   extraData.AddNamed("EnOcean Cfg", config.ToString());
                    //                 rootDev.set_PlugExtraData_Set(HS, extraData);
                    continue; // Skip interface
                }
                else
                {
                    var config = JObject.Parse(dataStr);
                    Console.WriteLine("Loaded config: {0}", config.ToString());
                    var ctrlInstance = new EnOceanController(HS, HSCB, hspiInst, config);
                    ctrlInstance.Initialize();
                    AddInterface(ctrlInstance);
                }
            }

        }

    }
    public class EnOceanController : IDisposable
    {
        IHSApplication HS;
        IAppCallbackAPI HSCB;
        String portName;
        String internalStatus;
        JObject config;
        HSPI hspiInst;
        public EnOceanController(IHSApplication pHsHost, IAppCallbackAPI pHsCB, HSPI pHspiInst, JObject initCfg)
        {
            hspiInst = pHspiInst;
            HS = pHsHost;
            HSCB = pHsCB;
            config = initCfg;
            portName = (string)config["portname"];
            UniqueControllerId = (string)config["unique_id"];
        }
        public void QueueProcesserThread()
        {
            DateTime nextRun = DateTime.UtcNow;
            while (hspiInst.Running)
            {
                while (DateTime.UtcNow < nextRun)
                    Thread.Sleep(50); // Sleep 
            }
        }
        protected void ReloadConfig()
        {
        }
        int hsRootDevRefId = 0;
        public enum EnOceanDeviceType { Unknown = 0, Controller, SimpleDevice, ChildDevice };

        public Scheduler.Classes.DeviceClass createHSDevice(String Name, EnOceanDeviceType type, String id = "")
        {
            var devRefId = HS.NewDeviceRef(Name);
            var newDev = (Scheduler.Classes.DeviceClass)HS.GetDeviceByRef(devRefId);

            var DT = new DeviceTypeInfo_m.DeviceTypeInfo();
            DT.Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Plug_In;
            DT.Device_Type = 33;
            DT.Device_SubType = (int)type;
            newDev.set_DeviceType_Set(HS, DT);
            newDev.set_Address(HS, id);
            newDev.set_Interface(HS, Constants.PLUGIN_STRING_NAME);
            newDev.set_InterfaceInstance(HS, "");
            newDev.set_Last_Change(HS, DateTime.Now);
            newDev.set_Location(HS, "EnOcean");
            newDev.set_Location2(HS, "EnOcean");
            return newDev;
        }
        public Scheduler.Classes.DeviceClass getHSDevice(EnOceanDeviceType type, string id = "")
        {
            Scheduler.Classes.clsDeviceEnumeration devenum = HS.GetDeviceEnumerator() as Scheduler.Classes.clsDeviceEnumeration;

            while (!devenum.Finished)
            {
                Scheduler.Classes.DeviceClass dev = devenum.GetNext();
                if (dev.get_Interface(null) != Constants.PLUGIN_STRING_NAME)
                    continue; // Not ours!
                if (dev.get_Device_Type_String(null) == "EnOcean " + type.ToString())
                {
                    string hsAddr = dev.get_Address(null); //FIXME: Should probably not use address but a plugin value!
                    if (id == hsAddr)
                    {
                        return dev;
                    }
                }
            }
            return null;
        }
        Dictionary<String, IEnOceanDevice> RegisteredDevices = new Dictionary<string, IEnOceanDevice>();
        public bool RegisterDevice(IEnOceanDevice dev)
        {
            RegisteredDevices.Add(dev.DeviceId, dev);
            return true;
        }
        public Scheduler.Classes.DeviceClass getHSRootDevice()
        {
            // Don't cache hsDevice directly - but we cache HS refId. 
            // We can invalidate root dev by setting hsRootDevRefId to 0
            if (hsRootDevRefId == 0)
            {
                var hsDev = getHSDevice(EnOceanDeviceType.Controller, this.getPortName());
                if (hsDev != null)
                {
                    hsRootDevRefId = hsDev.get_Ref(null);
                    return hsDev;
                }
                Console.WriteLine(" No EnOcean controller HS device - creating.");

                var newDev = createHSDevice("EnOcean controller: " + getPortName(), EnOceanDeviceType.Controller, getPortName());

                newDev.set_Device_Type_String(HS, "EnOcean Controller");
                newDev.MISC_Set(HS, Enums.dvMISC.NO_LOG);
                newDev.MISC_Set(HS, Enums.dvMISC.SHOW_VALUES);
                HS.SaveEventsDevices();
                return newDev;
            }

            Scheduler.Classes.DeviceClass rootDev = (Scheduler.Classes.DeviceClass)HS.GetDeviceByRef(hsRootDevRefId);
            return rootDev;
        }
        public void setControllerStatus(string status)
        {
            internalStatus = status;
            HS.SetDeviceString(rootDev.get_Ref(null), status, false);
        }
        EnOceanFrameLayer controller;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                controller.Close();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        Scheduler.Classes.DeviceClass rootDev;

        public void Close()
        {
            if (controller != null)
            {
                Console.WriteLine("Close controller: {0}", getPortName());
                controller.Close();
                controller = null;
            }
        }
        public Boolean Initialize()
        {
            Dictionary<int, Scheduler.Classes.DeviceClass> device_map = new Dictionary<int, Scheduler.Classes.DeviceClass>();
            //            Dictionary<int, JObject> json_map = new Dictionary<int, JObject>();
            Dictionary<int, bool> processed = new Dictionary<int, bool>();

            //config = new JObject();
            rootDev = getHSRootDevice();
            var extraData = rootDev.get_PlugExtraData_Get(HS);
            if (extraData == null)
                extraData = new PlugExtraData.clsPlugExtraData();
            var typeStr = (string)extraData.GetNamed("EnOcean Type");
            if (typeStr == null)
            {
                Console.WriteLine("No type on device - adding");
                extraData.AddNamed("EnOcean Type", "Controller");
                rootDev.set_PlugExtraData_Set(HS, extraData);
            }
            if (config["nodes"] == null)
            {
                config.Add("nodes", new JObject());
            }

            setControllerStatus("Initializing ctrl instance on port " + getPortName());
            controller = new EnOceanFrameLayer();
            if (controller.Open(getPortName()))
            {
                controller.PacketEventHandler += controller_PacketEvent;
                setControllerStatus("Active");
            }
            else
            {
                setControllerStatus("Port open error!");
                //                GetHSDeviceByAddress(0x1234abcd);
                return false;
            }
            var p = EnOceanPacket.MakePacket_CO_RD_VERSION();
            controller.Send(p, (EnOceanPacket recvPacket) =>
            {
                if (recvPacket.getType() != PacketType.RESPONSE)
                    return false;
                var br = new BinaryReader(new MemoryStream(recvPacket.GetData()));
                Console.WriteLine("Ret Code = {0}", br.ReadByte());
                Console.WriteLine("App Version {0}", br.ReadUInt32());
                Console.WriteLine("API Version {0}", br.ReadUInt32());
                var uniqueControllerId = br.ReadUInt32().ToString("x8");
                this.UniqueControllerId = uniqueControllerId;
                Console.WriteLine("Chip ID {0}", uniqueControllerId);
                Console.WriteLine("Chip Version {0}", br.ReadUInt32());
                var d = Encoding.UTF8.GetString(br.ReadBytes(16), 0, 16);
                Console.WriteLine("APP NAME: {0}", d);
                //TODO: Parse app description
                return true;
            });
            int timeout = 30;
            while (timeout-- > 0 && this.UniqueControllerId == "Unknown")
            {
                Console.WriteLine("Waiting for controller id!");
                Thread.Sleep(100);
            }
            //            GetHSDeviceByAddress(0x1234abcd);
            if (UniqueControllerId == "Unknown")
            {
                Console.WriteLine("USB Device did not respond!");
                setControllerStatus("Initialization error!");
                return false;
            }
            LoadChildDevices();
            return true;
        }
        void LoadChildDevices()
        {
            Scheduler.Classes.clsDeviceEnumeration devenum = HS.GetDeviceEnumerator() as Scheduler.Classes.clsDeviceEnumeration;

            while (!devenum.Finished)
            {
                Scheduler.Classes.DeviceClass hsDev = devenum.GetNext();
                if (hsDev.get_Interface(null) != Constants.PLUGIN_STRING_NAME)
                    continue; // Not ours!
                var extraData = hsDev.get_PlugExtraData_Get(HS);
                if (extraData == null)
                    extraData = new PlugExtraData.clsPlugExtraData();
                var typeStr = (string)extraData.GetNamed("EnOcean Type");
                //                if (typeStr == null)
                //                    Console.WriteLine("Warning: No device type set on child device");
                var dataStr = (string)extraData.GetNamed("EnOcean Cfg");
                var childConfig = new JObject();
                if (dataStr != null)
                    childConfig = JObject.Parse(dataStr);
                if (hsDev.get_Address(null).StartsWith(ControllerId))
                {
                    //                    Console.WriteLine("DT : {0} vs {1}", hsDev.get_Device_Type_String(null), EnOceanDeviceType.SimpleDevice.ToString());
                    if (hsDev.get_Device_Type_String(null) != "EnOcean " + EnOceanDeviceType.SimpleDevice.ToString())
                        continue;
                    DeviceTypes.CreateDeviceInstance(HS, this, hsDev.get_Address(null), (string)childConfig["device_type"], childConfig);
                    Console.WriteLine("Found child device: {0}", hsDev.get_Address(null));
                }
            }

        }
        String UniqueControllerId;
        public String ControllerId { get { if (UniqueControllerId == null) return "unknown"; return UniqueControllerId; } }
        void controller_PacketEvent(EnOceanPacket pkt)
        {
//            Console.WriteLine("Got packet: {0}, opt type {1}", pkt, pkt.Get_OptionalData().getType());
            Console.WriteLine("Got telegram of type: {0}", pkt.getTelegramType());
            if (pkt.Get_OptionalData() != null && pkt.Get_OptionalData().getSize() > 0)
            {
                var odata = pkt.Get_OptionalData();
//                Console.WriteLine(" - destination was {0:X8}", odata.getDestination());
                String childDevId = ControllerId + ":" + pkt.getSource().ToString("x8");
                //                var devInst = RegisteredDevices[ControllerId + ":" + odata.getDestination().ToString("x8")];
                if (RegisteredDevices.ContainsKey(childDevId))
                {
                    var devInst = RegisteredDevices[childDevId];
  //                  Console.WriteLine("Located hsDev : {0}", devInst.DeviceId);
                    devInst.ProcessPacket(pkt);
                }
                else
                {
                    if (pkt.getTelegramType() == TelegramType.TT_4BS && ((pkt.GetData()[4]) & 0x08) == 0x08)
                    {
                        Console.WriteLine("4BS - but no teach in bit, ignoring");
                    }
                    else
                    {
                        Console.WriteLine("Add to AddSeenDevices");
                        AddSeenDevice(childDevId);
                    }
                    Console.WriteLine("Did not locate {0:x8}", childDevId);
                }
            }
        }
        public Dictionary<String, JObject> getSeenDevices()
        {
            return seenDevices;
//            return config["nodes"].ToObject<JObject>();
        }
        Dictionary<String, JObject> seenDevices = new Dictionary<string, JObject>();
        private void AddSeenDevice(String strAddr)
        {
            JObject nInfo = new JObject();
            nInfo.Add("address", strAddr);
            nInfo.Add("configured", false);
            nInfo.Add("first_seen", DateTime.UtcNow);
            seenDevices[strAddr] = nInfo;
//            SaveConfiguration();

        }
        private Scheduler.Classes.DeviceClass GetHSDeviceByAddress(UInt32 p)
        {

            var strAddr = this.ControllerId + ":" + p.ToString("x8");
            Console.WriteLine("Trying to locate device {0} in HS DB", strAddr);
            var srcDev = getHSDevice(EnOceanDeviceType.SimpleDevice, strAddr);

            if (srcDev == null)
            {
                Console.WriteLine("Unknown RF device - checking stuff!");
                var un_list = (JObject)config["nodes"];
                if (un_list[strAddr] != null)
                {
                    Console.WriteLine("Already added to known list");
                    return null;
                }
                AddSeenDevice(strAddr);
                return null;
            }
            else
            {
                return srcDev;
            }
        }
        public string getPortName()
        {
            return portName;
        }

        public string getControllerStatus()
        {
            return internalStatus;
        }

        public void SaveConfiguration()
        {

            var extraData = rootDev.get_PlugExtraData_Get(HS);
            //extraData.AddNamed("EnOcean Type", "Controller");
            rootDev.set_PlugExtraData_Set(HS, extraData);
            config["unique_id"] = UniqueControllerId;
            var dataStr = (string)extraData.GetNamed("EnOcean Cfg");
            //if (dataStr == null) {
            //  Console.WriteLine("No json data on device - adding");
            extraData.AddNamed("EnOcean Cfg", config.ToString());
            rootDev.set_PlugExtraData_Set(HS, extraData);
            HS.SaveEventsDevices();

        }
    }
}

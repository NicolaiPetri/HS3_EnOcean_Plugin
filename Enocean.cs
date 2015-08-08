using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Drawing;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Scheduler;
using HSCF;
using HomeSeerAPI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using EnOcean;

namespace HSPI_EnOcean
{
    public class DeviceState
    {
        public Int64 devRefId;
        public String deviceType;
        public String deviceName;
        public String deviceAddress;
        //        public DeviceChartType chartType;
        public Int64 manualDeviceTypeGroup;
        public DateTime lastValueChange;
        public DateTime lastValuePoll;
        public Decimal lastValue;
        public Boolean collectData;
        public TimeSpan pollInterval;
        public Boolean pollDevice;
        //        public DeviceTypeSettings
    }

    class EnOceanManager
    {
        IHSApplication HS;
        IAppCallbackAPI HSCB;
        JObject config;
        HSPI hspiInst;
        List<EnOceanController> interfaceList = new List<EnOceanController>();
        public EnOceanManager(IHSApplication pHsHost, IAppCallbackAPI pHsCB, HSPI pHspiInst)
        {
            hspiInst = pHspiInst;
            HS = pHsHost;
            HSCB = pHsCB;
        //    Console.WriteLine("Encoding: {0}", System.Text.Encoding.Default);
            //System.Text.Encoding.Default = System.Text.UTF8Encoding.Default;
            //Thread queueHandlerTask = new Thread(new ThreadStart(this.QueueProcesserThread));
            //queueHandlerTask.Start();
        }
        public void AddInterface(EnOceanController newInterface)
        {
            interfaceList.Add(newInterface);
        }
        public IList<EnOceanController> GetInterfaces()
        {
 
            return interfaceList;
        }
        public void Initialize()
        {
            Dictionary<int, Scheduler.Classes.DeviceClass> device_map = new Dictionary<int, Scheduler.Classes.DeviceClass>();
            //            Dictionary<int, JObject> json_map = new Dictionary<int, JObject>();
            Dictionary<int, bool> processed = new Dictionary<int, bool>();

            config = new JObject();
            /*var rootDev = getHSRootDevice();
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
            var dataStr = (string)extraData.GetNamed("EnOcean Cfg");
            if (dataStr == null)
            {
                Console.WriteLine("No json data on device - adding");
                extraData.AddNamed("EnOcean Cfg", config.ToString());
                rootDev.set_PlugExtraData_Set(HS, extraData);
            }
            else
            {
                config = JObject.Parse(dataStr);
                Console.WriteLine("Loaded config: {0}", config.ToString());
            }
            if (config["nodes"] == null)
            {
                config.Add("nodes", new JObject());
            }

            setControllerStatus("Initializing");
            controller = new EnOceanFrameLayer();
            if (controller.Open("com23"))
            {
                controller.PacketEventHandler += controller_PacketEvent;
                setControllerStatus("Active");
            }
            else
            {
                setControllerStatus("Error!");
            }
            GetHSDeviceByAddress(0x1234abcd);
            */
        }

    }
    class EnOceanController
    {
        IHSApplication HS;
        IAppCallbackAPI HSCB;
        String portName;
        String internalStatus;
        //        Connector_Status Status;
      //  bool configurationChanged = true;
        JObject config;
        HSPI hspiInst;
        public EnOceanController(IHSApplication pHsHost, IAppCallbackAPI pHsCB, HSPI pHspiInst, JObject initCfg)
        {
            hspiInst = pHspiInst;
            HS = pHsHost;
            HSCB = pHsCB;
            config = initCfg;
            portName = (string)config["portname"];
        //    Console.WriteLine("Encoding: {0}", System.Text.Encoding.Default);
            //System.Text.Encoding.Default = System.Text.UTF8Encoding.Default;
            //Thread queueHandlerTask = new Thread(new ThreadStart(this.QueueProcesserThread));
            //queueHandlerTask.Start();
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
            //cloudName = HS.GetINISetting("EnOcean", "name", "HS3 Connector");
            //cloudKey = HS.GetINISetting("EnOcean", "authkey", "[missing]");
//            configurationChanged = false;
        }
        int hsRootDevRefId = 0;
        public enum EnOceanDeviceType { Unknown=0, Controller, SimpleDevice };
        public Scheduler.Classes.DeviceClass getHSDevice(EnOceanDeviceType type, string id="")
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
        public Scheduler.Classes.DeviceClass getHSRootDevice()
        {
            // Don't cache hsDevice directly - but we cache HS refId. 
            // We can invalidate root dev by setting hsRootDevRefId to 0
            if (hsRootDevRefId == 0)
            {
                var hsDev = getHSDevice(EnOceanDeviceType.Controller);
                if (hsDev != null)
                {
                    hsRootDevRefId = hsDev.get_Ref(null);
                    return hsDev;
                }
                Console.WriteLine(" No EnOcean controller HS device - creating.");
                // Not found - create device
                hsRootDevRefId = HS.NewDeviceRef("EnOcean Controller");
                var newDev = (Scheduler.Classes.DeviceClass)HS.GetDeviceByRef(hsRootDevRefId);
//                newDev.MISC_Set(HS, Enums.dvMISC.);
//                newDev.MISC_Set(HS, Enums.dvMISC.STATUS_ONLY);
                newDev.set_Interface(HS, Constants.PLUGIN_STRING_NAME);
                newDev.set_InterfaceInstance(HS, "");
                newDev.set_Last_Change(HS, DateTime.Now);

                newDev.set_Device_Type_String(HS, "EnOcean Controller");
                var DT = new DeviceTypeInfo_m.DeviceTypeInfo();
                DT.Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Plug_In;
                DT.Device_Type = 33;
                DT.Device_SubType = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceType_Plugin.Root;
                newDev.set_DeviceType_Set(HS, DT);

                VSVGPairs.VSPair v = new VSVGPairs.VSPair(ePairStatusControl.Both);
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
                newDev.MISC_Set(HS, Enums.dvMISC.NO_LOG);
                newDev.MISC_Set(HS, Enums.dvMISC.SHOW_VALUES);
                newDev.set_DeviceType_Set(HS, DT);
//                newDev.set_Address(HS, "NA");
                HS.SaveEventsDevices();
                return newDev;
            }

            Scheduler.Classes.DeviceClass rootDev = (Scheduler.Classes.DeviceClass)HS.GetDeviceByRef(hsRootDevRefId);
            return rootDev;
        }
        public void setControllerStatus(string status)
        {
            var rootDev = getHSRootDevice();
            internalStatus = status;
            HS.SetDeviceString(rootDev.get_Ref(null), status, false);
        }
        EnOceanFrameLayer controller;

        public void Initialize()
        {
            Dictionary<int, Scheduler.Classes.DeviceClass> device_map = new Dictionary<int, Scheduler.Classes.DeviceClass>();
            //            Dictionary<int, JObject> json_map = new Dictionary<int, JObject>();
            Dictionary<int, bool> processed = new Dictionary<int, bool>();

            //config = new JObject();
            var rootDev = getHSRootDevice();
            var extraData = rootDev.get_PlugExtraData_Get(HS);
            if (extraData == null)
                extraData = new PlugExtraData.clsPlugExtraData();
            var typeStr = (string)extraData.GetNamed("EnOcean Type");
            if (typeStr == null) {
                Console.WriteLine("No type on device - adding");
                extraData.AddNamed("EnOcean Type", "Controller");
                rootDev.set_PlugExtraData_Set(HS, extraData);
            } 
/*            var dataStr = (string)extraData.GetNamed("EnOcean Cfg");
            if (dataStr == null) {
                Console.WriteLine("No json data on device - adding");
                extraData.AddNamed("EnOcean Cfg", config.ToString());
                rootDev.set_PlugExtraData_Set(HS, extraData);
            } else {
                config = JObject.Parse(dataStr);
                Console.WriteLine("Loaded config: {0}", config.ToString());
            }
 */
            if (config["nodes"] == null)
            {
                config.Add("nodes", new JObject());
            }

            setControllerStatus("Initializing");
            controller = new EnOceanFrameLayer();
            if (controller.Open("com23")) {
                controller.PacketEventHandler += controller_PacketEvent;
                setControllerStatus("Active");
            } else {
                setControllerStatus("Error!");
            }
            GetHSDeviceByAddress(0x1234abcd);
        }
        void controller_PacketEvent(EnOceanPacket pkt)
        {
            Console.WriteLine("Got packet: {0}, opt type {1}", pkt, pkt.Get_OptionalData().getType());
            if (pkt.Get_OptionalData() != null) {
                var odata = pkt.Get_OptionalData();
                Console.WriteLine(" - destination was {0:X8}", odata.getDestination());
                var hsDev = GetHSDeviceByAddress(pkt.getSource());
            }
            //pkt.Get_OptionalData().

            /* Todo: 
             * 
             * 1: Locate sender
             * (2: check destination - me or broadcast)
             * 3: Add device to seen list or dispatch related event if device is 
             * ed
             */
            

            //throw new NotImplementedException();
        }
        public JObject getSeenDevices()
        {
            return config["nodes"].ToObject<JObject>() ;
        }
        private Scheduler.Classes.DeviceClass GetHSDeviceByAddress(UInt32 p)
        {

            Console.WriteLine("Trying to locate device {0:X8} in HS DB", p);
            var strAddr = p.ToString("x8");
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
                JObject nInfo = new JObject();
                nInfo.Add("address", strAddr);
                nInfo.Add("configured", false);
                nInfo.Add("first_seen", DateTime.UtcNow);
                un_list[strAddr] = nInfo;
                return null;
            }
            else
            {
                return srcDev;
            }
//            throw new NotImplementedException();
        }
        void JsonAddDeviceEvent(string Name, JObject JDest)
        {
            //return;
            JObject f = (JObject)(JDest["events"]);
            //   JDest["events"].Add(Name, new JObject());
        }
        /*        bool IsDeviceControlable(Scheduler.Classes.DeviceClass Device)
                {
                    if (Device.MISC_Check(null, Enums.dvMISC.STATUS_ONLY))
                        return false;
                    if (Device.get_Buttons(null) == "")
                        return false;

                    return true;
                }*/
        void JsonAddDeviceEntryPoints(Scheduler.Classes.DeviceClass Device, string Name, JObject JDest)
        {
            JObject jDesc = new JObject();
            JsonAddDeviceEvent("value_change", jDesc);
            //            JsonAddDeviceEvent("", JDesc);
            string dev_addr = Device.get_Address(null);
            //            if (dev_addr == "")
            //dev_addr = "HS-ID-"+Device.get_Ref(null).ToString();
            jDesc.Add("address", dev_addr);
            jDesc.Add("unique_id", "HS-ID-" + Device.get_Ref(null));
            jDesc.Add("name", Device.get_Name(null));
            jDesc.Add("id", Device.get_Ref(null));

            jDesc.Add("type", Device.get_Device_Type_String(null));
            DateTime lastChange = Device.get_Last_Change(null);
            if (lastChange > DateTime.MinValue)
            {
                jDesc.Add("value", Device.get_devValue(null));
                jDesc.Add("time", Device.get_Last_Change(null));
            }
            jDesc.Add("location", Device.get_Location(null));
            jDesc.Add("sublocation", Device.get_Location2(null));
            jDesc.Add("interface", Device.get_Interface(null));
            var vsps = HS.DeviceVSP_GetAllStatus(Device.get_Ref(null));
            Console.WriteLine("Got a total of {0} VSP's", vsps.Length);
            JArray jVsps = new JArray();
            int controlCount = 0;
            foreach (var vsp in vsps)
            {
                JObject jVsp = new JObject();
                jVsp.Add("pair_use", vsp.ControlStatus.ToString());
                if (vsp.ControlStatus != ePairStatusControl.Status)
                    controlCount++;
                jVsp.Add("pair_type", vsp.PairType.ToString());
                jVsp.Add("include_values", vsp.IncludeValues);
                jVsp.Add("render", vsp.Render.ToString());
                if (vsp.PairType == VSVGPairs.VSVGPairType.SingleValue)
                {
                  //  vsp["foo"];
                    jVsp.Add("value", vsp.Value);
                    jVsp.Add("value_renderer", vsp.GetPairString(vsp.Value, "", new string[0]));
                }
                else
                {
                    jVsp.Add("value_renderer", vsp.GetPairString(vsp.RangeStart, "", new string[0]));
                    jVsp.Add("value_start", vsp.RangeStart);
                    jVsp.Add("value_end", vsp.RangeEnd);
                    jVsp.Add("value_prefix", vsp.RangeStatusPrefix);
                    /*                    string rss = vsp.RangeStatusSuffix.ToString();
                                        byte[] srcBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(rss);
                                        string rssUTF = Encoding.GetEncoding("UTF-8").GetString(srcBytes);
                                        jVsp.Add("value_suffix", rssUTF);
                     */
                    jVsp.Add("value_suffix", vsp.RangeStatusSuffix.ToString());
                }
                jVsps.Add(jVsp);
            }
            jDesc.Add("supported_values", jVsps);
            try
            {
                if (controlCount == 0)
                {
                    // Datasource
                    //                if ((JDest["datasources"] as JObject).)
                    (JDest["datasources"] as JObject).Add(Name, jDesc);

                }
                else
                {
                    // Controlpoints
                    (JDest["controls"] as JObject).Add(Name, jDesc);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error adding {0} to device.. : {1}", Name, e.Message);
            }
        }
        public bool controlDeviceByValue(int refId, double newValue) {
            var arrCC = HS.CAPIGetControl(refId);
            foreach (var CC in arrCC) {
                if (CC.Range != null) {
                    if (newValue >= CC.Range.RangeStart && newValue <= CC.Range.RangeEnd) {
                       // CC.ControlValue = newValue;
                        HS.CAPIControlHandler(CC);
                        return true;
                    }
                } else if (CC.ControlValue == newValue) {
                    HS.CAPIControlHandler(CC);
                    return true;
                }
            }

            return false;
        }
        /*
          Dim Found As Boolean = False
    For Each CC In arrCC
        If CC Is Nothing Then Continue For
        If CC.Label Is Nothing Then Continue For
        If GotValue Then
            If CC.Range IsNot Nothing Then
                If Value >= CC.Range.RangeStart AndAlso Value <= CC.Range.RangeEnd Then
                    Found = True
                    Exit For
                End If
            Else
                If CC.ControlValue = Value Then
                    Found = True
                    Exit For
                End If
            End If
        Else
            If CC.Label.Trim.ToLower = Label.Trim.ToLower Then
                Found = True
                Exit For
            End If
        End If
    Next
        */
 
        public bool doPostRequest(String pUrl, string Payload)
        {
            try
            {
                WebClient wc = new WebClient();
                wc.Headers.Add("Content-Type", "application/json; charset=utf-8");
                string resp = wc.UploadString(pUrl, Payload);
                try
                {
                    var respJson = JObject.Parse(resp);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error parsing response : {0}\n{1}", e.Message, resp);
                }
                return true;
            }
            catch (WebException e)
            {
                Console.WriteLine("Error doing web request! : {0}", e.Message);
                HttpWebResponse myHttpWebResponse = e.Response as HttpWebResponse;
/*                if (myHttpWebResponse != null)
                {
                    if (myHttpWebResponse.StatusCode == HttpStatusCode.Unauthorized)
                        Status = Connector_Status.AUTH_FAILED;
                    else
                        Status = Connector_Status.UNKNOWN;
                }
                else
                {
                    Console.WriteLine(" - REQUEST ERROR - but no response object!!");
                }
*/                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Other EXCEPTION error : {0}", e.Message);
                return false;
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
    }
}

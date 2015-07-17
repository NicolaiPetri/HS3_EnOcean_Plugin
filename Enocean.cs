﻿using System;
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
    class EnOceanController
    {
        IHSApplication HS;
        IAppCallbackAPI HSCB;
//        Connector_Status Status;
      //  bool configurationChanged = true;
        string cloudKey;
        string cloudName;
        JObject config;
        HSPI hspiInst;
        public EnOceanController(IHSApplication pHsHost, IAppCallbackAPI pHsCB, HSPI pHspiInst)
        {
            hspiInst = pHspiInst;
            HS = pHsHost;
            HSCB = pHsCB;
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
            cloudName = HS.GetINISetting("EnOcean", "name", "HS3 Connector");
            cloudKey = HS.GetINISetting("EnOcean", "authkey", "[missing]");
//            configurationChanged = false;
        }
        int hsRootDevRefId = 0;
        public Scheduler.Classes.DeviceClass getHSRootDevice()
        {
            // Don't cache hsDevice directly - but we cache HS refId. 
            // We can invalidate root dev by setting hsRootDevRefId to 0
            if (hsRootDevRefId == 0)
            {
                Scheduler.Classes.clsDeviceEnumeration devenum = HS.GetDeviceEnumerator() as Scheduler.Classes.clsDeviceEnumeration;

                while (!devenum.Finished)
                {
                    Scheduler.Classes.DeviceClass dev = devenum.GetNext();
                    if (dev.get_Device_Type_String(null) == "EnOcean Controller")
                    {
                        hsRootDevRefId = dev.get_Ref(null);
                        return dev;
                    } 
                }
                Console.WriteLine(" No EnOcean controller HS device - creating.");
                // Not found - create device
                int newDevRef = HS.NewDeviceRef("EnOcean Controller");
                var newDev = (Scheduler.Classes.DeviceClass)HS.GetDeviceByRef(newDevRef);
                newDev.set_Device_Type_String(HS, "EnOcean Controller");
            }

            Scheduler.Classes.DeviceClass rootDev = (Scheduler.Classes.DeviceClass)HS.GetDeviceByRef(hsRootDevRefId);
            return rootDev;
        }
        public void setControllerStatus(string status)
        {
            var rootDev = getHSRootDevice();
            HS.SetDeviceString(rootDev.get_Ref(null), status, false);
        }
        EnOceanFrameLayer controller;

        public void Initialize()
        {
            Dictionary<int, Scheduler.Classes.DeviceClass> device_map = new Dictionary<int, Scheduler.Classes.DeviceClass>();
            //            Dictionary<int, JObject> json_map = new Dictionary<int, JObject>();
            Dictionary<int, bool> processed = new Dictionary<int, bool>();

            config = new JObject();
            var rootDev = getHSRootDevice();
            var extraData = rootDev.get_PlugExtraData_Get(null);
            var dataStr = (string)extraData.GetNamed("EnOcean Cfg");
            if (dataStr == null)
            {
                Console.WriteLine("No json data on device - adding");
                extraData.AddNamed("EnOcean Cfg", config.ToString());
                rootDev.set_PlugExtraData_Set(HS, extraData);
                HS.SaveEventsDevices();
            }
            else
            {
                config = JObject.Parse(dataStr);
                Console.WriteLine("Loaded config: {0}", config.ToString());
            }
            setControllerStatus("Initializing");
            controller = new EnOceanFrameLayer();
            if (controller.Open("com23"))
            {
                setControllerStatus("Active");
            }
            else
            {
                setControllerStatus("Error!");
            }
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
    }
}

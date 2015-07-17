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
        HSPI hspiInst;
        public EnOceanController(IHSApplication pHsHost, IAppCallbackAPI pHsCB, HSPI pHspiInst)
        {
            hspiInst = pHspiInst;
            HS = pHsHost;
            HSCB = pHsCB;
            Console.WriteLine("Encoding: {0}", System.Text.Encoding.Default);
            //System.Text.Encoding.Default = System.Text.UTF8Encoding.Default;
            Thread queueHandlerTask = new Thread(new ThreadStart(this.QueueProcesserThread));
            queueHandlerTask.Start();
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
        public void Initialize()
        {
            Dictionary<int, Scheduler.Classes.DeviceClass> device_map = new Dictionary<int, Scheduler.Classes.DeviceClass>();
            //            Dictionary<int, JObject> json_map = new Dictionary<int, JObject>();
            Dictionary<int, bool> processed = new Dictionary<int, bool>();
            return;
            try
            {
                JObject jConfig = new JObject();
                processed.Clear();
                device_map.Clear();

                JArray jDevices = new JArray();
                Scheduler.Classes.clsDeviceEnumeration devenum = HS.GetDeviceEnumerator() as Scheduler.Classes.clsDeviceEnumeration;

                while (!devenum.Finished)
                {
                    Scheduler.Classes.DeviceClass dev = devenum.GetNext();
                    device_map.Add(dev.get_Ref(null), dev);
                }
                foreach (var dev in device_map.Values)
                {
                    /*
                    string devImage = dev.get_Image(null);
                    try {
                        if (devImage != "")
                        {
                            Console.WriteLine("Got image path of {0}", devImage);
                            string baseName = Path.GetFileName(devImage);
                            string fpath = Path.GetFullPath("image_cache/" + baseName);
                            WebClient wc = new WebClient();
                            string fullUrl = "http://128.0.7.100:8080/" + devImage.Replace('\\', '/');
                            Console.WriteLine("Fetching image from {0}", fullUrl);
                            if (File.Exists(fpath) == false)
                            {
                                Stream stream = wc.OpenRead(fullUrl);
                                FileStream outfile = new FileStream(fpath, FileMode.Create);
                                stream.CopyTo(outfile);
                                outfile.Flush();
                                outfile.Close();
                            }
                            FileInfo fi = new FileInfo(fpath);
                            if (fi.Length > 5000)
                            {
                                Console.WriteLine("Image too big as thumbnail : {0}", fi.Length);
                                Image srcImage = Image.FromFile(fpath);
                                Image destImage = new Bitmap(srcImage, new Size(32, 32));
                                string destPath = "thumbs/32x32_" + Path.GetFileName(fpath);
                                HS.WriteHTMLImage(destImage, destPath, true);
                                dev.set_Image(HS, "images/" + destPath);
                                //Console.WriteLine("Resized to : {0}", 
                                //                            destImage.Save()
                            }
                          //Bitmap bitmap = new Bitmap(stream);
                          //Image img =
                               
                          //image.source = bitmap;                       
                        //string result = HS.GetURLImageEx("128.0.7.100", devImage, fpath, 8080);
                       // if (result == "") { 
                         //   FileInfo f = new FileInfo(fpath);
                           // Console.WriteLine("Got file image of size {0}", f.Length);
                        //} else {
                          //  Console.WriteLine("Error: {0}", result);
                        //}
                    }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Some error testing thumbnail: {0}", e.Message);
                    }*/
                    if (dev.get_Relationship(null) == Enums.eRelationship.Child)
                        continue;
                    JObject jDev = new JObject();
                    jDev.Add("name", dev.get_Name(null));
                    Console.WriteLine("Got device : {0} ({1})", dev.get_Name(null), dev.get_Ref(null));
                    string dev_addr = dev.get_Address(null);
                    //                    if (dev_addr == "")
                    jDev.Add("address", dev_addr);
                    jDev.Add("unique_id", "HS-ID-" + dev.get_Ref(null));
                    jDev.Add("id", dev.get_Ref(null));
                    jDev.Add("location", dev.get_Location(null));
                    jDev.Add("sublocation", dev.get_Location2(null));
                    DateTime lastChange = dev.get_Last_Change(null);
                    if (lastChange > DateTime.MinValue)
                    {
                        jDev.Add("value", dev.get_devValue(null));
                        jDev.Add("time", dev.get_Last_Change(null));
                    }

                    jDev.Add("type", dev.get_Device_Type_String(null));
                    jDev.Add("interface", dev.get_Interface(null));
                    jDev.Add("scale", dev.get_ScaleText(null));
                    jDev.Add("note", dev.get_UserNote(null));
                    jDev.Add("attention", dev.get_Attention(null));
                    jDev.Add("version", dev.Version);
                    jDev.Add("controls", new JObject());
                    jDev.Add("datasources", new JObject());
                    jDev.Add("events", new JObject());
                    //               jDev.Add("foo", dev.get_Last_Change(null));
                    switch (dev.get_Relationship(null))
                    {
                        case Enums.eRelationship.Child:
                            throw new Exception("Should never happen here! Skipping device - we are a child-- fixme");
                        //break;
                        case Enums.eRelationship.Standalone:
                        case Enums.eRelationship.Not_Set:
                            Console.WriteLine("Unknown or standalone device - we are a child -- fixme, {0} -> {1}", dev.get_Ref(null), dev.get_Address(null));
                            if (processed.ContainsKey(dev.get_Ref(null)))
                            {
                                Console.WriteLine(" DUPL !!!!");
                                continue;
                            }
                            jDevices.Add(jDev);
                            JsonAddDeviceEntryPoints(dev, "HS-ID-" + dev.get_Ref(null), jDev);
                            processed.Add(dev.get_Ref(null), true);
                            break;
                        case Enums.eRelationship.Parent_Root:
                            Console.WriteLine("Child List : {0}", string.Join(", ", dev.get_AssociatedDevices(null)));
                            foreach (var childRef in dev.get_AssociatedDevices(null))
                            {
                                Scheduler.Classes.DeviceClass childDev = device_map[childRef];
                                Console.WriteLine("   -> ChildRef {0}: {1} {2}", childRef, childDev.get_Address(null), childDev.get_Relationship(null).ToString());
                                if (processed.ContainsKey(childRef))
                                {
                                    Console.WriteLine(" CHLD DUPL !!!!");
                                    continue;
                                }

                                JsonAddDeviceEntryPoints(childDev, "HS-ID-" + childRef, jDev);
                                Console.WriteLine("Marking {0} as processed!", childDev.get_Ref(null));
                                processed.Add(childDev.get_Ref(null), true);
                            }
                            jDevices.Add(jDev);
                            processed.Add(dev.get_Ref(null), true);
                            break;
                        default:
                            throw new Exception("FIX ME - unhandled relationship");
                    }
                    Console.WriteLine("Marking {0} as processed!", dev.get_Ref(null));
                    //                jDev.Add("foo", );
                }
                //   Console.WriteLine("Got json of : {0}", jDevices.ToString());
                HS.SaveEventsDevices();
                JObject jSystem = new JObject();
                jSystem.Add("name", cloudName);
                jConfig.Add("devices", jDevices);
                jConfig.Add("system", jSystem);
                jConfig.Add("time", DateTime.UtcNow.ToString());

                //QueueEvent("Connector", "Inventory", jConfig);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error enumerating devices: {0}", e.Message);
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

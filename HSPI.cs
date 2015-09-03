using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

using System.Threading;
using HomeSeerAPI;
using EnOcean;
//using HomeSeerAPI.VGVSPair;

//using SensoristAPI;

//using System.Web.HttpUtility;


//using Microsoft.Win32;
[assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]
namespace HSPI_EnOcean
{

    public class HSPI : IPlugInAPI, IDisposable
    {
        private EnOcean.EnOceanManager mCore;
        private Dictionary<int, DateTime> mLastDSUpdate = new Dictionary<int, DateTime>();
        PageBuilder mPageBuilder;

        //public string Name = "Sensorist HS3 Plugin";
        //public bool HSCOMPort = false;
        //	private Dictionary<int, int> mSensorInstance2HsRef;
        private string mFriendlyName = Constants.PLUGIN_STRING_NAME;
        public bool Running = true;
        public IHSApplication hsHost;
        public IAppCallbackAPI hsHostCB;
        //	Dictionary<int, SensoristAPI.Connector> cloudInstances;
        // HS3 Plugin properties
        public string Name { get; private set; }

        public bool HSCOMPort { get; private set; }

        public bool ActionAdvancedMode { set; get; }

        public bool HasTriggers { get; private set; }

        public int TriggerCount { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                mPageBuilder.Dispose();
                mPageBuilder = null;
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        // HS3 Plugin methods
        public int AccessLevel()
        {
            return 1;  // 2 == Needs license, 1 .. free ?
        }

        public string ActionBuildUI(string sUnique, HomeSeerAPI.IPlugInAPI.strTrigActInfo ActInfo)
        {
            return "";
            //return null;
        }

        public bool ActionConfigured(HomeSeerAPI.IPlugInAPI.strTrigActInfo ActInfo)
        {
            return false;
        }

        public int ActionCount()
        {
            return 0;
        }

        public string ActionFormatUI(HomeSeerAPI.IPlugInAPI.strTrigActInfo ActInfo)
        {
            return "";
            //return null;
        }

        public HomeSeerAPI.IPlugInAPI.strMultiReturn ActionProcessPostUI(System.Collections.Specialized.NameValueCollection PostData, HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfoIN)
        {
            HomeSeerAPI.IPlugInAPI.strMultiReturn r = new IPlugInAPI.strMultiReturn();
            r.sResult = "NOT IMPLEMENTED";
            return r;
        }

        public bool ActionReferencesDevice(HomeSeerAPI.IPlugInAPI.strTrigActInfo ActInfo, int dvRef)
        {
            return false;
        }

        public int Capabilities()
        {
            return (int)HomeSeerAPI.Enums.eCapabilities.CA_IO;
        }

        public string ConfigDevice(int pDevRefId, string user, int userRights, bool pNewDevice)
        {
            return "";
            //		StringBuilder stb = new StringBuilder();
            //			String pageName = "NPChartsConfigDevice";
            //		return mPageBuilder.GetDeviceChartPage(pageName, devref, null);
//            return mPageBuilder.GetPage("NPEasyCharts/Chart", "ds=" + pDevRefId + "&inlinePage=1");
            //return mPageBuilder.GetConfigPage(pDevRefId, pNewDevice);
            //return "<b>TODO</b>";
        }

        public HomeSeerAPI.Enums.ConfigDevicePostReturn ConfigDevicePost(int devref, string data, string user, int userRights)
        {
            //HomeSeerAPI.Enums.ConfigDevicePostReturn r= new Enums.ConfigDevicePostReturn();
            return Enums.ConfigDevicePostReturn.DoneAndCancelAndStay;
        }

        public string GenPage(string link)
        {
            return "old stuff";
        }

        public string get_ActionName(int ActionNumber)
        {
            return "";
//            return null;
        }

        public bool get_Condition(HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo)
        {
            return false;
        }

        public bool get_HasConditions(int TriggerNumber)
        {
            return false;
        }

        public int get_SubTriggerCount(int TriggerNumber)
        {
            return 0;
        }

        public string get_SubTriggerName(int TriggerNumber, int SubTriggerNumber)
        {
            return "";
            //return null;
        }

        public bool get_TriggerConfigured(HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo)
        {
            return false;
        }

        public string get_TriggerName(int TriggerNumber)
        {
            return "";
            //return null;
        }

        public string GetPagePlugin(string page, string user, int userRights, string queryString)
        {

            //		return "<div> Not here </div>";
            return mPageBuilder.GetPage(page, queryString);
        }

        public bool HandleAction(HomeSeerAPI.IPlugInAPI.strTrigActInfo ActInfo)
        {
            return false;
        }

        
        public void HSEvent(HomeSeerAPI.Enums.HSEvent EventType, object[] parms)
        {
            JObject evtData = new JObject();
            try
            {
                string Source = "Unknown";
                evtData.Add("PARAMS", string.Join(", ", parms));
                switch (EventType)
                {
                    case Enums.HSEvent.CONFIG_CHANGE:
                        {
                            Console.WriteLine("Got CONFIG_CHANGE event");
                            Console.WriteLine(" - HSEvent {0}: {1}/{2}/{3}/{4}/{5}", EventType.ToString(), parms[5].ToString(), parms[0].ToString(), parms[1].ToString(), parms[2].ToString(), parms[3].ToString());
                            evtData.Add("time", DateTime.UtcNow.ToString());
                            //mCore.ReloadConfig(15); // Delay atleast 15 seconds to make time for other changes.
                        }
                        break;
                    case Enums.HSEvent.LOG:
                        //                    Source = parms[1] as string;
                        evtData.Add("typeid", parms[0] as string);
                        DateTime parsedTime;
                        if (DateTime.TryParse(parms[1] as string, out parsedTime))
                        {
                            evtData.Add("time", parsedTime);
                        }
                        else
                        {
                            // FIXME: Should not happen
                            Console.WriteLine("Failed time parse.. FIXME");
                            evtData.Add("time", DateTime.UtcNow);

                        }
                        
                        evtData.Add("category", parms[2] as string);
                        evtData.Add("message", parms[3] as string);
                        break;
                    case Enums.HSEvent.STRING_CHANGE:
                        {
                            Source = parms[3].ToString();
                            string dev_addr = parms[1].ToString();
                            //                         if (dev_addr == "")
                            evtData.Add("unique_id", "HS-ID-" + Source);
                            evtData.Add("address", dev_addr);
//                            evtData.Add("address", parms[1].ToString());
                            evtData.Add("value", parms[2].ToString());
                            Int32 sid = Int32.Parse(Source);
                            var ta = hsHost.DeviceLastChangeRef(sid);
                            evtData.Add("time", ta.ToUniversalTime());
                        }
                        break;
                    case Enums.HSEvent.VALUE_CHANGE:
                        {
                            Source = parms[4].ToString();
                            string dev_addr = parms[1].ToString();
                            evtData.Add("unique_id", "HS-ID-" + Source);
                            evtData.Add("address", dev_addr);
                            evtData.Add("value", parms[2].ToString());
                            evtData.Add("oldvalue", parms[3].ToString());
                            Int32 sid = Int32.Parse(Source);
                            evtData.Add("string_value", hsHost.DeviceString(sid));
                            var ta = hsHost.DeviceLastChangeRef(sid);
                            evtData.Add("time", ta.ToUniversalTime());
                            
                            //                    var tv = hsHost.DeviceValueEx(sid);
                            //                  Console.WriteLine("TA = {0}, TV={1}", ta, tv);
                            //                    mCore.deviceValueChanged((int)parms[4], (int)Utils.ToUnixTime(DateTime.UtcNow), (double)parms[2]);
                        }
                        break;
                    default:
                        evtData.Add("time", DateTime.UtcNow.ToString());
                        //                    evtData.Add("PARAMS", string.Format(" - HSEvent {0}: {1}/{2}/{3}/{4}/{5}", EventType.ToString(), parms[5].ToString(), parms[0].ToString(), parms[1].ToString(), parms[2].ToString(), parms[3].ToString()));
                        Console.WriteLine("No handler yet for HSEvent type {0}", EventType.ToString());
                        Console.WriteLine(" - HSEvent {0}: {1}", EventType.ToString(), String.Join(" | ", parms));
                        break;
                }
                //mCore.QueueEvent(Source, EventType.ToString(), evtData);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception something foo: {0}", e.Message);
            }
        }

        public string InitIO(string port)
        {
            Console.WriteLine("InitIO called!");
            Thread.Sleep(5000);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

     /*       try
            {
                Directory.CreateDirectory("Data/" + Constants.PLUGIN_STRING_ID);
                Console.WriteLine("Got dir created");
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not create directory: {0}", e.Message);
            }*/
            mCore = new EnOceanManager(hsHost, hsHostCB, this);
            mCore.Initialize();
/*
            Scheduler.Classes.clsDeviceEnumeration devenum = hsHost.GetDeviceEnumerator() as Scheduler.Classes.clsDeviceEnumeration;

           while (!devenum.Finished)
            {
                Scheduler.Classes.DeviceClass dev = devenum.GetNext();
                var devInfo = dev.get_DeviceType_Get(hsHost);
                Console.WriteLine(" got device of type {0}:{1} => {2}:{3} ({4})", devInfo.Device_Type, devInfo.Device_SubType, devInfo.Device_Type_Description, devInfo.Device_SubType_Description, dev.get_Ref(null));
 //               devInfo.
//                device_map.Add(dev.get_Ref(null), dev);
            }
  */ 
//            hsHost.Device

            //hsHostCB.RegisterEventCB(Enums.HSEvent.VALUE_CHANGE, Name, "");
            hsHostCB.RegisterEventCB(Enums.HSEvent.CONFIG_CHANGE, Name, "");
            //hsHostCB.RegisterEventCB(Enums.HSEvent.LOG, Name, "");
            hsHostCB.RegisterEventCB(Enums.HSEvent.SETUP_CHANGE, Name, "");
            //hsHostCB.RegisterEventCB(Enums.HSEvent.STRING_CHANGE, Name, "");
            hsHostCB.RegisterEventCB(Enums.HSEvent.GENERIC, Name, "");

            this.mPageBuilder = new PageBuilder(hsHost, hsHostCB, this, mCore);

//            hsHost.RegisterPage(Constants.PLUGIN_STRING_ID + "/Configuration", Name, "");
//            hsHost.RegisterPage(Constants.PLUGIN_STRING_ID + "/Charts", Name, "");
//            hsHost.RegisterPage(Constants.PLUGIN_STRING_ID + "/Chart", Name, "");
            hsHost.RegisterPage(Constants.PLUGIN_STRING_ID, Name, "");
            hsHost.RegisterPage(Constants.PLUGIN_STRING_ID+"_Interfaces", Name, "");
//            hsHost.Register
/*            WebPageDesc wpdCharts = new WebPageDesc();
            wpdCharts.link = Constants.PLUGIN_STRING_ID + "/Charts";
            wpdCharts.linktext = "Charts";
            wpdCharts.page_title = "Charts";
            wpdCharts.plugInName = Name;
            hsHostCB.RegisterLink(wpdCharts);
*/              
            WebPageDesc foo = new WebPageDesc();
            

            WebPageDesc wpd = new WebPageDesc();
            wpd.link = Constants.PLUGIN_STRING_ID;
            wpd.linktext = "Configuration";
            wpd.page_title = "Configuration";
            wpd.plugInName = Name;
            hsHostCB.RegisterLink(wpd);
            hsHostCB.RegisterConfigLink(wpd);
            
            WebPageDesc wpi = new WebPageDesc();
            wpi.link = Constants.PLUGIN_STRING_ID+"_Interfaces";
            wpi.linktext = "Interfaces";
            wpi.page_title = "Interfaces";
            wpi.plugInName = Name;
            hsHostCB.RegisterLink(wpi);
            //hsHostCB.RegisterConfigLink(wpi);

            /*      WebPageDesc wpd3 = new WebPageDesc();
                  wpd3.link = Constants.PLUGIN_STRING_ID + "/Chart";
                  wpd3.linktext = "Chart";
                  wpd3.page_title = "Chart";
                  wpd3.plugInName = Name;
                  hsHostCB.RegisterLink(wpd3);
             */
            Console.WriteLine("INIT IO complete");
            return ""; // empty == OK 
        }

        public string InstanceFriendlyName()
        {
            return mFriendlyName;
        }

        public HomeSeerAPI.IPlugInAPI.strInterfaceStatus InterfaceStatus()
        {
            HomeSeerAPI.IPlugInAPI.strInterfaceStatus r = new IPlugInAPI.strInterfaceStatus();
            r.intStatus = IPlugInAPI.enumInterfaceStatus.OK;
            r.sStatus = "Interface OK";
            return r;
        }

        public string PagePut(string data)
        {
            return "";
            //return null;
        }

        public object PluginFunction(string procName, object[] parms)
        {
            return "";
//            return null;
        }

        public object PluginPropertyGet(string procName, object[] parms)
        {
            return "";
//            return null;
        }

        public void PluginPropertySet(string procName, object value)
        {
        }

        public HomeSeerAPI.IPlugInAPI.PollResultInfo PollDevice(int dvref)
        {
            HomeSeerAPI.IPlugInAPI.PollResultInfo r = new IPlugInAPI.PollResultInfo();

            return r;
        }

        public string PostBackProc(string pPage, string pData, string pUser, int pUserRights)
        {
            return mPageBuilder.PostBack(pPage, pData, pUser, pUserRights);

        }

        public bool RaisesGenericCallbacks()
        {
            return true;
        }

        public HomeSeerAPI.SearchReturn[] Search(string SearchString, bool RegEx)
        {
            SearchReturn[] result = new SearchReturn[0];
            return result;
        }

        public void set_Condition(HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo, bool Value)
        {
        }

        public void SetIOMulti(System.Collections.Generic.List<CAPI.CAPIControl> colSend)
        {
            Console.WriteLine("Set IO Multi : {0}", colSend[0]);
        }

        public void ShutdownIO()
        {
            Running = false;
        }

        public void SpeakIn(int device, string txt, bool w, string host)
        {
        }

        public bool SupportsAddDevice()
        {
            return false;
        }

        public bool SupportsConfigDevice()
        {
            return false;
        }

        public bool SupportsConfigDeviceAll()
        {
            return false;
        }

        public bool SupportsMultipleInstances()
        {
            return false;
        }

        public bool SupportsMultipleInstancesSingleEXE()
        {
            return false;
        }

        public string TriggerBuildUI(string sUnique, HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo)
        {
            return "";
//            return null;
        }

        public string TriggerFormatUI(HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo)
        {
            return "";
//            return null;
        }

        public HomeSeerAPI.IPlugInAPI.strMultiReturn TriggerProcessPostUI(System.Collections.Specialized.NameValueCollection PostData, HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfoIN)
        {
            HomeSeerAPI.IPlugInAPI.strMultiReturn r = new IPlugInAPI.strMultiReturn();
            //            r.
            return r;
        }

        public bool TriggerReferencesDevice(HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo, int dvRef)
        {
            return false;
        }

        public bool TriggerTrue(HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo)
        {
            return false;
        }

        public HSPI()
            : this("")
        {
        }

        public HSPI(String pInstance)
        {
            Name = Constants.PLUGIN_STRING_NAME;
            HSCOMPort = false;
            //ActionAdvancedMode = xxx; // TODO:
            HasTriggers = false;
            TriggerCount = 0;

        }

    }
}

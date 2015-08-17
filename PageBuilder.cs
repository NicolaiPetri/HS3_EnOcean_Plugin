using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Linq;
using System.Web;
using System.Reflection;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Globalization;
using NPossible.Common;
//using HSCF;
using HomeSeerAPI;
using Scheduler;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace NPossible.Common
{

    public class PageReturn
    {
        public int return_code;
        public String content;
        public String content_type;
        public bool full_page;
        public PageReturn(String pContent, bool pFullPage = false, String pContentType = "text/html", int pReturnCode = 200)
        {
            return_code = pReturnCode;
            content = pContent;
            content_type = pContentType;
            full_page = pFullPage;
        }
    }
    /// <remarks>
    /// Class that contains basic functions for building HTML pages and processing postbacks
    /// </remarks>
    public class PageBuilderBase : Scheduler.PageBuilderAndMenu.clsPageBuilder
    {
        protected IHSApplication hsHost;
        protected IAppCallbackAPI hsHostCB;
        protected HSPI_EnOcean.HSPI pluginInstance;
        //		private StringBuilder stb;

        public PageBuilderBase(IHSApplication pHS, IAppCallbackAPI pHSCB, HSPI_EnOcean.HSPI plugInInst)
            : base("dummy")
        {
            hsHost = pHS;
            hsHostCB = pHSCB;
            pluginInstance = plugInInst;
            reset();
        }

        public String GetPage(String pPageName, String pParamString)
        {
            reset();
            PageName = pPageName;
            String pPageNameClean = pPageName;
            if (pPageNameClean.Contains("/"))
            {
                pPageNameClean = pPageName.Split('/')[1];
            }

            NameValueCollection parts = HttpUtility.ParseQueryString(pParamString);

            Type objType = GetType();
            MethodInfo handler = null;
            try
            {
                handler = objType.GetMethod("Page_" + pPageNameClean);
                if (handler == null)
                {
                    return "<h1>Page not found: " + pPageName + "</h1>";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception : " + e.ToString());
            }

            PageReturn page = handler.Invoke(this, new object[] { pPageName, pPageNameClean, parts }) as PageReturn;
            if (page.full_page)
            {
                return page.content;
            }
            else
            {
                AddHeader(hsHost.GetPageHeader(pPageName, pPageNameClean, "", "", false, true));
                AddBody(page.content);
                this.RefreshIntervalMilliSeconds = 10;
                suppressDefaultFooter = true;
                AddFooter(hsHost.GetPageFooter());
                return BuildPage();
            }
        }
        public String GetPage(String pPageName, NameValueCollection pParams)
        {
            return "TODO2";
        }
        public String PostBack(String pPageName, String pParamString, String pUser, int pUserRights)
        {
            reset();
            PageName = pPageName;
            String pPageNameClean = pPageName;
            if (pPageNameClean.Contains("/"))
            {
                pPageNameClean = pPageName.Split('/')[1];
            }

            NameValueCollection parts = HttpUtility.ParseQueryString(pParamString);

            Type objType = GetType();
            MethodInfo handler = null;
            try
            {
                handler = objType.GetMethod("PostHandler_" + pPageNameClean);
                if (handler == null)
                {
                    return "<h1>Page not found: " + pPageName + "</h1>";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception : " + e.ToString());
            }

            PageReturn page = handler.Invoke(this, new object[] { pPageName, pPageNameClean, parts }) as PageReturn;
            return postBackProc(pPageName, pParamString, pUser, pUserRights);
        }
    }
}


namespace HSPI_EnOcean
{
    class PageBuilder : PageBuilderBase
    {
        private HSPI_EnOcean.EnOceanManager mCore;
        public PageBuilder(IHSApplication pHS, IAppCallbackAPI pHSCB, HSPI_EnOcean.HSPI pluginInstance, EnOceanManager pCore)
            : base(pHS, pHSCB, pluginInstance)
        {
            mCore = pCore;
        }
        public PageReturn Page_HS3_EnOcean_Interfaces(String pPageName, String pCleanName, NameValueCollection pArgs)
        {
            var stb = new StringBuilder();

            string conf_node_id = pArgs.Get("configure_node");
            stb.Append(DivStart("pluginpage", ""));

            // Add message area for (ajax) errors
            stb.Append(DivStart("errormessage", "class='errormessage'"));
            stb.Append(DivEnd());

            stb.Append(DivEnd());

            var ifList = mCore.GetInterfaces();

            if (ifList.Count == 0)
            {
                stb.Append("<b>No interfaces added - please add interface below</b>");
            }
            else
            {
                stb.AppendLine("<table style=\"width: 400px\">");
                stb.AppendLine("<tr><th>Interface port</th><th>Status</th></tr>");
                foreach (var iface in ifList)
                {
                    stb.AppendLine("<tr><td>" + iface.getPortName() + "</td><td>" + iface.getControllerStatus() + "</td></tr>");
                }
                stb.AppendLine("</table>");
                // TODO: Show table with existing interfaces and status!
            }

            clsJQuery.jqButton ctrlBtnAddInterface  = new clsJQuery.jqButton("add_interface", "Add interface", pPageName, true);
//            var ctrlComPortList = new clsJQuery.jqDropList("com_selector", "Add interface", true);
            var ctrlComPortList = new clsJQuery.jqListBox("com_selector", "");
//            ctrlComPortList.items.Add("Test");
            stb.AppendLine(FormStart("addForm", pPageName, "POST"));
            stb.AppendLine("<select name=\"com_selector\">\n");
            foreach (var p in SerialPort.GetPortNames()) {
                var validPort = true;
//                ctrlComPortList.AddItem(p, p, false);
                //ctrlComPortList.items.Add(p);
                foreach (var i in ifList)
                {
                    if (i.getPortName() == p)
                        validPort = false;
                }
                if (validPort)
                    stb.AppendLine("\t<option value=\"" + p + "\">" + p + "</option>\n");
            }
            stb.AppendLine("</select>\n");
            stb.Append(ctrlComPortList.Build());
            stb.Append(ctrlBtnAddInterface.Build());
            stb.AppendLine(FormEnd());
            return new PageReturn(stb.ToString(), false);
        }

        public PageReturn PostHandler_HS3_EnOcean(String pPageName, String pCleanName, NameValueCollection pArgs)
        {
            var stb = new StringBuilder();
            return new PageReturn(""); ;
        }
        public PageReturn PostHandler_HS3_EnOcean_Interfaces(String pPageName, String pCleanName, NameValueCollection pArgs)
        {
            var stb = new StringBuilder();
            if (pArgs.Get("add_interface") != null)
            {
                String port = pArgs.Get("com_selector");
                Console.WriteLine("Adding interface: " + port);
                var ifList = mCore.GetInterfaces();
                foreach (var i in ifList)
                {
                    if (i.getPortName() == port)
                        return new PageReturn("ERROR - port exist");
                }

                var initCfg = new JObject();
                initCfg.Add("portname", port);
                var newCtrl = new EnOceanController(hsHost, hsHostCB, pluginInstance, initCfg);
                newCtrl.Initialize();
                newCtrl.SaveConfiguration();
                mCore.AddInterface(newCtrl);
                
            }
            return new PageReturn(RedirectPage(PageName));
        }
        public PageReturn Page_HS3_EnOcean(String pPageName, String pCleanName, NameValueCollection pArgs)
        {
            var stb = new StringBuilder();

            string conf_node_id = pArgs.Get("configure_node");
            stb.Append(DivStart("pluginpage", ""));

            // Add message area for (ajax) errors
            stb.Append(DivStart("errormessage", "class='errormessage'"));
            stb.Append(DivEnd());

            stb.Append(DivEnd());
            //AddBody(stb.ToString());
            if (conf_node_id != null)
            {
                stb.AppendLine(DivStart("configuration_"+conf_node_id, ""));
                stb.AppendLine("<h2>Configuration for node " + conf_node_id + "</h2>");
                clsJQuery.jqSelector ctrl = new clsJQuery.jqSelector("connector_authkey", "text", true);
                ctrl.AddItem("Type", "", true); 

                stb.AppendLine("TODO!");
                stb.Append(DivEnd());
                return new PageReturn(stb.ToString(), false);
            }
            stb.AppendLine(FormStart("cfgForm", "cfgForm", "POST"));
  //          clsJQuery.jqTextBox ctrlSourceName = new clsJQuery.jqTextBox("connector_name", "text", hsHost.GetINISetting("EnOcean", "name", "HS3 Connector") , pPageName, 64, true);
  //          clsJQuery.jqTextBox ctrlApiKey = new clsJQuery.jqTextBox("connector_authkey", "text", hsHost.GetINISetting("EnOcean", "authkey", "[please set me]"), pPageName, 64, true);
   //         clsJQuery.jqButton ctrlBtnTest  = new clsJQuery.jqButton("test_connection", "Check connection", pPageName, true);
            stb.AppendLine("<h2>Known EnOcean devices</h2>\n");
            stb.AppendLine("<table cellpadding=\"0\" cellspacing=\"0\" border=\"1\" style=\"width: 100%\">\n");
            stb.AppendLine("<tr><th>Node id</th><th>First seen</th><th>Configured</th><th>Actions</th></tr>");
            foreach (var iface in mCore.GetInterfaces())
            {
                foreach (JObject deviceInfo in iface.getSeenDevices().Values()) 
            {
                stb.AppendLine("<tr><td>" + deviceInfo["address"] + "</td><td>" + deviceInfo["first_seen"] + "</td><td>" + deviceInfo["configured"] + "</td>");
                stb.AppendLine("<td><a href=\"?configure_node="+deviceInfo["address"]+"\">Configure</a></td>");
                stb.Append("</tr>");
            }

            }
/*            foreach (JObject deviceInfo in mCore.getSeenDevices().Values())
            {
                stb.AppendLine("<tr><td>" + deviceInfo["address"] + "</td><td>" + deviceInfo["first_seen"] + "</td><td>" + deviceInfo["configured"] + "</td>");
                stb.AppendLine("<td><a href=\"?configure_node="+deviceInfo["address"]+"\">Configure</a></td>");
                stb.Append("</tr>");
            }*/
//            stb.Append("<tr><td>Connector name</td><td>"+ctrlSourceName.Build()+"</td></tr>\n");
//            stb.Append("<tr><td>Connector API Key</td><td>"+ctrlApiKey.Build()+"</td></tr>\n");
//            stb.Append("<tr><td>Status</td><td>"+mCore.GetStatus().ToString()+"</td></tr>\n");
            //stb.Append("<tr><td>&nbsp;</td><td>"+ctrlBtnTest.Build()+"</td></tr>\n");
            stb.AppendLine("</table>");
            stb.Append("<br/>");
            stb.AppendLine(FormEnd());
//            build_DeviceTypeCfgTable(stb);
            stb.AppendLine(DivEnd());

            return new PageReturn(stb.ToString(), false);
        }
    }
}
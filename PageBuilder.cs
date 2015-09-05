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
using HomeSeerAPI;
using Scheduler;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using EnOcean;

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
            if (page.full_page)
                return page.content;
            else
                return postBackProc(pPageName, pParamString, pUser, pUserRights);
        }
    }
}


namespace HSPI_EnOcean
{
    using EnOcean;
    class PageBuilder : PageBuilderBase
    {
        private EnOceanManager mCore;
        private IHSApplication HS;
        public PageBuilder(IHSApplication pHS, IAppCallbackAPI pHSCB, HSPI_EnOcean.HSPI pluginInstance, EnOceanManager pCore)
            : base(pHS, pHSCB, pluginInstance)
        {
            mCore = pCore;
            HS = pHS;
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
            stb.AppendLine("<h3>Existing controllers</h3>");
            stb.AppendLine("<table border=\"1\" style=\"width: 400px\" cellspacing=\"0\">");
            stb.AppendLine("<tr><th>Interface port</th><th>Status</th></tr>");
            int ifCount = 0;
            foreach (var iface in ifList)
            {
                stb.AppendLine("<tr><td>" + iface.getPortName() + "</td><td>" + iface.getControllerStatus() + "</td></tr>");
                ifCount++;
            }
            if (ifCount == 0)
                stb.AppendLine("<tr><td colspan=\"2\">No interfaces added.</td></tr>");
            stb.AppendLine("</table>");
            // TODO: Show table with existing interfaces and status!

            clsJQuery.jqButton ctrlBtnAddInterface = new clsJQuery.jqButton("add_interface", "Add interface", pPageName, true);
//            var ctrlComPortList = new clsJQuery.jqListBox("com_selector", "");
            stb.AppendLine(FormStart("addForm", pPageName, "POST"));
            stb.AppendLine("<h3>Add new controller instance</h3>");
            stb.AppendLine("<table cellspacing=\"0\">");
            stb.AppendLine("<tr><td>");
            stb.AppendLine("<input type=\"text\" name=\"name\" value=\"Primary Controller\">");
            stb.AppendLine("</td><td>");
            stb.AppendLine("<select name=\"com_selector\">\n");
            foreach (var p in SerialPort.GetPortNames())
            {
                var validPort = true;
                foreach (var i in ifList)
                {
                    if (i.getPortName() == p)
                        validPort = false;
                }
                if (validPort)
                    stb.AppendLine("\t<option value=\"" + p + "\">" + p + "</option>\n");
            }
            stb.AppendLine("</select>\n");
            stb.AppendLine("</td></tr>");
  //          stb.Append(ctrlComPortList.Build());
            stb.AppendLine("<tr><td>&nbsp;</td><td>");
            stb.Append("<input type=\"submit\" name=\"add_interface\" value=\"Add\">");
            stb.AppendLine("</td></tr>");
            stb.AppendLine("</table>");
            stb.AppendLine(FormEnd());
            stb.AppendLine("<br/>");
            return new PageReturn(stb.ToString(), false);
        }

        public PageReturn PostHandler_HS3_EnOcean(String pPageName, String pCleanName, NameValueCollection pArgs)
        {
            var node_id = pArgs.Get("configure_node");
            var controller_id = pArgs.Get("controller_id");
            var node_type = pArgs.Get("device_profile");
            var node_name = pArgs.Get("node_name");
            var ctrl = mCore.GetInterfaceById(controller_id);
            var newConfig = new JObject();
            newConfig["node_name"] = node_name;
            DeviceTypes.CreateDeviceInstance(HS, ctrl, node_id, node_type, newConfig);
            if (ctrl.getSeenDevices().ContainsKey(node_id))
                ctrl.getSeenDevices().Remove(node_id);
            return new PageReturn("<script>window.location='" + pPageName + "';</script>\n", true);
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
                if (newCtrl.Initialize())
                {
                    newCtrl.SaveConfiguration();
                    mCore.AddInterface(newCtrl);
                }
                else
                {
                    Console.WriteLine("Error adding interface: could not get id");
                    newCtrl.Close();
                }

            }
            return new PageReturn("<script>window.location='" + pPageName + "';</script>\n", true);
        }
        public PageReturn Page_HS3_EnOcean(String pPageName, String pCleanName, NameValueCollection pArgs)
        {
            var stb = new StringBuilder();

            string conf_node_id = pArgs.Get("configure_node");
            string conf_controller_id = pArgs.Get("controller_id");
            stb.Append(DivStart("pluginpage", ""));

            // Add message area for (ajax) errors
            stb.Append(DivStart("errormessage", "class='errormessage'"));
            stb.Append(DivEnd());

            stb.Append(DivEnd());
            if (conf_node_id != null)
            {
                stb.AppendLine(DivStart("configuration_" + conf_node_id, ""));
                stb.AppendLine("<h2>Configuration for node " + conf_node_id + "</h2>");
                stb.AppendLine("<form name=\"cfgForm\" method=\"post\" action=\"" + pPageName + "\">");
                stb.AppendLine("<input type=\"hidden\" name=\"controller_id\" value=\"" + conf_controller_id + "\">");
                stb.AppendLine("<input type=\"hidden\" name=\"configure_node\" value=\"" + conf_node_id + "\">");
                stb.AppendLine("<table>");
                stb.AppendLine("<tr><td>");
                stb.AppendLine("Please give device a name: ");
                stb.AppendLine("</td><td>");
                stb.AppendLine("<input type=\"text\" name=\"node_name\" value=\"" + conf_node_id + "\">");
                stb.AppendLine("</td></tr>");
                stb.AppendLine("<tr><td>");
                stb.AppendLine("Please select a device profile: ");
                stb.AppendLine("</td><td>");
                stb.AppendLine("<select name=\"device_profile\" >");
                foreach (var pType in Enum.GetValues(typeof(EnOcean.EDeviceTypes)))
                {
                    if ((int)pType == (int)EnOcean.EDeviceTypes.UNKNOWN)
                        continue;
                    stb.AppendLine("<option value=\"" + (int)pType + "\">" + pType.ToString() + "</option>");
                }
                stb.AppendLine("</select>");
                stb.AppendLine("</td></tr>");
                stb.AppendLine("<tr><td>&nbsp;</td><td>");
                stb.AppendLine("<input type=\"submit\" name=\"Save\">");
                stb.AppendLine("</td></tr>");
                stb.AppendLine("</table>");
                stb.AppendLine("</form>");
                stb.Append(DivEnd());
                stb.AppendLine("<br/>");
                return new PageReturn(stb.ToString(), false);
            }
            stb.AppendLine("<form name=\"cfgForm\" method=\"POST\">");

            stb.AppendLine("<h2>Unconfigured EnOcean devices</h2>\n");
            stb.AppendLine("<table cellpadding=\"0\" cellspacing=\"0\" border=\"1\" style=\"width: 100%\">\n");
            stb.AppendLine("<tr><th>Node id</th><th>First seen</th><th>Configured</th><th>Actions</th></tr>");
            foreach (var iface in mCore.GetInterfaces())
            {
                foreach (JObject deviceInfo in iface.getSeenDevices().Values)
                {
                    stb.AppendLine("<tr><td>" + deviceInfo["address"] + "</td><td>" + deviceInfo["first_seen"] + "</td><td>" + deviceInfo["configured"] + "</td>");
                    stb.AppendLine("<td><a href=\"?configure_node=" + deviceInfo["address"] + "&controller_id=" + iface.ControllerId + "\">Configure</a></td>");
                    stb.Append("</tr>");
                }

            }
            stb.AppendLine("</table>");
            stb.Append("<br/>");
            stb.AppendLine("</form>");
            stb.AppendLine(DivEnd());

            return new PageReturn(stb.ToString(), false);
        }
    }
}
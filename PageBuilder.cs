using System;
using System.IO;
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
        //		private StringBuilder stb;

        public PageBuilderBase(IHSApplication pHS, IAppCallbackAPI pHSCB)
            : base("dummy")
        {
            hsHost = pHS;
            hsHostCB = pHSCB;
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
        private HSPI_EnOcean.EnOceanController mCore;
        public PageBuilder(IHSApplication pHS, IAppCallbackAPI pHSCB, EnOceanController pCore)
            : base(pHS, pHSCB)
        {
            mCore = pCore;
        }
        public PageReturn PostHandler_HS3_EnOcean(String pPageName, String pCleanName, NameValueCollection pArgs)
        {
            var stb = new StringBuilder();
            return new PageReturn("");
        }
        public PageReturn Page_HS3_EnOcean(String pPageName, String pCleanName, NameValueCollection pArgs)
        {
            var stb = new StringBuilder();

            stb.Append(DivStart("pluginpage", ""));

            // Add message area for (ajax) errors
            stb.Append(DivStart("errormessage", "class='errormessage'"));
            stb.Append(DivEnd());

            stb.Append(DivEnd());
            //AddBody(stb.ToString());
            stb.Append(DivStart("configuration", ""));
            stb.Append(FormStart("cfgForm", "cfgForm", "POST"));
  //          clsJQuery.jqTextBox ctrlSourceName = new clsJQuery.jqTextBox("connector_name", "text", hsHost.GetINISetting("EnOcean", "name", "HS3 Connector") , pPageName, 64, true);
  //          clsJQuery.jqTextBox ctrlApiKey = new clsJQuery.jqTextBox("connector_authkey", "text", hsHost.GetINISetting("EnOcean", "authkey", "[please set me]"), pPageName, 64, true);
   //         clsJQuery.jqButton ctrlBtnTest  = new clsJQuery.jqButton("test_connection", "Check connection", pPageName, true);
            stb.Append("<table cellpadding=\"0\" cellspacing=\"0\">\n");
//            stb.Append("<tr><td>Connector name</td><td>"+ctrlSourceName.Build()+"</td></tr>\n");
//            stb.Append("<tr><td>Connector API Key</td><td>"+ctrlApiKey.Build()+"</td></tr>\n");
//            stb.Append("<tr><td>Status</td><td>"+mCore.GetStatus().ToString()+"</td></tr>\n");
            //stb.Append("<tr><td>&nbsp;</td><td>"+ctrlBtnTest.Build()+"</td></tr>\n");
            stb.Append("</table>");
            
            stb.Append(FormEnd());
//            build_DeviceTypeCfgTable(stb);
            stb.Append(DivEnd());

            return new PageReturn(stb.ToString(), false);
        }
    }
}
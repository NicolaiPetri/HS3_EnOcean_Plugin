﻿using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;

using HomeSeerAPI;
using HSCF.Communication.Scs.Communication;
using HSCF.Communication.Scs.Communication.EndPoints.Tcp;
using HSCF.Communication.ScsServices.Client;
using HSCF.Communication.ScsServices.Service;

namespace HSPI_EnOcean
{
    using EnOcean;
    public class Manager : IDisposable
    {
        IScsServiceClient<IHSApplication> client;
        IScsServiceClient<IAppCallbackAPI> clientCB;
        IHSApplication hsHost;
        IAppCallbackAPI hsHostCB;

        HSPI pluginInst;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                pluginInst.Dispose();
                pluginInst = null;
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void run()
        {
            string[] cmdArgs = Environment.GetCommandLineArgs();
            Console.WriteLine("Manager::run() - arguments are {0}", Environment.CommandLine);
            String paramServer = "127.0.0.1";
            String paramInstance = "";
            foreach (string arg in cmdArgs)
            {
                Console.WriteLine(" - arg: {0}", arg);
                if (arg.Contains("="))
                {
                    String[] ArgS = arg.Split('=');
                    Console.WriteLine(" -- {0}=>{1}", ArgS[0], ArgS[1]);
                    switch (ArgS[0])
                    {
                        case "server":
                            paramServer = ArgS[1];
                            break;
                        case "instance":
                            paramInstance = ArgS[1];
                            break;
                        default:
                            Console.WriteLine("Unhandled param: {0}", ArgS[0]);
                            break;

                    }
                }
            }
            pluginInst = new HSPI(paramInstance);

            //Environment.CommandLine.
            client = ScsServiceClientBuilder.CreateClient<IHSApplication>(new ScsTcpEndPoint(paramServer, 10400), pluginInst);
            clientCB = ScsServiceClientBuilder.CreateClient<IAppCallbackAPI>(new ScsTcpEndPoint(paramServer, 10400), pluginInst);

            try
            {
                client.Connect();
                clientCB.Connect();
                hsHost = client.ServiceProxy;
                double ApiVer = hsHost.APIVersion;
                Console.WriteLine("Host ApiVersion : {0}", ApiVer);
                hsHostCB = clientCB.ServiceProxy;
                ApiVer = hsHostCB.APIVersion;
                Console.WriteLine("Host CB ApiVersion : {0}", ApiVer);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot start instance because of : {0}", e.Message);
                return;
            }
            Console.WriteLine("Connection to HS succeeded!");
            try
            {
                pluginInst.hsHost = hsHost;
                pluginInst.hsHostCB = hsHostCB;
                hsHost.Connect(pluginInst.Name, "");
                Console.WriteLine("Connected, waiting to be initialized...");
                do
                {
                    Thread.Sleep(500);
                } while (client.CommunicationState == CommunicationStates.Connected && pluginInst.Running);

                Console.WriteLine("Connection lost, exiting");
                pluginInst.Running = false;

                client.Disconnect();
                clientCB.Disconnect();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to host connect: {0}", e.Message);
                return;
            }

            Console.WriteLine("Exiting!!!");
        }
    }
    class Program
    {

        static private List<String> _moduleDirectories;// = new List<string>();
        static private System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Console.WriteLine("dirs have {0} entries", _moduleDirectories.Count);
            Console.WriteLine("Asked to find assembly : |{0}|", args.Name.Split(',')[0]);
            foreach (var moduleDir in _moduleDirectories)
            {
                try
                {
                    return System.Reflection.Assembly.LoadFrom(moduleDir+"/"+args.Name.Split(',')[0]+".dll");
                    var di = new System.IO.DirectoryInfo(moduleDir);
                    Console.WriteLine("Testing ({0} ({1})", di.FullName, moduleDir);
                    var module = di.GetFiles().FirstOrDefault(i => i.Name == (args.Name.Split(',')[0]) + ".dll");
                    if (module != null)
                    {
                        Console.WriteLine("Found it at {0}", di.FullName);
                        return System.Reflection.Assembly.LoadFrom(module.FullName);
                    }
                }
                catch (Exception e)
                {
                   Console.WriteLine("dll load error: {0}", e);
                }
            }
            Console.WriteLine(" - error locating assembly");
            return null;
        }


        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            Console.WriteLine("Trying to add {0} to include path!", Environment.CurrentDirectory + "/Bin/HS3_EnOcean");
            _moduleDirectories = new List<string>();
            //_moduleDirectories.Add("bin");
            //_moduleDirectories.Add(Environment.CurrentDirectory);
            _moduleDirectories.Add("Bin/HS3_EnOcean");
            _moduleDirectories.Add(Environment.CurrentDirectory + "/Bin/HS3_EnOcean");
            String binPath = Environment.CurrentDirectory + "/bin";
            _moduleDirectories.Add(binPath);
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            
//            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + binPath);
            Manager m = new Manager();
            m.run();
        }
    }
}

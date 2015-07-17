using System;
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
    public class Manager
    {
        IScsServiceClient<IHSApplication> client;
        IScsServiceClient<IAppCallbackAPI> clientCB;
        IHSApplication hsHost;
        IAppCallbackAPI hsHostCB;

        HSPI pluginInst;
        public void run()
        {
            EnOceanFrameLayer hest = new EnOceanFrameLayer();
            hest.Open("com23");
            
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
            //                    client = ScsServiceClientBuilder.CreateClient(Of IHSApplication)(New ScsTcpEndPoint(sIp, 10400), gAppAPI)
            //      clientCallback = ScsServiceClientBuilder.CreateClient(Of IAppCallbackAPI)(New ScsTcpEndPoint(sIp, 10400), gAppAPI)

            try
            {
                client.Connect();
//                Thread.Sleep(5000);
                clientCB.Connect();
             //   Thread.Sleep(5000);
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
                    //Console.Write("'");
                    Thread.Sleep(1000);
                } while (client.CommunicationState == CommunicationStates.Connected && pluginInst.Running);
                //Loop While client.CommunicationState = HSCF.Communication.Scs.Communication.CommunicationStates.Connected And Not HSPI.bShutDown
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
            Console.WriteLine("Asked to find assembly : |{0}|", args.Name.Split(',')[0]);
            foreach (var moduleDir in _moduleDirectories)
            {
                var di = new System.IO.DirectoryInfo(moduleDir);
                var module = di.GetFiles().FirstOrDefault(i => i.Name == (args.Name.Split(',')[0]) + ".dll");
                //Console.WriteLine("Checking {0} which gave {1}", moduleDir, module);
                if (module != null)
                {
                    return System.Reflection.Assembly.LoadFrom(module.FullName);
                }
            }
            return null;
        }


        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            //CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            //CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
            
            // Try to be nice about finding sqlite interop.dll
            String binPath = Environment.CurrentDirectory + "/bin";
            Console.WriteLine("Add Homeseer bin path to include lists: {0}", binPath);
            _moduleDirectories = new List<string>();
            _moduleDirectories.Add("bin");
            _moduleDirectories.Add(Environment.CurrentDirectory);
            _moduleDirectories.Add(binPath);
            _moduleDirectories.Add(Environment.CurrentDirectory + "/refs");
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            //System.AppDomain.pr
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + binPath);
            Manager m = new Manager();
            m.run();
            //            Console.WriteLine("This plugin cannot be instantiated directly.. please start from homeseer\n");
            //          Console.ReadLine();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using ConsoleApp1.Model;
namespace Quickstarts.ConsoleReferenceClient

{
    /// <summary>
    /// The program.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main entry point.
        /// </summary>
        public static async Task Main(string[] args)
        {
            TextWriter output = Console.Out;
            output.WriteLine("OPC UA Console Reference Client");

            output.WriteLine("OPC UA library: {0} @ {1} -- {2}",
                Utils.GetAssemblyBuildNumber(),
                Utils.GetAssemblyTimestamp().ToString("G", CultureInfo.InvariantCulture),
                Utils.GetAssemblySoftwareVersion());

            // The application name and config file names
            var applicationName = "ConsoleReferenceClient";
            var configSectionName = "Quickstarts.ReferenceClient";
            var usage = $"Usage: dotnet {applicationName}.dll [OPTIONS]";

            // command line options
            bool showHelp = false;
            bool autoAccept = true;
            string username = null;
            string userpassword = null;
            bool logConsole = false;
            bool appLog = false;
            bool renewCertificate = false;
            bool loadTypes = false;
            bool browseall = false;
            bool fetchall = false;
            bool jsonvalues = false;
            bool verbose = false;
            bool subscribe = false;
            bool noSecurity = true;
            string password = null;
            int timeout = Timeout.Infinite;
            string logFile = null;
            string reverseConnectUrlString = null;
            bool test = true;
            List<Variable> vars = new List<Variable>() {
            new Variable("test1", "ns=2;s=s7-1200.PLS.1"),
            new Variable("test2", "ns=2;s=s7-1200.PLS.2"),
            new Variable("test3", "ns=2;s=s7-1200.PLS.3"),
            new Variable("test4", "ns=2;s=s7-1200.PLS.4"),
             new Variable("test4", "ns=2;s=s7-1200.PLS.5"),
             new Variable("test4", "ns=2;s=s7-1200.PLS.6"),
             new Variable("test4", "ns=2;s=s7-1200.PLS.7"),
             new Variable("test4", "ns=2;s=s7-1200.PLS.8"),
             new Variable("test4", "ns=2;s=s7-1200.PLS.9"),
             new Variable("test4", "ns=2;s=s7-1200.PLS.10"),
           new Variable("test1", "ns=2;s=s7-1200.PLS.1"),
            new Variable("test2", "ns=2;s=s7-1200.PLS.2"),
            new Variable("test3", "ns=2;s=s7-1200.PLS.3"),
            new Variable("test4", "ns=2;s=s7-1200.PLS.4"),
             new Variable("test4", "ns=2;s=s7-1200.PLS.5"),
             new Variable("test4", "ns=2;s=s7-1200.PLS.6"),
             new Variable("test4", "ns=2;s=s7-1200.PLS.7"),
             new Variable("test4", "ns=2;s=s7-1200.PLS.8"),
             new Variable("test4", "ns=2;s=s7-1200.PLS.9"),
             new Variable("test4", "ns=2;s=s7-1200.PLS.10")
        };
      

        Mono.Options.OptionSet options = new Mono.Options.OptionSet {
                usage,
                { "h|help", "show this message and exit", h => showHelp = h != null },
                { "a|autoaccept", "auto accept certificates (for testing only)", a => autoAccept = a != null },
                { "nsec|nosecurity", "select endpoint with security NONE, least secure if unavailable", s => noSecurity = s != null },
                { "un|username=", "the name of the user identity for the connection", (string u) => username = u },
                { "up|userpassword=", "the password of the user identity for the connection", (string u) => userpassword = u },
                { "c|console", "log to console", c => logConsole = c != null },
                { "l|log", "log app output", c => appLog = c != null },
                { "p|password=", "optional password for private key", (string p) => password = p },
                { "r|renew", "renew application certificate", r => renewCertificate = r != null },
                { "t|timeout=", "timeout in seconds to exit application", (int t) => timeout = t * 1000 },
                { "logfile=", "custom file name for log output", l => { if (l != null) { logFile = l; } } },
                { "lt|loadtypes", "Load custom types", lt => { if (lt != null) loadTypes = true; } },
                { "b|browseall", "Browse all references", b => { if (b != null) browseall = true; } },
                { "f|fetchall", "Fetch all nodes", f => { if (f != null) fetchall = true; } },
                { "j|json", "Output all Values as JSON", j => { if (j != null) jsonvalues = true; } },
                { "v|verbose", "Verbose output", v => { if (v != null) verbose = true; } },
                { "s|subscribe", "Subscribe", s => { if (s != null) subscribe = true; } },
                { "rc|reverseconnect=", "Connect using the reverse connect endpoint. (e.g. rc=opc.tcp://localhost:65300)", (string url) => reverseConnectUrlString = url},
            };

            ReverseConnectManager reverseConnectManager = null;

            try
            {
                // parse command line and set options
                var extraArg = ConsoleUtils.ProcessCommandLine(output, args, options, ref showHelp, "REFCLIENT", false);

                // connect Url?
                Uri serverUrl = new Uri("opc.tcp://172.16.3.200:49320");
                if (!string.IsNullOrEmpty(extraArg))
                {
                    serverUrl = new Uri(extraArg);
                }

                // log console output to logger
                if (logConsole && appLog)
                {
                    output = new LogWriter();
                }

                // Define the UA Client application
                ApplicationInstance.MessageDlg = new ApplicationMessageDlg(output);
                CertificatePasswordProvider PasswordProvider = new CertificatePasswordProvider(password);
                ApplicationInstance application = new ApplicationInstance
                {
                    ApplicationName = applicationName,
                    ApplicationType = ApplicationType.Client,
                    ConfigSectionName = configSectionName,
                    CertificatePasswordProvider = PasswordProvider
                };

                // load the application configuration.
                var config = await application.LoadApplicationConfiguration(silent: false).ConfigureAwait(false);

                // override logfile
                if (logFile != null)
                {
                    var logFilePath = config.TraceConfiguration.OutputFilePath;
                    var filename = Path.GetFileNameWithoutExtension(logFilePath);
                    config.TraceConfiguration.OutputFilePath = logFilePath.Replace(filename, logFile);
                    config.TraceConfiguration.DeleteOnLoad = true;
                    config.TraceConfiguration.ApplySettings();
                }

                // setup the logging
                ConsoleUtils.ConfigureLogging(config, applicationName, logConsole, LogLevel.Information);

                // delete old certificate
                if (renewCertificate)
                {
                    await application.DeleteApplicationInstanceCertificate().ConfigureAwait(false);
                }

                // check the application certificate.
                bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, minimumKeySize: 0).ConfigureAwait(false);
                if (!haveAppCertificate)
                {
                    throw new ErrorExitException("Application instance certificate invalid!", ExitCode.ErrorCertificate);
                }

                if (reverseConnectUrlString != null)
                {
                    // start the reverse connection manager
                    output.WriteLine("Create reverse connection endpoint at {0}.", reverseConnectUrlString);
                    reverseConnectManager = new ReverseConnectManager();
                    reverseConnectManager.AddEndpoint(new Uri(reverseConnectUrlString));
                    reverseConnectManager.StartService(config);
                }

                // wait for timeout or Ctrl-C
                var quitCTS = new CancellationTokenSource();
                var quitEvent = ConsoleUtils.CtrlCHandler(quitCTS);

                // connect to a server until application stops
                bool quit = false;
                DateTime start = DateTime.UtcNow;
                int waitTime = int.MaxValue;
                do
                {
                    test = true;
                    if (timeout > 0)
                    {
                        waitTime = timeout - (int)DateTime.UtcNow.Subtract(start).TotalMilliseconds;
                        if (waitTime <= 0)
                        {
                            break;
                        }
                    }

                    // create the UA Client object and connect to configured server.

                    using (UAClient uaClient = new UAClient(application.ApplicationConfiguration, reverseConnectManager, output, ClientBase.ValidateResponse)
                    {
                        AutoAccept = autoAccept,
                        SessionLifeTime = 60_000,
                    })
                    {
                        // set user identity
                        if (!String.IsNullOrEmpty(username))
                        {
                            uaClient.UserIdentity = new UserIdentity(username, userpassword ?? string.Empty);
                        }

                        bool connected = await uaClient.ConnectAsync(serverUrl.ToString(), !noSecurity, quitCTS.Token).ConfigureAwait(false);
                        if (connected)
                        {
                            test = true;
                        }
                        while (test && connected)
                        {
                            output.WriteLine("Connected! Ctrl-C to quit.");

                            // enable subscription transfer
                            uaClient.ReconnectPeriod = 100;
                            uaClient.ReconnectPeriodExponentialBackoff = 10000;
                           // uaClient.Session.MinPublishRequestCount = 3;
                            uaClient.Session.TransferSubscriptionsOnReconnect = true;
                            var samples = new ClientSamples(output, ClientBase.ValidateResponse, quitEvent, verbose);
                            if (loadTypes)
                            {
                                await samples.LoadTypeSystem(uaClient.Session).ConfigureAwait(false);
                            }

                            if (browseall || fetchall || jsonvalues)
                            {
                                NodeIdCollection variableIds = null;
                                ReferenceDescriptionCollection referenceDescriptions = null;
                                if (browseall)
                                {
                                    referenceDescriptions =
                                        samples.BrowseFullAddressSpace(uaClient, Objects.RootFolder);
                                    variableIds = new NodeIdCollection(referenceDescriptions
                                        .Where(r => r.NodeClass == NodeClass.Variable && r.TypeDefinition.NamespaceIndex != 0)
                                        .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, uaClient.Session.NamespaceUris)));
                                }

                                IList<INode> allNodes = null;
                                if (fetchall)
                                {
                                    allNodes = samples.FetchAllNodesNodeCache(
                                        uaClient, Objects.RootFolder, true, true, false);
                                    variableIds = new NodeIdCollection(allNodes
                                        .Where(r => r.NodeClass == NodeClass.Variable && r is VariableNode && ((VariableNode)r).DataType.NamespaceIndex != 0)
                                        .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, uaClient.Session.NamespaceUris)));
                                }

                                if (jsonvalues && variableIds != null)
                                {
                                    await samples.ReadAllValuesAsync(uaClient, variableIds).ConfigureAwait(false);
                                }

                                if (subscribe && (browseall || fetchall))
                                {
                                    // subscribe to 100 random variables
                                    const int MaxVariables = 100;
                                    NodeCollection variables = new NodeCollection();
                                    Random random = new Random(62541);
                                    if (fetchall)
                                    {
                                        variables.AddRange(allNodes
                                            .Where(r => r.NodeClass == NodeClass.Variable && r.NodeId.NamespaceIndex > 1)
                                            .Select(r => ((VariableNode)r))
                                            .OrderBy(o => random.Next())
                                            .Take(MaxVariables));
                                    }
                                    else if (browseall)
                                    {
                                        var variableReferences = referenceDescriptions
                                            .Where(r => r.NodeClass == NodeClass.Variable && r.NodeId.NamespaceIndex > 1)
                                            .Select(r => r.NodeId)
                                            .OrderBy(o => random.Next())
                                            .Take(MaxVariables)
                                            .ToList();
                                        variables.AddRange(uaClient.Session.NodeCache.Find(variableReferences).Cast<Node>());
                                    }

                                    await samples.SubscribeAllValuesAsync(uaClient,
                                        variableIds: new NodeCollection(variables),
                                        samplingInterval: 1000,
                                        publishingInterval: 5000,
                                        queueSize: 10,
                                        lifetimeCount: 12,
                                        keepAliveCount: 2).ConfigureAwait(false);

                                    // Wait for DataChange notifications from MonitoredItems
                                    output.WriteLine("Subscribed to {0} variables. Press Ctrl-C to exit.", MaxVariables);
                                    quit = quitEvent.WaitOne(timeout > 0 ? waitTime : Timeout.Infinite);
                                }
                                else
                                {
                                    quit = true;
                                }
                            }
                            else
                            {
                                // Run tests for available methods on reference server.

                                test= samples.ReadNodes(uaClient.Session,vars);
                                    //  samples.WriteNodes(uaClient.Session);
                                  //    samples.Browse(uaClient.Session);
                                    //  samples.CallMethod(uaClient.Session);
                                    // samples.SubscribeToDataChanges(uaClient.Session, 10_000);
                                    
                              
                                output.WriteLine("Waiting...");

                                // Wait for some DataChange notifications from MonitoredItems
                               // quit = quitEvent.WaitOne(timeout > 0 ? waitTime : 1_000);
                            }

                            // output.WriteLine("Client disconnected.");

                            //  uaClient.Disconnect();
                        
                        }
                        output.WriteLine("Could not connect to server! Retry in 10 seconds or Ctrl-C to quit.");
                        quit = quitEvent.WaitOne(Math.Min(10000, waitTime));
                    }
                   

                } while (!quit);

                output.WriteLine("Client stopped.");
            }
            catch (Exception ex)
            {
                output.WriteLine(ex.Message);
            }
            finally
            {
                Utils.SilentDispose(reverseConnectManager);
               output.Close();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTANetworkServer;
using Marten;
using GTAIdentity.Models;
using GTAIdentity.Modules;
using Newtonsoft.Json;
using AutoMapper;
using GTANetworkShared;
using Common.Logging;
using Common.Logging.Configuration;

namespace GTAIdentity
{
    /// <summary>
    /// The main entry point to our script. 
    /// </summary>
    public sealed partial class ScriptMain : Script
    {

        private IDocumentStore Store { get; }
        private IdentityModule Identity { get; }
        private AdminModule AdminModule { get; }
        private CrimeModule CrimeModule { get; }
        private ILog Log { get; }

        public ScriptMain()
        {
            API.onResourceStart += API_onResourceStart;
            API.onResourceStop += API_onResourceStop;

            SetLogging();
            Log = LogManager.GetLogger<ScriptMain>();

            // You should change the connection string property of the store configurator object.
            Store = new DevelopmentStoreConfigurator().GetStore();
            Identity = new IdentityModule(Store, API)
            {
                ClientRestrictions = ClientRestrictions.RequireAccountLogin
            };
            AdminModule = new AdminModule(Store, Identity, API);
            CrimeModule = new CrimeModule(API, Identity, Store);

            Log.Info($"ScriptMain finished initializing dependencies.");
        }

        private void API_onResourceStart()
        {
        }

        private void API_onResourceStop()
        {
            Store.Dispose();
        }

        private void SetLogging()
        {
            var properties = new NameValueCollection();
            properties["showDateTime"] = "true";
            LogManager.Adapter = new Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter(properties);
        }
    }
}

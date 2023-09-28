using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;

namespace IntuneGroupAssignments
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        static App()
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            Models.AppSettings? settings = config.GetRequiredSection("settings").Get<Models.AppSettings>();

            //var ClientID = settings.clientId;
            //var Tenent = settings.tenantId;
            if (settings.clientId.Length > 0)
            {
                _clientApp = PublicClientApplicationBuilder.Create(settings.clientId)
                    .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
                    .WithAuthority(AzureCloudInstance.AzurePublic, settings.tenantId)
                    .WithDefaultRedirectUri()
                    .Build();
            }

        }

        private static IPublicClientApplication _clientApp;

        public static IPublicClientApplication PublicClientApp { get { return _clientApp; } }
    }
}

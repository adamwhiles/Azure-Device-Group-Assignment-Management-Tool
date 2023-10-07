using Microsoft.Data.OData.Query;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Graph;
using Microsoft.Graph.Beta;
using System.Net.Http.Headers;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Net.Http;
using Tavis.UriTemplates;
using Prompt = Microsoft.Identity.Client.Prompt;
using IntuneGroupAssignments.Models;
using Application = IntuneGroupAssignments.Models.Application;
using System.Diagnostics.PerformanceData;
using Microsoft.Graph.InformationProtection.ThreatAssessmentRequests.Item.Results;
using Microsoft.Web.WebView2.Core;
using System.ComponentModel;
using System.Windows.Threading;
using System.Text.Json.Nodes;
using Azure.Core;
using Microsoft.Graph.Models.ExternalConnectors;
using Microsoft.Graph.Models.TermStore;
using Microsoft.Identity.Client.NativeInterop;
using System.Windows.Input.Manipulations;
using Configuration = IntuneGroupAssignments.Models.Configuration;
using Microsoft.Graph.Beta.DeviceManagement.Reports.GetCompliancePoliciesReportForDevice;
using Microsoft.Graph.Beta.Models;
using System.Windows.Interop;

namespace IntuneGroupAssignments
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        //Set the scope for API call to user.read
        string[] scopes = new string[] { "user.read", "group.read.all", "groupmember.read.all", "devicemanagementapps.read.all", "devicemanagementconfiguration.read.all" };
        List<AZDevice> devices = new List<AZDevice>();
        List<Application> applications = new List<Application>();
        List<Configuration> configurations = new List<Configuration>();
        List<Remediation> remediations = new List<Remediation>();
        List<Script> scripts = new List<Script>();
        List<Policy> policies = new List<Policy>();
        public MainWindow()
        {
            InitializeComponent();
        }
        

        private async Task<Microsoft.Graph.Models.Group> GetGroupInfo(HttpClient client, String sGroup)
        {
            devices.Clear();
           
            var graphClient = new Microsoft.Graph.GraphServiceClient(client);
            var group = await graphClient.Groups.GetAsync((requestConfiguration) =>
            {
                requestConfiguration.QueryParameters.Filter = $"(displayName eq '{sGroup}')";
                requestConfiguration.QueryParameters.Select = new string[] { "id", "displayName", "groupTypes", "membershipRule", "members" };
            });
            if (group.Value.Count > 0)
            {
                var groupMembers = await graphClient.Groups[$"{group.Value[0].Id}"].Members.GetAsync();
                if (groupMembers.Value.Count > 0)
                {
                    foreach (var item in groupMembers.Value)
                    {
                        if (item.OdataType == "#microsoft.graph.device")
                        {
                            AZDevice device = new AZDevice();
                            device.DisplayName = ((Microsoft.Graph.Models.Device)item).DisplayName;
                            devices.Add(device);
                        }

                    }
                }
                return group.Value[0];
            } else
            {
                throw new Exception();
            }
            
            
        }
        private async Task<List<Application>> GetApplications_v2(HttpClient client)
        {
            var graphClient = new Microsoft.Graph.GraphServiceClient(client);
            var apps = await graphClient.DeviceAppManagement.MobileApps.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select =
                    new string[] { "Id", "displayName", "LastModifiedDateTime" };
            });
            var tasks = apps.Value.Select(async a =>
            {
                Application App = new Application();
                App.Id = a.Id;
                App.DisplayName = a.DisplayName;
                App.ModifiedDate = DateTime.ParseExact(a.LastModifiedDateTime.ToString(), "M/d/yyyy h:mm:ss tt zzz", System.Globalization.CultureInfo.InvariantCulture).ToString();


                var appAssignments = await graphClient.DeviceAppManagement.MobileApps[$"{a.Id}"].Assignments.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select =
                        new string[] { "Id", "Target", "Intent" };
                });

                foreach (var item in appAssignments.Value)
                {

                    if (item.Target is Microsoft.Graph.Models.GroupAssignmentTarget groupAssignment)
                    {
                        Assignment assignment = new Assignment();
                        App.Intent = item.Intent.Value.ToString();
                        assignment.GroupID = groupAssignment.GroupId;
                        App.Assignments.Add(assignment);
                    }
                }

                applications.Add(App);
            });
            await Task.WhenAll(tasks);
            return applications;
        }

        private async Task GetRemediations(HttpClient client)
        {
            var graphClient = new Microsoft.Graph.GraphServiceClient(client);
            var betaClient = new Microsoft.Graph.Beta.GraphServiceClient(client);

            var rems = await betaClient.DeviceManagement.DeviceHealthScripts.GetAsync((requestConfiguration) =>
            {
                requestConfiguration.QueryParameters.Expand = new string[] { "assignments" };
            });
            var tasks = rems.Value.Select(async (rem) =>
            {
                Remediation remediation = new Remediation();
                remediation.Id = rem.Id;
                remediation.DisplayName = rem.DisplayName;
                remediation.ModifiedDate = DateTime.ParseExact(rem.LastModifiedDateTime.ToString(), "M/d/yyyy h:mm:ss tt zzz", System.Globalization.CultureInfo.InvariantCulture).ToString();

                if (rem.Assignments != null)
                {
                    foreach (var item in rem.Assignments)
                    {
                        Assignment assignment = new Assignment();
                        assignment.GroupID = item.Id.Substring(item.Id.IndexOf(':') + 1);
                        remediation.Assignments.Add(assignment);
                    }
                }
                remediations.Add(remediation);
            });
            await Task.WhenAll(tasks);
        }

        private async Task GetPolicies(HttpClient client)
        {
            var graphClient = new Microsoft.Graph.GraphServiceClient(client);
            var betaClient = new Microsoft.Graph.Beta.GraphServiceClient(client);

            var devicePolicies = await betaClient.DeviceManagement.DeviceCompliancePolicies.GetAsync();

            foreach (var a in devicePolicies.Value)
            {
                Policy Policy = new Policy();
                Policy.Id = a.Id;
                Policy.DisplayName = a.DisplayName;
                Policy.ModifiedDate = DateTime.ParseExact(a.LastModifiedDateTime.ToString(), "M/d/yyyy h:mm:ss tt zzz", System.Globalization.CultureInfo.InvariantCulture).ToString();


                var policyAssignments = await betaClient.DeviceManagement.DeviceCompliancePolicies[$"{(a.Id)}"].Assignments.GetAsync();
                if (policyAssignments.Value != null)
                {
                    foreach (var item in policyAssignments.Value)
                    {

                        Assignment assignment = new Assignment();
                        assignment.GroupID = item.Id.Substring(item.Id.IndexOf('_') + 1);
                        Policy.Assignments.Add(assignment);

                    }
                }

                policies.Add(Policy);
            }

            var compliancePolicies = await betaClient.DeviceManagement.CompliancePolicies.GetAsync();

            foreach (var a in compliancePolicies.Value)
            {
                Policy Policy = new Policy();
                Policy.Id = a.Id;
                Policy.DisplayName = a.Name;
                Policy.ModifiedDate = DateTime.ParseExact(a.LastModifiedDateTime.ToString(), "M/d/yyyy h:mm:ss tt zzz", System.Globalization.CultureInfo.InvariantCulture).ToString();

                var complianceAssignments = await betaClient.DeviceManagement.CompliancePolicies[$"{(a.Id)}"].Assignments.GetAsync();
                if (complianceAssignments.Value != null)
                {
                    foreach (var item in complianceAssignments.Value)
                    {

                        Assignment assignment = new Assignment();
                        assignment.GroupID = item.Id.Substring(item.Id.IndexOf(':') + 1);
                        Policy.Assignments.Add(assignment);

                    }
                }

                policies.Add(Policy);
            }
        }

        private async Task GetScripts(HttpClient client)
        {
            var graphClient = new Microsoft.Graph.GraphServiceClient(client);
            var betaClient = new Microsoft.Graph.Beta.GraphServiceClient(client);

            var deviceScripts = await betaClient.DeviceManagement.DeviceManagementScripts.GetAsync();

            foreach (var a in deviceScripts.Value)
            {
                Script Script = new Script();
                Script.Id = a.Id;
                Script.DisplayName = a.DisplayName;
                Script.ModifiedDate = DateTime.ParseExact(a.LastModifiedDateTime.ToString(), "M/d/yyyy h:mm:ss tt zzz", System.Globalization.CultureInfo.InvariantCulture).ToString();


                var scriptAssignments = await betaClient.DeviceManagement.DeviceManagementScripts[$"{a.Id}"].Assignments.GetAsync();
                if (scriptAssignments.Value != null)
                {
                    foreach (var item in scriptAssignments.Value)
                    {

                        Assignment assignment = new Assignment();
                        assignment.GroupID = item.Id.Substring(item.Id.IndexOf(':') + 1);
                        Script.Assignments.Add(assignment);

                    }
                }

                scripts.Add(Script);
            }

            var shellScripts = await betaClient.DeviceManagement.DeviceShellScripts.GetAsync();

            foreach (var a in shellScripts.Value)
            {
                Script Script = new Script();
                Script.Id = a.Id;
                Script.DisplayName = a.DisplayName;
                Script.ModifiedDate = DateTime.ParseExact(a.LastModifiedDateTime.ToString(), "M/d/yyyy h:mm:ss tt zzz", System.Globalization.CultureInfo.InvariantCulture).ToString();

                var shellScriptAssignments = await betaClient.DeviceManagement.DeviceShellScripts[$"{(a.Id)}"].Assignments.GetAsync();
                if (shellScriptAssignments.Value != null)
                {
                    foreach (var item in shellScriptAssignments.Value)
                    {

                        Assignment assignment = new Assignment();
                        assignment.GroupID = item.Id.Substring(item.Id.IndexOf(':') + 1);
                        Script.Assignments.Add(assignment);

                    }
                }

                scripts.Add(Script);
            }
        }
        private async Task GetConfigurations(HttpClient client)
        {
            var graphClient = new Microsoft.Graph.GraphServiceClient(client);
            var betaClient = new Microsoft.Graph.Beta.GraphServiceClient(client);

            var tasks = new Task[]
            {
                Task.Run(async () =>
                {
                    var configs = await betaClient.DeviceManagement.DeviceConfigurations.GetAsync((requestConfiguration) =>
                        {
                            requestConfiguration.QueryParameters.Expand = new string[] { "assignments" };
                        });
                    configs.Value.ForEach(config =>
                    {
                        Configuration Config = new Configuration();
                        Config.Id = config.Id;
                        Config.DisplayName = config.DisplayName;
                        Config.ModifiedDate = DateTime.ParseExact(config.LastModifiedDateTime.ToString(), "M/d/yyyy h:mm:ss tt zzz", System.Globalization.CultureInfo.InvariantCulture).ToString();

                        if (config.Assignments != null)
                        {
                            foreach (var item in config.Assignments)
                            {
                                Assignment assignment = new Assignment();
                                assignment.GroupID = item.Id.Substring(item.Id.IndexOf('_') + 1);
                                Config.Assignments.Add(assignment);

                            }
                        }
                        configurations.Add(Config);
                    });
                 
                }),

                Task.Run(async () =>
                {
                    var configs = await betaClient.DeviceManagement.GroupPolicyConfigurations.GetAsync((requestConfiguration) =>
                        {
                            requestConfiguration.QueryParameters.Expand = new string[] { "assignments" };
                        });
                    configs.Value.Select(async config =>
                    {
                        Configuration Config = new Configuration();
                        Config.Id = config.Id;
                        Config.DisplayName = config.DisplayName;
                        Config.ModifiedDate = DateTime.ParseExact(config.LastModifiedDateTime.ToString(), "M/d/yyyy h:mm:ss tt zzz", System.Globalization.CultureInfo.InvariantCulture).ToString();

                        if (config.Assignments != null)
                        {
                            foreach (var item in config.Assignments)
                            {
                                Assignment assignment = new Assignment();
                                assignment.GroupID = item.Id.Substring(item.Id.IndexOf('_') + 1);
                                Config.Assignments.Add(assignment);

                            }
                        }
                        configurations.Add(Config);
                    });
                }),

                Task.Run(async () =>
                {
                    var configs = await betaClient.DeviceManagement.ConfigurationPolicies.GetAsync((requestConfiguration) =>
                        {
                            requestConfiguration.QueryParameters.Expand = new string[] { "assignments" };
                        });
                    var pageIterator = PageIterator<DeviceManagementConfigurationPolicy, DeviceManagementConfigurationPolicyCollectionResponse>
                    .CreatePageIterator(
                    betaClient,
                    configs,
                    async (config) =>
                    {
                        Configuration Config = new Configuration();
                        Config.Id = config.Id;
                        Config.DisplayName = config.Name;
                        Config.ModifiedDate = DateTime.ParseExact(config.LastModifiedDateTime.ToString(), "M/d/yyyy h:mm:ss tt zzz", System.Globalization.CultureInfo.InvariantCulture).ToString();

                        if (config.Assignments != null)
                        {
                            foreach (var item in config.Assignments)
                            {
                                Assignment assignment = new Assignment();
                                assignment.GroupID = item.Id.Substring(item.Id.IndexOf('_') + 1);
                                Config.Assignments.Add(assignment);
                            }
                        }
                        configurations.Add(Config);
                        return true;
                    },
                    (req) =>
                    {
                        return req;
                    }
                    );
                await pageIterator.IterateAsync();
                }),
            };
            await Task.WhenAll(tasks);
        }

        private async void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            var accounts = await App.PublicClientApp.GetAccountsAsync();

            if (accounts.Any())
            {
                try
                {
                    await App.PublicClientApp.RemoveAsync(accounts.FirstOrDefault());
                    this.SignOutButton.Visibility = Visibility.Collapsed;
                }
                catch (MsalException ex)
                {
                    ResultText.Text = $"Error signing-out user: {ex.Message}";
                }
            }
        }

        private async void btnSearchGroup_Click(object sender, RoutedEventArgs e)
        {
            var searchGroup = txtGroupName.Text.Trim();
            if (searchGroup == null || searchGroup.Length == 0)
            {
                MessageBox.Show("Please enter a group name and try again.", "Missing Group Name");
                return;
            }

            AuthenticationResult authResult = null;
            var app = App.PublicClientApp;


            var accounts = await app.GetAccountsAsync();
            //var firstAccount = accounts.FirstOrDefault();

            IAccount firstAccount = (await app.GetAccountsAsync()).FirstOrDefault();

            try
            {
                authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                try
                {
                    authResult = await app.AcquireTokenInteractive(scopes)
                        .WithParentActivityOrWindow(new WindowInteropHelper(this).Handle)
                        .WithPrompt(Prompt.SelectAccount)
                        .WithPrompt(Prompt.ForceLogin)
                        .ExecuteAsync();
                }
                catch (MsalException msalex)
                {
                    ResultText.Text = $"Error Acquiring Token:{System.Environment.NewLine}{msalex}";
                }
            }
            
            

            if (authResult != null)
            {

                if (applications.Any() || configurations.Any())
                {
                    var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

                    ApplicationList.ItemsSource = null;
                    ConfigurationList.ItemsSource = null;
                    DevicesList.ItemsSource = null;
                    RemediationList.ItemsSource = null;
                    ScriptsList.ItemsSource = null;
                    PoliciesList.ItemsSource = null;

                    gifSearch.Visibility = Visibility.Visible;

                    Microsoft.Graph.Models.Group groupInfo = await GetGroupInfo(httpClient, searchGroup);
                    
                    txtGroupInfoId.Text = groupInfo.Id;
                    txtGroupInfoName.Text = groupInfo.DisplayName;
                    txtGroupInfoRule.Text = groupInfo.MembershipRule;
                    txtGroupInfoType.Text = groupInfo.GroupTypes[0];

                    ApplicationList.ItemsSource = applications.Where(a => a.Assignments.Any(b => b.GroupID == $"{groupInfo.Id}")).ToList();
                    ConfigurationList.ItemsSource = configurations.Where(a => a.Assignments.Any(b => b.GroupID == $"{groupInfo.Id}")).ToList();
                    DevicesList.ItemsSource = devices;
                    RemediationList.ItemsSource = remediations.Where(a => a.Assignments.Any(b => b.GroupID == $"{groupInfo.Id}")).ToList();
                    ScriptsList.ItemsSource = scripts.Where(a => a.Assignments.Any(b => b.GroupID == $"{groupInfo.Id}")).ToList();
                    PoliciesList.ItemsSource = policies.Where(a => a.Assignments.Any(b => b.GroupID == $"{groupInfo.Id}")).ToList();

                    gifSearch.Visibility = Visibility.Hidden;
                }
                else
                {

                    var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                    this.SignOutButton.Visibility = Visibility.Visible;
                    Microsoft.Graph.Models.Group groupInfo = null;
                    gifSearch.Visibility = Visibility.Visible;

                    try { groupInfo = await GetGroupInfo(httpClient, searchGroup); }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Group err: " + ex.ToString());
                        return;
                    }
                    
                    txtGroupInfoId.Text = groupInfo.Id;
                    txtGroupInfoName.Text = groupInfo.DisplayName;
                    txtGroupInfoRule.Text = groupInfo.MembershipRule;
                    txtGroupInfoType.Text = groupInfo.GroupTypes[0];

                    var tasks = new Task[]
                        {
                        Task.Run(async () =>
                        {

                            applications = await GetApplications_v2(httpClient);
                            Dispatcher.Invoke(() =>
                            {
                                ApplicationList.ItemsSource = applications.Where(a => a.Assignments.Any(b => b.GroupID == $"{groupInfo.Id}")).ToList();
                                DevicesList.ItemsSource = devices;
                            });
                        }),

                        Task.Run(async () =>
                        {
                            await GetConfigurations(httpClient);
                            Dispatcher.Invoke(() => {
                                ConfigurationList.ItemsSource = configurations.Where(a => a.Assignments.Any(b => b.GroupID == $"{groupInfo.Id}")).ToList();
                            });
                        }),

                        Task.Run(async () =>
                        {
                            await GetRemediations(httpClient);
                            Dispatcher.Invoke(() =>
                            {
                                RemediationList.ItemsSource = remediations.Where(a => a.Assignments.Any(b => b.GroupID == $"{groupInfo.Id}")).ToList();
                            });
                        }),

                        Task.Run(async () =>
                        {
                            await GetScripts(httpClient);
                            Dispatcher.Invoke(() =>
                            {
                                ScriptsList.ItemsSource = scripts.Where(a => a.Assignments.Any(b => b.GroupID == $"{groupInfo.Id}")).ToList();
                            });
                        }),

                        Task.Run(async () =>
                        {
                            await GetPolicies(httpClient);
                            Dispatcher.Invoke(() =>
                            {
                                PoliciesList.ItemsSource = policies.Where(a => a.Assignments.Any(b => b.GroupID == $"{groupInfo.Id}")).ToList();
                            });
                        })

                        };
                    await Task.WhenAll(tasks);

                    gifSearch.Visibility = Visibility.Hidden;
                }
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Owner = Window.GetWindow(this);
            settingsWindow.Show();
        }
    }
}

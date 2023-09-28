using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IntuneGroupAssignments
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        IConfigurationRoot config;

        Models.AppSettings? appSettings;

        public SettingsWindow()
        {
           config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json")
               .AddEnvironmentVariables()
               .Build();
            appSettings = config.GetRequiredSection("settings").Get<Models.AppSettings>();
            
            InitializeComponent();
            txtClientID.Text = appSettings.clientId ?? "Failed to read appsettings";
            txtTenantID.Text = appSettings.tenantId ?? "Failed to read appsettings";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnCanccel_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {


            if (txtClientID.Text.Length > 0 && txtTenantID.Text.Length > 0)
            {
                appSettings.clientId = txtClientID.Text;
                appSettings.tenantId = txtTenantID.Text;

                var jsonWriteOptions = new JsonSerializerOptions()
                {
                    WriteIndented = true,
                };
                jsonWriteOptions.Converters.Add(new JsonStringEnumConverter());
                //var newJson = JsonSerializer.Serialize(settings, jsonWriteOptions);
                var newJson = JsonSerializer.Serialize(new
                {
                    settings = appSettings
                }, jsonWriteOptions);
                var appSettingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                File.WriteAllText(appSettingsPath, newJson);
                MessageBox.Show("After updating the ClientID or TenantID you must exit and relaunch the app.");
                Window.GetWindow(this).Close();
                Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
                
            }

        }
    }
}

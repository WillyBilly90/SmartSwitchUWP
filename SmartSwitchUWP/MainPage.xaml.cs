using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Resources;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SmartSwitchUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        string userName, passWord;
        HomeWizardConnector @switch = new HomeWizardConnector();
        private List<SmartSwitch> switchList;

        public MainPage()
        {
            this.InitializeComponent();

            //get saved HomeWizard credentials 
            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                userName = localSettings.Values["Username"].ToString();
                passWord = localSettings.Values["Password"].ToString();
                FillSmartSwitchListView(userName, passWord);
            }
            catch (Exception e)
            {
                //no credentials available, open popup
                CredentialMenu_Click(null,null);
            }
        }

        public void FillSmartSwitchListView(string userName,string passWord)
        {
            try
            {
                switchList = @switch.PowerSwitch(userName, passWord);
                SmartSwitchListView.Items.Clear();
                foreach (var item in switchList)
                {
                    //create string for listview
                    string temp = "Controller: " + item.ControllerName + " Device: " + item.DeviceName;
                    //add to listview
                    SmartSwitchListView.Items.Add(temp);
                }
            }
            catch (Exception e)
            {
                //error getting json, mostly problem with credentials or internetconnection
                var loader=new ResourceLoader();
                string message = loader.GetString("JsonLoadError");
                string header=loader.GetString("JsonLoadErrorHeader");
                Show(message,header);
            }         
        }

        public static async void Show(string message, string header)
        {
            //little task for an errormessage
            var dialog = new MessageDialog(message, header);
            await dialog.ShowAsync();
        }

        private void CredentialMenu_Click(object sender, RoutedEventArgs e)
        {
            ppup.Height = Window.Current.Bounds.Height;
            ppup.IsOpen = true;
        }

        private void Shutdown_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            ppup.IsOpen = false;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            //save credentials to local settings
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Username"] = UserNameTbx.Text;
            localSettings.Values["Password"] = PasswordTbx.Text;
            //refresh listbox
            FillSmartSwitchListView(UserNameTbx.Text, PasswordTbx.Text);
            ppup.IsOpen = false;
        }

        public void PowerOnBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SmartSwitchListView.SelectedItem != null)
            {
                string controllerId = switchList[SmartSwitchListView.SelectedIndex].ControllerId;
                string deviceId = switchList[SmartSwitchListView.SelectedIndex].DeviceId;
                @switch.DoAction(controllerId,deviceId,"On",userName,passWord);
            }
            else
            {
                //No device chosen
                var loader = new ResourceLoader();
                string message = loader.GetString("NoDeviceChosenError");
                string header = loader.GetString("NoDeviceChosenHeader");
                Show(message, header);
            }
        }

        private void PowerOffBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SmartSwitchListView.SelectedItem != null)
            {
                string controllerId = switchList[SmartSwitchListView.SelectedIndex].ControllerId;
                string deviceId = switchList[SmartSwitchListView.SelectedIndex].DeviceId;
                @switch.DoAction(controllerId, deviceId, "Off", userName, passWord);
            }
            else
            {
                //No device chosen
                var loader = new ResourceLoader();
                string message = loader.GetString("NoDeviceChosenError");
                string header = loader.GetString("NoDeviceChosenHeader");
                Show(message, header);
            }
        }
    }
}

using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using Windows.UI.Notifications;

namespace TapShareBLEServer
{
    public partial class Form1 : Form
    {
        public Guid ServiceUUID = new Guid("5C593F65-CEE2-4696-ADD4-09CBB8B5480B");
        public Guid CharacteristicUUID = new Guid("C39B773A-847B-4129-9BB9-AAA9D494995A");
        public GattServiceProvider serviceProvider;
        public GattLocalCharacteristic writeCharacteristic;
        public Form1()
        {
            InitializeComponent();
        }
        public static void handleToastEvent(string args)
        {
            if (args.StartsWith("copy-"))
            {
                Clipboard.SetText(File.ReadAllText("./" + args.Substring(5)));
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            initBLEServer();
        }
        public void raiseErrorDialog(string message)
        {
            MessageBox.Show(message, "TapShare BLE Server Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        public void raiseMessageDialog(string message)
        {
            long currentTicks = DateTime.Now.Ticks;
            File.WriteAllText("./" + currentTicks.ToString(), message);
            if (message.StartsWith("http://") || message.StartsWith("https://"))
            {
                Process.Start(message);
            }
            else if (Regex.Match(message, "(av[1-9]\\d*)|(BV1[1-9A-NP-Za-km-z]{9})").Success)
            {
                Process.Start("https://b23.tv/" + message);
            }
            else
            {
                //MessageBox.Show(new Form { TopMost = true }, message, "Incoming message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ToastContentBuilder builder = new ToastContentBuilder()
                .AddText("新投送消息")
                    .AddText(new String(message.Where(ch => XmlConvert.IsXmlChar(ch)).ToArray()))
                    .AddButton("复制", ToastActivationType.Background, "copy-" + currentTicks)
                    .AddButton("完成", ToastActivationType.Background, "")
                    .SetToastScenario(ToastScenario.Reminder);
                var toastNotif = new ToastNotification(builder.GetToastContent().GetXml());
                toastNotif.Tag = "incoming-message";
                toastNotif.Group = "tapshare-server-common-notification";
                ToastNotificationManagerCompat.CreateToastNotifier().Show(toastNotif);
            }
        }
        private async void initBLEServer()
        {
            GattServiceProviderResult result = await GattServiceProvider.CreateAsync(ServiceUUID);
            if (result.Error == BluetoothError.Success)
            {
                serviceProvider = result.ServiceProvider;
            }
            GattLocalCharacteristicParameters writeParameters = new GattLocalCharacteristicParameters();
            writeParameters.CharacteristicProperties = GattCharacteristicProperties.WriteWithoutResponse;
            GattLocalCharacteristicResult characteristicResult = await serviceProvider.Service.CreateCharacteristicAsync(CharacteristicUUID, writeParameters);
            if (characteristicResult.Error != BluetoothError.Success)
            {
                raiseErrorDialog("CreateCharacteristicAsync returns " + characteristicResult.Error);
                return;
            }
            writeCharacteristic = characteristicResult.Characteristic;
            writeCharacteristic.WriteRequested += WriteCharacteristic_WriteRequested;
            GattServiceProviderAdvertisingParameters advParameters = new GattServiceProviderAdvertisingParameters
            {
                IsDiscoverable = true,
                IsConnectable = true
            };
            serviceProvider.StartAdvertising(advParameters);
        }
        async void WriteCharacteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            var request = await args.GetRequestAsync();
            var reader = DataReader.FromBuffer(request.Value);
            raiseMessageDialog(reader.ReadString(reader.UnconsumedBufferLength));
            if (request.Option == GattWriteOption.WriteWithResponse)
            {
                request.Respond();
            }
            deferral.Complete();
        }
    }
}

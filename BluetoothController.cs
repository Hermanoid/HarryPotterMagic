using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace Microsoft.Samples.Kinect.InfraredBasics
{
    public class BluetoothController
    {
        private Guid SERVICE_UUID = new Guid("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
        private Guid CHARACTERISTIC_UUID = new Guid("12381d6f-57cd-4845-a22b-39382199a992");
        private List<string> badDeviceIds = new List<string>();
        private Dictionary<Spell, BluetoothLEDevice> spellDevices;
        private Dictionary<Spell, GattCharacteristic> spellTriggers;
        public delegate void SpellListChangeEvent(Spell[] spells);
        public event SpellListChangeEvent OnSpellListChanged;
        //private GattServiceProvider serviceProvider;
        //private GattLocalCharacteristic actionCharacteristic;
        //private GattLocalCharacteristic nameCharacteristic;
        public BluetoothController()
        {
            spellDevices = new Dictionary<Spell, BluetoothLEDevice>();
            spellTriggers = new Dictionary<Spell, GattCharacteristic>();
        }

        public void Initialize()
        {

            // Query for extra properties you want returned
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            DeviceWatcher deviceWatcher =
                        DeviceInformation.CreateWatcher("System.ItemNameDisplay:~~\"Potter: \"",
                                requestedProperties,
                                DeviceInformationKind.AssociationEndpoint);
            
            // Register event handlers before starting the watcher.
            // Added, Updated and Removed are required to get all nearby devices
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;

            // Start the watcher.
            deviceWatcher.Start();

            SpellListChanged();

            //var advertisementWatcher = new BluetoothLEAdvertisementWatcher();
            //advertisementWatcher.ScanningMode = BluetoothLEScanningMode.Active;
            ////advertisementWatcher.SignalStrengthFilter = new BluetoothSignalStrengthFilter
            //{
            //    InRangeThresholdInDBm = -75,
            //    OutOfRangeThresholdInDBm = -76,
            //    OutOfRangeTimeout = TimeSpan.FromSeconds(2),
            //    SamplingInterval = TimeSpan.FromSeconds(2)
            //};
            //advertisementWatcher.AdvertisementFilter =
            //     new BluetoothLEAdvertisementFilter
            //     {
            //         Advertisement =
            //                  new BluetoothLEAdvertisement
            //                  {
            //                      ServiceUuids =
            //                                {
            //                                        SERVICE_UUID
            //                                }
            //                  }
            //     };
            //advertisementWatcher.Received += AdvertisementRecieved;
            //advertisementWatcher.Start();
        }

        //private void AdvertisementRecieved(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        //{
        //    //BluetoothLEDevice bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(args.BluetoothAddress);

        //    Console.WriteLine(args.Advertisement.LocalName + ":" + String.Join(", ", args.Advertisement.ServiceUuids));
        //}

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (badDeviceIds.Contains(args.Id))
            {
                return;
            }
            Console.WriteLine($"Added: {args.Id} ({args.Kind})");
            try
            {
                BluetoothLEDevice device = BluetoothLEDevice.FromIdAsync(args.Id).AsTask().Result;
                if (device == null) return;
                Spell result = await Connect(args, device);
                device.ConnectionStatusChanged += async (dev, obj) =>
                {
                    if (dev.ConnectionStatus == BluetoothConnectionStatus.Connected)
                    {
                        await Connect(args, dev);
                    }
                    else
                    {
                        spellDevices.Remove(result);
                        spellTriggers.Remove(result);
                    }
                    SpellListChanged();
                };
                SpellListChanged();

            }
            catch (Exception e)
            {
                object friendlyNameObj;
                string friendlyName = "<no_name>";
                if (args.Properties.TryGetValue("System.ItemNameDisplay", out friendlyNameObj))
                {
                    friendlyName = (string)friendlyNameObj;
                }
                Console.WriteLine($"Failed to connect to {args.Id} ({friendlyName}): {e.Message}");
            }
        }

        private async Task<Spell> Connect(DeviceInformation args, BluetoothLEDevice device)
        {
            string spellName = device.Name.Split(new string[] { ": " }, StringSplitOptions.None)[1];
            Spell result;
            Action<string> markBadDevice = (msg) =>
            {
                Console.WriteLine(msg);
                badDeviceIds.Add(args.Id);
                device.Dispose();
                return;
            };
            if (!Enum.TryParse<Spell>(spellName, out result))
            {
                markBadDevice("Spell device: " + device.Name + " has an invalid spell name!");
            }
            var serviceResult = await device.GetGattServicesForUuidAsync(SERVICE_UUID);
            if (serviceResult.Services.Count != 1)
            {
                markBadDevice("Spell device" + device.Name + " does not have a spell service!");
            }
            var characteristicsResult = await serviceResult.Services[0].GetCharacteristicsForUuidAsync(CHARACTERISTIC_UUID);
            if (characteristicsResult.Characteristics.Count != 1)
            {
                markBadDevice("Spell device " + device.Name + " has an invalid number of trigger characteristics!");
            }
            spellDevices[result] = device;
            spellTriggers[result] = characteristicsResult.Characteristics[0];
            return result;
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {

        }
        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            // We currently don't address this, when a device is manually disconnected.
        }

        public async Task TriggerSpell(Spell spell)
        {
            if (spellTriggers.ContainsKey(spell))
            {
                var writer = new DataWriter();
                writer.WriteString("Trigger");
                await spellTriggers[spell].WriteValueAsync(writer.DetachBuffer());
            }
            else
            {
                Console.WriteLine("No attached device exists for spell: " + spell.ToString());
            }
        }

        private void SpellListChanged()
        {
            OnSpellListChanged.Invoke(spellDevices.Keys.ToArray());
        }
    }
}

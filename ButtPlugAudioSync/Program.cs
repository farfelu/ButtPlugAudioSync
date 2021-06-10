using Buttplug;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ButtPlugAudioSync
{
    class Program
    {
        private static async Task WaitForKey()
        {
            while (!Console.KeyAvailable)
            {
                await Task.Delay(1);
            }
            Console.ReadKey(true);
        }

        private static async Task RunAudioSync()
        {
            Console.CursorVisible = false;

            var client = new ButtplugClient("Audio Sync Client");
            await client.ConnectAsync(new ButtplugEmbeddedConnectorOptions());

            void HandleDeviceAdded(object aObj, DeviceAddedEventArgs aArgs)
            {
                Console.WriteLine($"Device connected: {aArgs.Device.Name}");
            }

            client.DeviceAdded += HandleDeviceAdded;

            void HandleDeviceRemoved(object aObj, DeviceRemovedEventArgs aArgs)
            {
                Console.WriteLine($"Device disconnected: {aArgs.Device.Name}");
            }

            client.DeviceRemoved += HandleDeviceRemoved;

            async Task ScanForDevices()
            {
                Console.WriteLine("Scanning for devices, press any key to accept or abort...");
                await client.StartScanningAsync();
                await WaitForKey();

                await client.StopScanningAsync();
            }

            await ScanForDevices();

            if (!client.Devices.Any())
            {
                Console.WriteLine("No devices available, exiting...");
                await Task.Delay(2000);
                //return;
            }

            Console.WriteLine("Syncing audio, press escape to exit");
            
            async Task SendAllDevices(double num)
            {
                var tasks = new List<Task>();
                foreach (var client in client.Devices)
                {
                    tasks.Add(client.SendVibrateCmd(num));
                }

                await Task.WhenAll();
            }

            Console.Clear();
            Console.Write("Syncing Audio. Press ESCAPE to exit...");
            try
            {
                var soundCapture = new SoundCapture();
                var sw = Stopwatch.StartNew();
                var multiplier = 1.0f;
                var keepRunning = true;
                while (keepRunning)
                {
                    while (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        // can't break from switch/case
                        switch (key)
                        {
                            case ConsoleKey.Escape:
                                keepRunning = false;
                                break;
                            case ConsoleKey.UpArrow:
                                multiplier += 0.1f;
                                break;
                            case ConsoleKey.DownArrow:
                                multiplier -= 0.1f;
                                break;
                        }
                    }

                    sw.Restart();
                    var data = soundCapture.GetData(multiplier, true);
                    await SendAllDevices(data);
                    //Console.WriteLine(data);

                    var ms = (int)sw.ElapsedMilliseconds;
                    var sleep = 100 - ms;
                    sleep = sleep < 0 ? 0 : sleep;
                    await Task.Delay(sleep);
                }

                await SendAllDevices(0);

                Console.Clear();
                Console.WriteLine("Exiting...");
                await Task.Delay(4000);
                return;
            }
            catch (ButtplugDeviceException)
            {
                Console.Clear();
                Console.WriteLine("Device disconnected, exiting...");
                await Task.Delay(2000);
            }
        }


        private static void Main()
        {
            RunAudioSync().Wait();
            return;
        }
    }
}

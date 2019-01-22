using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LoRaTools;
using LoRaTools.LoRaMessage;
using LoRaTools.LoRaPhysical;
using LoRaWan.Test.Shared;
using Xunit;

namespace LoRaWan.IntegrationTest
{
    // Tests ABP requests
    [Collection(Constants.TestCollectionName)] // run in serial
    [Trait("Category", "SkipWhenLiveUnitTesting")]
    public sealed class SimulatorTestCollection : IntegrationTestBase
    {
        private readonly TimeSpan intervalBetweenMessages;
        private readonly TimeSpan intervalAfterJoin;
        public TestConfiguration Configuration = TestConfiguration.GetConfiguration();

        public SimulatorTestCollection(IntegrationTestFixture testFixture) : base(testFixture)
        {
            this.intervalBetweenMessages = TimeSpan.FromSeconds(5);
            this.intervalAfterJoin = TimeSpan.FromSeconds(10);
        }

        // check if we need to parametrize address
        //IPEndPoint CreateNetworkServerEndpoint() => new IPEndPoint(IPAddress.Broadcast, 1680);
        IPEndPoint CreateNetworkServerEndpoint() => new IPEndPoint(IPAddress.Parse(this.Configuration.NetworkServerIP), 1680);
                
        //[Fact]
        // public async Task Ten_Devices_Sending_Messages_Each_Second()
        // {
        //     var listSimulatedDevices = new List<SimulatedDevice>();
        //     foreach (var device in this.TestFixture.DeviceRange1000_ABP)
        //     {
        //         var simulatedDevice = new SimulatedDevice(device);
        //         listSimulatedDevices.Add(simulatedDevice);
        //     }
            
        //     var networkServerIPEndpoint = CreateNetworkServerEndpoint();

        //     using (var simulatedPacketForwarder = new SimulatedPacketForwarder(networkServerIPEndpoint))
        //     {
        //         simulatedPacketForwarder.Start();

        //         var deviceTasks = new List<Task>();
        //         foreach (var device in this.TestFixture.DeviceRange1000_ABP)
        //         {
        //             var simulatedDevice = new SimulatedDevice(device);
        //             deviceTasks.Add(SendDeviceMessagesAsync(simulatedPacketForwarder, simulatedDevice, 60, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)));
        //             await Task.Delay(2000);
        //         }

        //         await Task.WhenAll(deviceTasks);
        //         await simulatedPacketForwarder.StopAsync();
        //     }

        //     var eventsByDevices = this.TestFixture.IoTHubMessages.GetEvents().GroupBy(x => x.SystemProperties["iothub-connection-device-id"]);
        //     Assert.Equal(10, eventsByDevices.Count());
        // }
        

        [Fact]
        public async Task Single_ABP_Simulated_Device()
        {
            const int MessageCount = 5;

            var device = this.TestFixture.Device1001_Simulated_ABP;
            var simulatedDevice = new SimulatedDevice(device);
            var networkServerIPEndpoint = CreateNetworkServerEndpoint();

            using (var simulatedPacketForwarder = new SimulatedPacketForwarder(networkServerIPEndpoint))
            {
                simulatedPacketForwarder.Start();

                for (var i=1; i <= MessageCount; i++)
                {
                    await simulatedDevice.SendUnconfirmedMessageAsync(simulatedPacketForwarder, i.ToString());
                    await Task.Delay(intervalBetweenMessages);
                }

                await simulatedPacketForwarder.StopAsync();
            }
        }

        [Fact]
        public async Task Single_OTAA_Simulated_Device()
        {
            const int MessageCount = 5;

            var device = this.TestFixture.Device1002_Simulated_OTAA;
            var simulatedDevice = new SimulatedDevice(device);
            var networkServerIPEndpoint = CreateNetworkServerEndpoint();

            using (var simulatedPacketForwarder = new SimulatedPacketForwarder(networkServerIPEndpoint))
            {
                simulatedPacketForwarder.Start();

                bool joined = await simulatedDevice.JoinAsync(simulatedPacketForwarder);
                Assert.True(joined, "OTAA join failed");

                await Task.Delay(intervalAfterJoin);

                for (var i=1; i <= MessageCount; i++)
                {
                    await simulatedDevice.SendUnconfirmedMessageAsync(simulatedPacketForwarder, i.ToString());
                    await Task.Delay(intervalBetweenMessages);
                }

                await simulatedPacketForwarder.StopAsync();
            }

            // wait 10 seconds before checking if iot hub content is available
            await Task.Delay(TimeSpan.FromSeconds(10));

            var msgsFromDevice = this.TestFixture.IoTHubMessages.GetEvents().Where(x => x.GetDeviceId() == simulatedDevice.LoRaDevice.DeviceID);
            Assert.Equal(MessageCount, msgsFromDevice.Count());
        }


        
        [Fact(Skip = "simulated")]
        public async Task Simulated_Http_Based_Decoder_Scenario()
        {
            var device = this.TestFixture.Device1003_Simulated_HttpBasedDecoder;
            var simulatedDevice = new SimulatedDevice(device);
            var networkServerIPEndpoint = CreateNetworkServerEndpoint();

            using (var simulatedPacketForwarder = new SimulatedPacketForwarder(networkServerIPEndpoint))
            {
                simulatedPacketForwarder.Start();

                bool joined = await simulatedDevice.JoinAsync(simulatedPacketForwarder);
                Assert.True(joined, "OTAA join failed");

                await Task.Delay(intervalAfterJoin);

                for (var i=1; i <= 3; i++)
                {
                    await simulatedDevice.SendUnconfirmedMessageAsync(simulatedPacketForwarder, i.ToString());
                    await Task.Delay(intervalBetweenMessages);
                }

                await simulatedPacketForwarder.StopAsync();
            }

            // wait 10 seconds before checking if iot hub content is available
            await Task.Delay(TimeSpan.FromSeconds(10));

        }

        // Scenario:
        // - 100x ABP devices
        // - 10x OTAA devices
        // - Sending unconfirmed messages
        // - Goal: 20 devices in parallel
        [Fact]
        public async Task Multiple_ABP_and_OTAA_Simulated_Devices_Unconfirmed()
        {
            // amount of devices to test with. Maximum is 100
            var scenarioDeviceNumber = 20;

            // amount of messages to send per device (without warm-up phase)
            var scenarioMessagesPerDevice = 10;

            // amount of devices to send data in parallel
            var scenarioDeviceStepSize = 20;

            // amount of devices to send data in parallel for the warm-up phase
            var warmUpDeviceStepSize = 2;

            // amount of messages to send before device join is to occur
            var messagesBeforeJoin = 10;

            // amount of miliseconds to wait before checking LoRaWanNetworkSrvModule
            // for successful sending of messages to IoT Hub.
            // delay for 100 devices: 120000
            // delay for 20 devices: 15000
            var delayNetworkServerCheck = 15000;

            // amount of miliseconds to wait before checking of messages in IoT Hub
            // delay for 100 devices: 60000
            // delay for 20 devices: 15000
            var delayIoTHubCheck = 15000;

            // Get random number seed
            Random rnd = new Random();
            int seed = rnd.Next(100, 999);

            int count = 0;
            var listSimulatedDevices = new List<SimulatedDevice>();
            foreach (var device in this.TestFixture.DeviceRange1200_100_ABP)
            {
                if (count < scenarioDeviceNumber && count < 100)
                {
                    var simulatedDevice = new SimulatedDevice(device);
                    listSimulatedDevices.Add(simulatedDevice);
                    count++;
                }
            }
            var totalDevices = listSimulatedDevices.Count;

            var listSimulatedJoinDevices = new List<SimulatedDevice>();
            foreach (var joinDdevice in this.TestFixture.DeviceRange1300_10_OTAA)
            {
                var simulatedJoinDevice = new SimulatedDevice(joinDdevice);
                listSimulatedJoinDevices.Add(simulatedJoinDevice);
            }

            var networkServerIPEndpoint = CreateNetworkServerEndpoint();

            using (var simulatedPacketForwarder = new SimulatedPacketForwarder(networkServerIPEndpoint))
            {
                simulatedPacketForwarder.Start();

                // 1. picking 2x devices send an initial message (warm device cache in NtwSrv module)
                //    timeout of 2 seconds between each loop                
                var tasks = new List<Task>();
                for (var i = 0; i < totalDevices;)
                {
                    tasks.Clear();
                    foreach (var device in listSimulatedDevices.Skip(i).Take(warmUpDeviceStepSize)) // works?
                    {
                        await Task.Delay(rnd.Next(10, 250)); // Sleep between 10 and 250ms

                        TestLogger.Log($"[WARM-UP] {device.LoRaDevice.DeviceID}");
                        device.FrmCntUp = 1;
                        tasks.Add(device.SendUnconfirmedMessageAsync(simulatedPacketForwarder, seed + "000"));
                    }

                    await Task.WhenAll(tasks);
                    await Task.Delay(5000);

                    i += warmUpDeviceStepSize;
                }

                // 2. picking 20x devices sends messages (send 10 messages for each device)
                //    timeout of 5 seconds between each
                int messageCounter = 0;
                int joinDevice = 0;

                for (var messageId = 1; messageId <= scenarioMessagesPerDevice; ++messageId)
                {
                    for (var i = 0; i < totalDevices;)
                    {
                        tasks.Clear();
                        var payload = seed + messageId.ToString().PadLeft(3, '0');

                        foreach (var device in listSimulatedDevices.Skip(i).Take(scenarioDeviceStepSize))
                        {
                            await Task.Delay(rnd.Next(10, 250)); // Sleep between 10 and 250ms

                            tasks.Add(device.SendUnconfirmedMessageAsync(simulatedPacketForwarder, payload));

                            messageCounter++;
                            if (messageCounter == messagesBeforeJoin - 1)
                            {

                                await Task.Delay(rnd.Next(10, 250)); // Sleep between 10 and 250ms

                                tasks.Add(listSimulatedJoinDevices[joinDevice].JoinAsync(simulatedPacketForwarder));
                                TestLogger.Log($"[INFO] Join request sent for {listSimulatedJoinDevices[joinDevice].LoRaDevice.DeviceID}");

                                joinDevice = (joinDevice == 10) ? 0 : joinDevice + 1;
                                messageCounter = 0;
                            }
                        }

                        await Task.WhenAll(tasks);
                        await Task.Delay(5000);

                        i += scenarioDeviceStepSize;
                    }
                }

                await simulatedPacketForwarder.StopAsync();
            }

            // Wait before executing to allow for all messages to be sent
            TestLogger.Log($"[INFO] Waiting for {delayNetworkServerCheck / 1000} sec. before the test continues...");
            await Task.Delay(delayNetworkServerCheck);

            // 3. test Network Server logs if messages have arrived
            string expectedPayload;
            foreach (var device in listSimulatedDevices)
            {
                TestLogger.Log($"[INFO] Looking for upstream messages for {device.LoRaDevice.DeviceID}");
                for (var messageId = 0; messageId <= scenarioMessagesPerDevice; ++messageId)
                {
                    // Find "<all Device ID>: message '{"value":<seed+0 to number of msg/device>}' sent to hub" in network server logs
                    expectedPayload = $"{device.LoRaDevice.DeviceID}: message '{{\"value\":{seed + messageId.ToString().PadLeft(3, '0')}}}' sent to hub";
                    await this.TestFixture.AssertNetworkServerModuleLogStartsWithAsync(expectedPayload);
                }
            }

            TestLogger.Log($"[INFO] Waiting for {delayIoTHubCheck / 1000} sec. before the test continues...");
            await Task.Delay(delayIoTHubCheck);

            // IoT Hub test for arrival of messages.
            var eventsByDevices = this.TestFixture.IoTHubMessages.GetEvents()
                .GroupBy(x => x.SystemProperties["iothub-connection-device-id"])
                .ToDictionary(x => x.Key, x => x.ToList());

            // 4. Check that we have the right amount of devices receiving messages in IoT Hub
            //Assert.Equal(totalDevices, eventsByDevices.Count());

            if (totalDevices == eventsByDevices.Count())
            {
                TestLogger.Log($"[INFO] Devices sending messages: {totalDevices}, == Devices receiving messages in IoT Hub: {eventsByDevices.Count()}");
            }
            else
            {
                TestLogger.Log($"[WARN] Devices sending messages: {totalDevices}, != Devices receiving messages in IoT Hub: {eventsByDevices.Count()}");
            }

            // 5. Check that the correct number of messages have arrived in IoT Hub per device
            //    Warn only.
            foreach (var device in listSimulatedDevices)
            {
                //Assert.True(
                //    eventsByDevices.TryGetValue(device.LoRaDevice.DeviceID, out var events), 
                //    $"No messages were found for device {device.LoRaDevice.DeviceID}");
                //if (events.Count > 0)

                if (eventsByDevices.TryGetValue(device.LoRaDevice.DeviceID, out var events))
                {
                    var actualAmountOfMsgs = events.Where(x => !x.Properties.ContainsKey("iothub-message-schema")).Count();
                    // Assert.Equal((1 + scenarioMessagesPerDevice), actualAmountOfMsgs);

                    if ((1 + scenarioMessagesPerDevice) != actualAmountOfMsgs)
                    {
                        TestLogger.Log($"[WARN] Wrong events for device {device.LoRaDevice.DeviceID}. Actual: {actualAmountOfMsgs}. Expected {1 + scenarioMessagesPerDevice}");
                    }
                    else
                    {
                        TestLogger.Log($"[INFO] Correct events for device {device.LoRaDevice.DeviceID}. Actual: {actualAmountOfMsgs}. Expected {1 + scenarioMessagesPerDevice}");
                    }
                }
                else
                {
                    TestLogger.Log($"[WARN] No messages were found for device {device.LoRaDevice.DeviceID}");
                }
            }

            // 6. Check if all expected messages have arrived in IoT Hub
            //    Warn only.
            foreach (var device in listSimulatedDevices)
            {
                TestLogger.Log($"[INFO] Looking for IoT Hub messages for {device.LoRaDevice.DeviceID}");
                for (var messageId = 0; messageId <= scenarioMessagesPerDevice; ++messageId)
                {
                    // Find message containing '{"value":<seed>.<0 to number of msg/device>}' for all leaf devices in IoT Hub
                    expectedPayload = $"{{\"value\":{seed + messageId.ToString().PadLeft(3, '0')}}}";
                    await this.TestFixture.AssertIoTHubDeviceMessageExistsAsync(device.LoRaDevice.DeviceID, expectedPayload);
                }
            }
        }
    }
}
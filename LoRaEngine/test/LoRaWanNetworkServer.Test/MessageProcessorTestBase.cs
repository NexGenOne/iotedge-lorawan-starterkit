﻿using LoRaTools.LoRaMessage;
using LoRaTools.LoRaPhysical;
using LoRaWan.NetworkServer;
using LoRaWan.NetworkServer.V2;
using LoRaWan.Test.Shared;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoRaWan.NetworkServer.Test
{
    public class MessageProcessorTestBase
    {
        protected const string ServerGatewayID = "test-gateway";

        private long startTime;
        private readonly Rxpk rxpk;
        private readonly byte[] macAddress;
        private NetworkServerConfiguration serverConfiguration;
        private readonly Mock<ILoRaDeviceFrameCounterUpdateStrategy> frameCounterUpdateStrategy;
        private readonly Mock<ILoRaDeviceFrameCounterUpdateStrategyFactory> frameCounterUpdateStrategyFactory;

        protected NetworkServerConfiguration ServerConfiguration { get => serverConfiguration; }

        protected Mock<ILoRaDeviceFrameCounterUpdateStrategy> FrameCounterUpdateStrategy => frameCounterUpdateStrategy;

        protected Mock<ILoRaDeviceFrameCounterUpdateStrategyFactory> FrameCounterUpdateStrategyFactory { get => frameCounterUpdateStrategyFactory; }

        private readonly Mock<ILoRaDeviceRegistry> loRaDeviceRegistry;
        protected Mock<ILoRaDeviceRegistry> LoRaDeviceRegistry => loRaDeviceRegistry;

        public MessageProcessorTestBase()
        {
            this.startTime = DateTimeOffset.UtcNow.Ticks;
            this.rxpk = new Rxpk()
            {
                chan = 7,
                rfch = 1,
                freq = 903.700000,
                stat = 1,
                modu = "LORA",
                datr = "SF10BW125",
                codr = "4/5",
                rssi = -17,
                lsnr = 12.0f
            };

            this.macAddress = Utility.GetMacAddress();
            this.serverConfiguration = new NetworkServerConfiguration
            {
                GatewayID = ServerGatewayID,
            };

            this.frameCounterUpdateStrategy = new Mock<ILoRaDeviceFrameCounterUpdateStrategy>(MockBehavior.Strict);
            this.frameCounterUpdateStrategyFactory = new Mock<ILoRaDeviceFrameCounterUpdateStrategyFactory>(MockBehavior.Strict);
            this.loRaDeviceRegistry = new Mock<ILoRaDeviceRegistry>(MockBehavior.Strict);
            this.loRaDeviceRegistry.Setup(x => x.RegisterDeviceInitializer(It.IsNotNull<ILoRaDeviceInitializer>()));
        }


        protected Rxpk CreateRxpk(LoRaPayload loraPayload)
        {
            var rxpk = new Rxpk()
            {
                chan = 7,
                rfch = 1,
                freq = 903.700000,
                stat = 1,
                modu = "LORA",
                datr = "SF10BW125",
                codr = "4/5",
                rssi = -17,
                lsnr = 12.0f
            };

            var data = loraPayload.GetByteMessage();
            rxpk.data = Convert.ToBase64String(data);
            rxpk.size = (uint)data.Length;
            // tmst it is time in micro seconds
            var now = DateTimeOffset.UtcNow;
            var tmst = (now.UtcTicks - this.startTime) / (TimeSpan.TicksPerMillisecond / 1000);
            if (tmst >= UInt32.MaxValue)
            {
                tmst = tmst - UInt32.MaxValue;
                this.startTime = now.UtcTicks - tmst;
            }
            rxpk.tmst = Convert.ToUInt32(tmst);

            return rxpk;
        }
    }
}
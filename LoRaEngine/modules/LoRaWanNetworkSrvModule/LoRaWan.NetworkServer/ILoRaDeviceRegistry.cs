﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LoRaWan.NetworkServer
{
    public interface ILoRaDevice
    {
        string GatewayID { get; }
        int FcntUp { get; }
        string AppSKey { get; }
        int? ReceiveDelay1 { get; }
        int? ReceiveDelay2 { get; }
        bool AlwaysUseSecondWindow { get; }

        bool IsABP();
        bool WasNotJustReadFromCache();
        int IncrementFcntDown(int value);
        bool IsABPRelaxedFrameCounter();
        void SetFcntUp(int value);
        void SetFcntDown(int value);

        Task<Message> ReceiveCloudToDeviceAsync(TimeSpan waitTime);
        Task CompleteCloudToDeviceMessageAsync(Message c2dMsg);
        Task AbandonCloudToDeviceMessageAsync(Message additionalMsg);
        Task UpdateTwinAsync(object twinProperties);
        Dictionary<string, object> GetTwinProperties();
        Task SendEventAsync(Message message);
    }

    public interface ILoRaDeviceRegistry
    {
        // Going to search devices in
        // 1. Cache
        // 2. If offline -> local storage (future functionality, reverse lookup)
        // 3. If online -> call function (return all devices with matching devaddr)
        // 3.1 Validate [mic, gatewayid]

        // In order to handle a scenario where the network server is restarted and the fcntDown was not yet saved (we save every 10)
        // If device does not have gatewayid this will be handled by the service facade function (NextFCntDown)
        // 4. if (loraDeviceInfo.IsABP() && loraDeviceInfo.GatewayID != null && loraDeviceInfo was not read from cache)  device.FcntDown += 10;


        Task<ILoRaDevice> GetDeviceForPayloadAsync(LoRaTools.LoRaMessage.LoRaPayloadData loraPayload);
    }
}
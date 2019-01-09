﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LoRaWan.NetworkServer.V2
{
    /// <summary>
    /// LoRa Device API Service
    /// </summary>
    public sealed class LoRaDeviceAPIService : LoRaDeviceAPIServiceBase
    {
        private readonly NetworkServerConfiguration configuration;
        private readonly IServiceFacadeHttpClientProvider serviceFacadeHttpClientProvider;

        public LoRaDeviceAPIService(NetworkServerConfiguration configuration, IServiceFacadeHttpClientProvider serviceFacadeHttpClientProvider)
            : base(configuration)
        {
            this.configuration = configuration;
            this.serviceFacadeHttpClientProvider = serviceFacadeHttpClientProvider;
        }

        public override async Task<ushort> NextFCntDownAsync(string devEUI, int fcntDown, int fcntUp, string gatewayId)
        {
            Logger.Log(devEUI, $"syncing FCntDown for multigateway", Logger.LoggingLevel.Info);

            var client = this.serviceFacadeHttpClientProvider.GetHttpClient();
            var url = $"{this.URL}NextFCntDown?code={this.AuthCode}&DevEUI={devEUI}&FCntDown={fcntDown}&FCntUp={fcntUp}&GatewayId={gatewayId}";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Logger.Log(devEUI, $"error calling the NextFCntDown function, check the function log. {response.ReasonPhrase}", Logger.LoggingLevel.Error);
                return 0;

            }

            string fcntDownString = await response.Content.ReadAsStringAsync();

            if (ushort.TryParse(fcntDownString, out var newFCntDown))
                return newFCntDown;

            return 0;
        }


        public override async Task<bool> ABPFcntCacheResetAsync(string devEUI)
        {
            Logger.Log(devEUI, $"ABP FCnt cache reset for multigateway", Logger.LoggingLevel.Info);
            var client = this.serviceFacadeHttpClientProvider.GetHttpClient();
            var url = $"{this.URL}NextFCntDown?code={this.AuthCode}&DevEUI={devEUI}&ABPFcntCacheReset=true";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Logger.Log(devEUI, $"error calling the NextFCntDown function, check the function log, {response.ReasonPhrase}", Logger.LoggingLevel.Error);
                return false;
            }

            return true;
        }

      


        public override async Task<SearchDevicesResult> SearchDevicesAsync(string gatewayId = null, string devAddr = null, string devEUI = null, string appEUI = null, string devNonce = null)
        {
            var client = this.serviceFacadeHttpClientProvider.GetHttpClient();
            var url = new StringBuilder();
            url.Append(this.URL)
                .Append("GetDevice?code=")
                .Append(this.AuthCode);

            if (!string.IsNullOrEmpty(gatewayId))
            {   
                url.Append("&GatewayId=")
                    .Append(gatewayId);
            }

            if (!string.IsNullOrEmpty(devAddr))
            {
                url.Append("&DevAddr=")
                    .Append(devAddr);
            }

            if (!string.IsNullOrEmpty(devEUI))
            {
                url.Append("&DevEUI=")
                    .Append(devEUI);
            }

            if (!string.IsNullOrEmpty(appEUI))
            {
                url.Append("&AppEUI=")
                    .Append(appEUI);
            }

            if (!string.IsNullOrEmpty(devNonce))
            {
                url.Append("&DevNonce=")
                    .Append(devNonce);
            }


            var response = await client.GetAsync(url.ToString());
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var badReqResult = await response.Content.ReadAsStringAsync();

                    if (!String.IsNullOrEmpty(badReqResult) && badReqResult == "UsedDevNonce")
                    {
                        Logger.Log(devEUI ?? string.Empty, $"DevNonce already used by this device", Logger.LoggingLevel.Info);
                        return new SearchDevicesResult
                        {
                            IsDevNonceAlreadyUsed = true,
                        };
                    }
                }
                
                Logger.Log(devAddr, $"error calling façade api: {response.ReasonPhrase} check the azure function log", Logger.LoggingLevel.Error);

                // TODO: FBE check if we return null or throw exception
                return new SearchDevicesResult();
            }

            
            var result = await response.Content.ReadAsStringAsync();
            var devices = ((List<IoTHubDeviceInfo>)JsonConvert.DeserializeObject(result, typeof(List<IoTHubDeviceInfo>)));
            return new SearchDevicesResult(devices);
        }
    }
}
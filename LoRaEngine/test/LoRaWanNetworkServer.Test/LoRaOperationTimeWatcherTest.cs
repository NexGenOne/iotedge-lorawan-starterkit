//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using LoRaTools.Regions;
using LoRaWan.NetworkServer;
using LoRaWan.NetworkServer.V2;
using LoRaWan.Shared;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LoRaWan.NetworkServer.Test
{
    // Tests of the LoRa Operation time watcher
    public class LoRaOperationTimeWatcherTest
    {
        [Fact]
        public void After_One_Second_Join_First_Window_Should_Be_Greater_Than_3sec()
        {
            var target = new LoRaOperationTimeWatcher(RegionFactory.CreateEU868Region(), DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(1)));
            var actual = target.GetRemainingTimeToJoinAcceptFirstWindow();
            Assert.InRange(actual.TotalMilliseconds, 3500, 5000);
        }

        [Fact]
        public void After_5_Seconds_Join_First_Window_Should_Be_Negative()
        {
            var target = new LoRaOperationTimeWatcher(RegionFactory.CreateEU868Region(), DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(5)));
            var actual = target.GetRemainingTimeToJoinAcceptFirstWindow();
            Assert.True(actual.TotalMilliseconds < 0, $"First window is over, value should be negative");
        }

        [Fact]
        public void After_3_Seconds_Should_Be_In_Time_For_Join()
        {
            var target = new LoRaOperationTimeWatcher(RegionFactory.CreateEU868Region(), DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(3)));
            Assert.True(target.InTimeForJoinAccept());
        }

        [Fact]
        public void After_5_Seconds_Should_Be_In_Time_For_Join()
        {
            var target = new LoRaOperationTimeWatcher(RegionFactory.CreateEU868Region(), DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(5)));
            Assert.True(target.InTimeForJoinAccept());
        }

        [Fact]
        public void After_6_Seconds_Should_Not_Be_In_Time_For_Join()
        {
            var target = new LoRaOperationTimeWatcher(RegionFactory.CreateEU868Region(), DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(6)));
            Assert.False(target.InTimeForJoinAccept());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(950)]
        public void When_In_Time_For_First_Window_But_Device_Wants_Always_Seconds_Should_Resolve_Window_2(int delayInMs)
        {
            var target = new LoRaOperationTimeWatcher(RegionFactory.CreateEU868Region(), DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMilliseconds(delayInMs)));
            var loRaDevice = new LoRaDevice("31312", "312321321", null)
            {
                AlwaysUseSecondWindow = true,
            };

            Assert.Equal(2, target.ResolveReceiveWindowToUse(loRaDevice));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(950)]
        public void When_In_Time_For_First_Window_Should_Resolve_Window_1(int delayInMs)
        {
            var target = new LoRaOperationTimeWatcher(RegionFactory.CreateEU868Region(), DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMilliseconds(delayInMs)));
            var loRaDevice = new LoRaDevice("31312", "312321321", null);

            Assert.Equal(1, target.ResolveReceiveWindowToUse(loRaDevice));
        }

        [Theory]
        [InlineData(2000)]
        [InlineData(2950)]
        public void When_In_Time_For_Second_Window_Should_Resolve_Window_2(int delayInMs)
        {
            var target = new LoRaOperationTimeWatcher(RegionFactory.CreateEU868Region(), DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMilliseconds(delayInMs)));
            var loRaDevice = new LoRaDevice("31312", "312321321", null);

            Assert.Equal(2, target.ResolveReceiveWindowToUse(loRaDevice));
        }

        [Fact]
        public void When_Missed_Both_Windows_Should_Resolve_Window_0()
        {
            var target = new LoRaOperationTimeWatcher(RegionFactory.CreateEU868Region(), DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(4)));
            var loRaDevice = new LoRaDevice("31312", "312321321", null);

            Assert.Equal(0, target.ResolveReceiveWindowToUse(loRaDevice));
        }
    }
}
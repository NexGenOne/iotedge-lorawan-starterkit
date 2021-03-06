﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LoRaWan.IntegrationTest
{
    /// <summary>
    /// Helper class enabling logging in Integration Test
    /// When running the tests in Visual Studio Log does not output
    /// </summary>
    static class TestLogger
    {
        /// <summary>
        /// Logs
        /// </summary>
        /// <param name="text"></param>
        internal static void Log(string text)
        {
          // Log to diagnostics if a debbuger is attached
            if (Debugger.IsAttached)
            {
                System.Diagnostics.Debug.WriteLine(text);
            }
            else
            {
                Console.WriteLine(text);
            }
        }

    }
}

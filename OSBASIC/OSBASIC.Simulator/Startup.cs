using OpenSilver.Simulator;

using System;

namespace OSBASIC.Simulator
{
    internal static class Startup
    {
        [STAThread]
        static int Main(string[] args)
        {
            // Start a background initializer to host the emulator once the OpenSilver UI is ready
            OSBASIC.Simulator.Host.InitializeWhenReady();
            return SimulatorLauncher.Start(typeof(App));
        }
    }
}

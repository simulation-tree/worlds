using System;
using System.Threading;
using System.Threading.Tasks;
using Unmanaged;

namespace Simulation.Tests
{
    public abstract class SimulationTests : UnmanagedTests
    {
        private World world;
        private Simulator simulator;

        public World World => world;
        public Simulator Simulator => simulator;

        [SetUp]
        protected override void SetUp()
        {
            base.SetUp();
            world = new();
            simulator = new(world);
        }

        protected override void CleanUp()
        {
            simulator.Dispose();
            world.Dispose();
            base.CleanUp();
        }

        protected async Task Simulate(World world, CancellationToken cancellation)
        {
            TimeSpan delta = TimeSpan.FromSeconds(0.1f);
            Simulator.Update(delta);
            await Task.Delay(delta, cancellation).ConfigureAwait(false);
        }
    }
}
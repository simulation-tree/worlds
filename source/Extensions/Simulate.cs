using System.Threading.Tasks;
using System.Threading;

namespace Simulation
{
    /// <summary>
    /// Represents a function that intends to iterate on the given <paramref name="world"/>.
    /// </summary>
    public delegate Task Simulate(World world, CancellationToken cancellation);
}
using System.Threading;
using System.Threading.Tasks;

namespace Worlds
{
    /// <summary>
    /// Represents a function that intends to iterate on the given <paramref name="world"/>.
    /// </summary>
    public delegate Task Update(World world, CancellationToken cancellation);
}
using System.Runtime.InteropServices;

namespace Worlds
{
    /// <summary>
    /// Less frequently accessed information about an entity in a <see cref="World"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct SlotMetadata
    {
        /// <summary>
        /// Start and length of the references.
        /// </summary>
        public ulong referenceRange;

        /// <summary>
        /// The state of this entity.
        /// </summary>
        public SlotState state;

        /// <summary>
        /// Flags describing the contents of this slot.
        /// </summary>
        public SlotFlags flags;

        private readonly ushort padding1;
        private readonly uint padding2;
    }
}
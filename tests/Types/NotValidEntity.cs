#pragma warning disable T0002

using System;

namespace Worlds.Tests
{
    public readonly struct NotValidEntity : IEntity
    {
        private readonly byte aSingleByte;

        readonly void IEntity.Describe(ref Archetype archetype)
        {
        }

        readonly void IDisposable.Dispose()
        {
        }
    }
}
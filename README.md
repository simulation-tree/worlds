# Simulation
Library for implementing logic acting on data stored as _components_ and _arrays_, where both can be found through _entities_.
Entities themselves are stored in _worlds_, which can be serialized, deserialized and appended to other worlds at runtime.

### Initializing
To use this library, a source generated `TypeTable` must be invoked to register all component and array types:
```cs
private static void Main()
{
    RuntimeHelpers.RunClassConstructor(typeof(TypeTable).TypeHandle);
    Console.WriteLine($"Component types: {ComponentType.All.Count}");
    Console.WriteLine($"Array types: {ArrayType.All.Count}");
}
```

> If you'd like, registration of types can be done manually with the `Register<T>()` method in `ComponentType` and `ArrayType`.

### Storing values in components
```cs
using (World world = new())
{
    uint entity = world.CreateEntity();
    world.AddComponent(entity, new MyComponent(25));
}

public struct MyComponent(uint value)
{
    public uint value = value;
}
```
> Only 1 component of each type can be on an entity

### Storing multiple values with arrays
Unlike components, arrays offer a way to store multiple of the same type,
and can be resized:
```cs
Span<char> many = world.CreateArray<char>(entity, "Hello world");
Span<char> moreMany = world.ResizeArray<char>(entity, 5);
Assert.That(moreMany.ToString(), Is.EqualTo("Hello"));
```

### Fetching data and querying
Polling of components, and modifying them can be done through multiple ways.
The following examples ascend in efficiency:
```cs
uint sum;

void Do()
{
    foreach (uint entity in world.GetAll<MyComponent>())
    {
        //this approach suffers from having to fetch each component individually
        ref MyComponent component = ref world.GetComponentRef<MyComponent>(entity);
        component.value *= 2;
        sum += component.value;
    }
}
```

The next approach is just as simple, but more efficient due to operating in bulk. With `ref`
access to each wanted component:
```cs
uint sum;

void Do()
{
    //this approach suffers from causing GC and lambda capture rules
    uint thisSum = sum;
    world.ForEach((in uint entity, ref MyComponent component) =>
    {
        component.value *= 2;
        thisSum += component.value;
    });

    sum = thisSum;
}
```

This is the most efficient and manual approach, and doesn't require any extra allocations:
```cs
uint sum = 0;

void Do()
{
    //only downside here is having to read a lot of code, and communicating it
    Dictionary<uint, ComponentChunk> chunks = world.ComponentChunks;
    for (int i = 0; i < chunks.Count; i++)
    {
        int key = chunks.Keys[i];
        ComponentChunk chunk = chunks[key];
        if (chunk.ContainsTypes(typesSpan))
        {
            List<uint> entities = chunk.Entities;
            for (uint e = 0; e < entities.Count; e++)
            {
                uint entity = entities[e];
                ref MyComponent component = ref chunk.GetComponentRef<MyComponent>(e);
                component.value *= 2;
                sum += component.value;
            }
        }
    }
}
```

Last is the `ComponentQuery` approach, which buffers found components for later iteration with
`ref` access to each component. It's `Update` method is used to fill its internals with the
latest view of the world:
```cs
uint sum = 0;
ComponentQuery<MyComponent> query;

void Do()
{
    //compared to most efficient approach, it only suffers from having to create an object
    query.Update(world);
    foreach (var x in query)
    {
        uint entity = x.entity;
        ref MyComponent component = ref x.Component1;
        component.value *= 2;
        sum += component.value;
    }
}
```

### Relationship references to other entities
Components with `uint` values that are _meant_ to reference other entities will be
susceptible to pointing to the wrong entity when worlds are appended (drift). Because the
value represents a position that may already be occupied by another entity during loading.

This is solved by storing an `rint` value that indexes to entity references stored relatively.
Then when worlds are then appended or loaded, the entities that they are meant to point to can
shift together as they're added, preserving the relationship.

```cs
public struct MyComponent(rint entityReference)
{
    public rint entityReference = entityReference;
}

using World dummyWorld = new();
uint firstEntity = dummyWorld.CreateEntity();
uint secondEntity = dummyWorld.CreateEntity();
rint entityReference = dummyWorld.AddReference(firstEntity, secondEntity);
dummyWorld.AddComponent(firstEntity, new MyComponent(entityReference));

//after appending, find the original first entity and its referenced second entity
world.Append(dummyWorld);
world.TryGetFirst(out uint oldFirstEntity, out MyComponent component);
uint oldSecondEntity = world.GetReference(oldFirstEntity, component.entityReference);
```

### Forming entity types
A commonly reused pattern with components is to formalize them into types, where the
type is qualified by components present on the entity. For example: if an entity
contains an `IsPlayer` then its a player entity. This design is supported through the
`IEntity` interface and its required `Definition` property:
```cs
public struct IsPlayer(FixedString name)
{
    public FixedString name = name;
}

public readonly struct Player : IEntity
{
    public readonly Entity entity;

    public readonly ref FixedString Name => ref entity.GetComponentRef<IsPlayer>().name;

    readonly uint IEntity.Value => entity.value;
    readonly World IEntity.World => entity.world;
    readonly Definition IEntity.Definition => new Definition().AddComponentType<IsPlayer>();

    public Player(World world, FixedString name)
    {
        this.entity = new(world);
        entity.AddComponent(new IsPlayer(name));
    }
}

//creating a player using its type's constructor
Player player = new(world, "unnamed");
```
> `IEntity` types are expected to be only big enough to store an `Entity` field,
or `uint`+`World` fields.

These types can then be used to transform or interpret existing entities:
```cs
//creating an entity, and making it into a player
Player player = new Entity(world).Become<Player>();
player.Name = "unnamed";

//creating an entity, manually adding the components, then reinterpreting
uint anotherEntity = world.CreateEntity();
world.AddComponent(anotherEntity, new IsPlayer("unnamed"));
Player anotherPlayer = new Entity(world, anotherEntity).As<Player>();
```

### Simulators, programs, and systems
`Simulator` instances contain systems, and run through `Program` entities.
Each of these programs gets its own world instance created separate from the simulator.
And every system added to the simulator will be initialized, updated and finalized with
both the simulator, and every program world:
```cs
uint returnCode;
using (World world = new())
{
    using (Simulator simulator = new(world))
    {
        simulator.AddSystem<ExampleSystem>();
        using (Program program = Program.Create<ExampleProgram>(world))
        {
            DateTime lastTime = DateTime.UtcNow;
            do
            {
                DateTime now = DateTime.UtcNow;
                TimeSpan delta = now - lastTime;
                lastTime = now;

                simulator.Update(delta);
            }
            while (!program.IsFinished(out returnCode));
        }

        simulator.RemoveSystem<ExampleSystem>();
    }
}

return (int)returnCode;
```

Each program's update function is expected to return a code, where 0 meaning
it should continue forward and any other value means exit:
```cs
public unsafe readonly struct ExampleProgram : IProgram
{
    private readonly DateTime startTime;

    readonly StartFunction IProgram.Start => new(&Start);
    readonly UpdateFunction IProgram.Update => new(&Update);
    readonly FinishFunction IProgram.Finish => new(&Finish);

    private ExampleProgram(DateTime startTime) 
    {
        this.startTime = startTime;
    }

    [UnmanagedCallersOnly]
    private static void Start(Simulator simulator, Allocation allocation, World world)
    {
        allocation.Write(new ExampleProgram(DateTime.UtcNow));
    }

    [UnmanagedCallersOnly]
    private static uint Update(Simulator simulator, Allocation allocation, World world, TimeSpan delta)
    {
        ref ExampleProgram program = ref allocation.Read<ExampleProgram>();
        if (DateTime.UtcNow - program.startTime > TimeSpan.FromSeconds(5))
        {
            return 1;
        }

        return 0;
    }

    [UnmanagedCallersOnly]
    private static void Finish(Simulator simulator, Allocation allocation, World world, uint returnCode)
    {
        ref ExampleProgram program = ref allocation.Read<ExampleProgram>();
    }
}
```
> The size of the allocation that is given is the size of this program type. In this example,
the start function is intentionally overwriting the value in order to respect the read only field.

> Program allocations are still accessible through the simulator after they have finished.

Each system that is added to the simulator is given access to the simulator itself,
and either the simulator world, or some program world:
```cs
public unsafe readonly struct ExampleSystem : ISystem
{
    readonly InitializeFunction ISystem.Initialize => new(&Initialize);
    readonly IterateFunction ISystem.Iterate => new(&Iterate);
    readonly FinalizeFunction ISystem.Finalize => new(&Finalize);

    [UnmanagedCallersOnly]
    private static void Initialize(SystemContainer container, World world)
    {
        if (container.World == world)
        {
            ref SimpleSystem system = ref container.Read<SimpleSystem>();
            Entity firstEntity = new(world);
            firstEntity.AddComponent(0u);
        }
    }

    [UnmanagedCallersOnly]
    private static void Iterate(SystemContainer container, World world, TimeSpan delta)
    {
        if (container.World == world)
        {
            ref uint firstEntityValue = ref world.GetComponentRef<uint>(1);
            firstEntityValue++;
        }
    }

    [UnmanagedCallersOnly]
    private static void Finalize(SystemContainer container, World world)
    {
        if (container.World == world)
        {
            ref uint firstEntityValue = ref world.GetComponentRef<uint>(1);
            firstEntityValue *= 10;
        }
    }
}
```
> Checking if the system's world is the given world is a way to run these functions
only once within a simulator.

### Serialization and deserialization
Serializing a world to bytes is simple:
```cs
using World world = new();
Entity entity = new(world);
entity.AddComponent(25f);
entity.AddComponent(true);
world.CreateArray<char>(entity, "Hello world");

using BinaryWriter writer = new();
writer.WriteObject(world);
USpan<byte> bytes = writer.GetBytes();
```

But for deserializing, because component and array types are not deterministic (due to the order
that they're registered with, and which are used), deserialization requires functions for
remapping the saved type to the expected:
```cs
World.SerializationContext.GetComponentType = (fullTypeName) =>
{
    if (fullTypeName.SequenceEqual(typeof(float).FullName.AsSpan()))
    {
        return ComponentType.Get<float>();
    }
    else if (fullTypeName.SequenceEqual(typeof(bool).FullName.AsSpan()))
    {
        return ComponentType.Get<bool>();
    }

    throw new Exception($"Unknown type {fullTypeName.ToString()}");
};

World.SerializationContext.GetArrayType = (fullTypeName) =>
{
    if (fullTypeName.SequenceEqual(typeof(char).FullName.AsSpan()))
    {
        return ArrayType.Get<char>();
    }

    throw new Exception($"Unknown type {fullTypeName.ToString()}");
};

using BinaryReader reader = new(bytes);
World deserializedWorld = reader.ReadObject<World>();
anotherWorld.Append(deserializedWorld);
```

### Contributing and Design
This library implements the "[entity-component-system](https://en.wikipedia.org/wiki/Entity_component_system)" pattern of the "archetype" variety.
Created for building programs of whatever kind, with an open door to the author for efficiency.

Contributions to this are welcome.
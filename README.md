# Worlds
Library for implementing efficient storage of data as _components_ and _arrays_, where both can be found through _entities_.
Entities themselves are stored within these _worlds_, which can be serialized, deserialized, and appended to other worlds at runtime.

### Initializing
To use this library, all of the component and array types that will be used need to be registered.
This is done by decorating them with either `[Component]` or `[Array]` (or both), and then calling
the type table's constructor:
```cs
private static void Main()
{
    RuntimeHelpers.RunClassConstructor(typeof(TypeTable).TypeHandle);
    ArrayType.Register<char>();
}

[Component]
public struct MyComponent(uint value)
{
    public uint value = value;
}
```

If the attributes aren't present, or the type table isn't initialized, then each type needs to
be registered manually:
```cs
private static void Main()
{
    ComponentType.Register<MyComponent>();
    ComponentType.Register<IsPlayer>();
    ArrayType.Register<char>();
}
```

> If a type isn't registered, an exception will be thrown when trying to use it.

### Storing values in components
```cs
using (World world = new())
{
    uint entity = world.CreateEntity();
    world.AddComponent(entity, new MyComponent(25));
}
```
> Only 1 component of each type can be on an entity

### Storing multiple values with arrays
Unlike components, arrays offer a way to store multiple of the same type,
and can be resized:
```cs
Span<char> many = world.CreateArray(entity, "Hello world".AsSpan());
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
    ComponentType type = ComponentType.Get<MyComponent>();
    for (int i = 0; i < chunks.Count; i++)
    {
        int key = chunks.Keys[i];
        ComponentChunk chunk = chunks[key];
        if (chunk.ContainsType(type))
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
[Component]
public struct MyReference(rint entityReference)
{
    public rint entityReference = entityReference;
}

using World dummyWorld = new();
uint firstEntity = dummyWorld.CreateEntity();
uint secondEntity = dummyWorld.CreateEntity();
rint entityReference = dummyWorld.AddReference(firstEntity, secondEntity);
dummyWorld.AddComponent(firstEntity, new MyReference(entityReference));

//after appending, find the original first entity and its referenced second entity
world.Append(dummyWorld);
world.TryGetFirst(out uint oldFirstEntity, out MyReference component);
uint oldSecondEntity = world.GetReference(oldFirstEntity, component.entityReference);
```

### Forming entity types
A commonly reused pattern with components is to formalize them into types, where the
type is qualified by components present on the entity. For example: if an entity
contains an `IsPlayer` then its a player entity. This design is supported through the
`IEntity` interface and its required `Definition` property:
```cs
[Component]
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

### Serialization and deserialization
Serializing a world to bytes is simple:
```cs
ComponentType.Register<float>();
ComponentType.Register<bool>();
ArrayType.Register<char>();

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

# Worlds
Library for implementing efficient storage of data as _components_ and _arrays_, where both can be found through _entities_.
Entities themselves are stored within these _worlds_, which can be serialized, deserialized, and appended to other worlds at runtime.

### Initializing
To use this library, all of the component and array types that will be used need to be registered ahead of time.
This is done by decorating them with either `[Component]` or `[Array]` (or both), and utilizing the `TypeLayoutRegistry` class
to register the layouts first. And then creating a schema with `SchemaRegistry.Get()` for each world to use:
```cs
private static void Main()
{
    TypeLayoutRegistry.RegisterAll();
    Schema schema = SchemaRegistry.Get();
    using World world = new(schema);
    //...
}

[Component]
public struct MyComponent(uint value)
{
    public uint value = value;
}
```

If the attributes aren't present, or the type layouts aren't registered, then each type needs to
be registered with both `TypeLayout` and `Schema`:
```cs
private static void Main()
{
    TypeLayout.Register<MyComponent>("MyComponent");
    TypeLayout.Register<IsPlayer>("IsPlayer");
    TypeLayout.Register<MyReference>("MyReference");
    TypeLayout.Register<char>("char");
    Schema schema = SchemaRegistry.Get();
    schema.RegisterComponent<MyComponent>();
    schema.RegisterComponent<IsPlayer>();
    schema.RegisterComponent<MyReference>();
    schema.RegisterArrayElement<char>();
    using World world = new(schema);
    //...
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

### Storing multiple values with arrays
Unlike components, arrays offer a way to store multiple of the same type,
and can be resized:
```cs
Span<char> many = world.CreateArray(entity, "Hello world".AsSpan());
Span<char> moreMany = world.ResizeArray<char>(entity, 5);
Assert.That(moreMany.ToString(), Is.EqualTo("Hello"));
```

### Fetching data and querying
Polling of components, and modifying them can be done through a few different ways:
```cs
uint sum;

void Do()
{
    foreach (uint entity in world.GetAll<MyComponent>())
    {
        //this approach suffers from having to fetch each component individually
        ref MyComponent component = ref world.GetComponent<MyComponent>(entity);
        component.value *= 2;
        sum += component.value;
    }
}
```

This approach is the most efficient, but is manual:
```cs
uint sum = 0;

void Do()
{
    //only downside here is having to read a lot of code
    Dictionary<Definition, Chunk> chunks = world.Chunks;
    ComponentType componentType = world.Schema.GetComponent<MyComponent>();
    foreach (Definition key in chunks.Keys)
    {
        if (key.ComponentTypes == componentType)
        {
            Chunk chunk = chunks[key];
            USpan<uint> entities = chunk.Entities;
            USpan<MyComponent> components = chunk.GetComponents<MyComponent>();
            for (uint e = 0; e < entities.Length; e++)
            {
                uint entity = entities[e];
                ref MyComponent component = ref components[e];
                component.value *= 2;
                sum += component.value;
            }
        }
    }
}
```

Last is the `ComponentQuery` approach, relying on a `foreach` statement to iterate over
found components with `ref` access to each:
```cs
uint sum = 0;

void Do()
{
    //a little slower than manually iterating, but more readable
    ComponentQuery<MyComponent> query = new(world);
    foreach (var x in query)
    {
        uint entity = x.entity;
        ref MyComponent component = ref x.component1;
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

using World dummyWorld = new(SchemaRegistry.Get());
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

    public readonly ref FixedString Name => ref entity.GetComponent<IsPlayer>().name;

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
> The layout of an `IEntity` type is expected to be match `uint`+`World`.

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
using World prefabWorld = new(SchemaRegistry.Get());
Entity entity = new(prefabWorld);
entity.AddComponent(new MyComponent(1337));
prefabWorld.CreateArray<char>(entity, "Hello world");

using BinaryWriter writer = new();
writer.WriteObject(prefabWorld);
USpan<byte> bytes = writer.GetBytes();

using BinaryReader reader = new(bytes);
using World deserializedWorld = reader.ReadObject<World>();
using World anotherWorld = new(SchemaRegistry.Get());
anotherWorld.Append(deserializedWorld);
```

### Contributing and Design
This library implements the "[entity-component-system](https://en.wikipedia.org/wiki/Entity_component_system)" pattern of the "archetype" variety.
Created for building programs of whatever kind, with an open door to the author when targeting efficiency.

Contributions to this are welcome.

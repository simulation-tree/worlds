# Worlds

[![Test](https://github.com/simulation-tree/worlds/actions/workflows/test.yml/badge.svg)](https://github.com/simulation-tree/worlds/actions/workflows/test.yml)

Library for implementing data as _components_, _arrays_, and _tags_, found on _entities_.

Entities themselves are stored within these _worlds_, which can be serialized, deserialized, and appended to other worlds at runtime.

### Creating worlds

All worlds contain a `Schema`, describing which types are possible to use.
They can be loaded after a world is created, or by passing one to the constructor:
```cs
private static void Main()
{
    TypeRegistryLoader.Load();

    Schema schema = new();
    schema.RegisterComponent<float>();
    schema.RegisterComponent<int>();
    schema.RegisterComponent<Fruit>();

    using World world = new(schema);

    uint entity = world.CreateEntity();
    world.AddComponent(entity, 3.14f);
    world.AddComponent(entity, 1337);
    world.AddComponent(entity, new Fruit(25));
}

public struct Fruit(uint value)
{
    public uint value = value;
}
```
> The `TypeRegistryLoader` is part of the [`types`](https://github.com/simulation-tree/types) project and it initializes metadata for all types.

### Schema loader

Included is a generator for a `SchemaLoader` type available only to projects with an 
entry point. It ensures that all mentioned types are registered, saving the need
to manually register them:
```cs
private static void Main()
{
    TypeRegistryLoader.Load();
    Schema schema = SchemaLoader.Get();
    using World world = new(schema);

    uint entity = world.CreateEntity();
    world.AddComponent(entity, 3.14f);
    world.AddComponent(entity, 1337);
    world.AddComponent(entity, new Fruit(25));
}
```

### Storing values in components

```cs
using (World world = new())
{
    uint entity = world.CreateEntity();
    world.AddComponent(entity, new Fruit(25));
}
```

### Storing multiple values with arrays

Unlike components, arrays offer a way to store multiple of the same type,
and can be resized:
```cs
USpan<char> many = world.CreateArray(entity, "Hello world".AsSpan());
USpan<char> moreMany = world.ResizeArray<char>(entity, 5);
Assert.That(moreMany.ToString(), Is.EqualTo("Hello"));
```

### Fetching data and querying

Polling of components, and modifying them can be done through a few different ways:
```cs
uint sum;

void Do()
{
    foreach (uint entity in world.GetAllContaining<Fruit>())
    {
        //this approach suffers from having to fetch each component individually
        ref Fruit component = ref world.GetComponent<Fruit>(entity);
        component.value *= 2;
        sum += component.value;
    }
}
```

This approach is the most efficient and quickest:
```cs
uint sum = 0;

void Do()
{
    //only downside here is having to read a lot of code
    ComponentType componentType = world.Schema.GetComponent<Fruit>();
    foreach (Chunk chunk in world.Chunks)
    {
        if (chunk.Contains(componentType))
        {
            USpan<Fruit> components = chunk.GetComponents<Fruit>(componentType);
            foreach (uint entity in chunk.Entities)
            {
                ref Fruit component = ref components[entity];
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
    ComponentQuery<Fruit> query = new(world);
    foreach (var x in query)
    {
        uint entity = x.entity;
        ref Fruit component = ref x.component1;
        component.value *= 2;
        sum += component.value;
    }
}
```

### Tagging entities

Entities can be tagged with tag types:
```cs
public struct IsThing
{
}

uint entity = world.CreateEntity();
world.AddTag<IsThing>(entity);

Assert.That(world.Contains<IsThing>(entity), Is.True);
```

### Relationship references to other entities

Components with `uint` values that are _meant_ to reference other entities will be
susceptible to drift after serialization. This is because the entity value represents
a position, that may be occupied by another existing entity.

This is solved by storing the references locally and accessing with an `rint` index.
Then when worlds are appended to another world, the referenced entities can shift together
as they're added, preserving the relationship.

```cs
public struct MyReference(rint entityReference)
{
    public rint entityReference = entityReference;
}

using World dummyWorld = new(SchemaLoader.Get());
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

A commonly reused pattern with components is to formalize them into argued objects, where the
type is qualified by the data present on the entity. For example, if an entity
contains a `PlayerName`, then its a player entity. This design is supported with the
`IEntity` interface and its required `Describe()` method:
```cs
public struct PlayerName(FixedString name)
{
    public FixedString name = name;
}

public readonly partial struct Player : IEntity
{
    public readonly ref FixedString Name => ref GetComponent<PlayerName>().name;

    readonly void IEntity.Describe(ref Archetype archetype)
    {
        archetype.AddComponentType<PlayerName>();    
    }

    public Player(World world, FixedString name)
    {
        this.world = world;
        value = world.CreateEntity(new PlayerName(name));
    }
}

//creating a player using its type's constructor
Player player = new(world, "unnamed");
```
> Entity types that are partial will have all of the API available

These types can then be used to transform or interpret existing entities:
```cs
//creating an entity, and making it into a player
Entity supposedPlayer = new(world);
supposedPlayer.Become<Player>();

if (!supposedPlayer.Is<Player>())
{
    throw new InvalidOperationException($"Entity `{supposedPlayer}` was expected to be a player");
}

Player player = supposedPlayer.As<Player>();
player.Name = "New name";
```

### Serialization and appending

Serializing a world to bytes and then appending to another world:
```cs
Schema schema = SchemaLoader.Get();
using World prefabWorld = new(schema);
Entity entity = new(prefabWorld);
entity.AddComponent(new Fruit(1337));
entity.CreateArray<char>("Hello world".AsSpan());

using BinaryWriter writer = new();
writer.WriteObject(prefabWorld);
USpan<byte> bytes = writer.AsSpan();

using BinaryReader reader = new(bytes);
using World loadedWorld = reader.ReadObject<World>();
using World anotherWorld = new(schema);
anotherWorld.Append(loadedWorld);
```

### Processing deserialized schemas

When worlds are serialized they contain the schema that was used. Together
with the original `TypeLayout` values. Allowing for them to be processed
in different assemblies, and routing found types into available ones:
```cs
using World loadedWorld = World.Deserialize(reader, Process);

static TypeLayout Process(TypeLayout type, DataType.Kind dataType)
{
    if (type.Is<Fruit>())
    {
        //change the type to uint
        return TypeRegistry.Get<uint>();
    }
    else
    {
        return type;
    }
}
```

### Contributing and design

This library implements the "[entity-component-system](https://en.wikipedia.org/wiki/Entity_component_system)" pattern
of the "archetype" variety. Created for building programs of whatever kind, with an open door to the author
when targeting runtime efficiency.

Contributions to this are welcome.

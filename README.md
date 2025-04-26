# Worlds

[![Test](https://github.com/simulation-tree/worlds/actions/workflows/test.yml/badge.svg)](https://github.com/simulation-tree/worlds/actions/workflows/test.yml)

Library for implementing data as _components_, _arrays_, and _tags_, found on _entities_.

Entities are stored within these _worlds_, which can then be serialized, deserialized, and
appended to other worlds at runtime.

### Creating worlds

`World`s contain a `Schema`, describing which types are possible to use with it.
When using a type that isn't registered while interacting with a world,
errors will be thrown in debug mode. All types that are used must be registered.

Schemas can be loaded after a world is created, or by passing one to the constructor:
```cs
private static void Main()
{
    TypeRegistryLoader.Load();              //register type metadata

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

Included is a generator for a `SchemaLoader` type for projects that have an entry point.
It ensures that all mentioned types with a world are registered. Saving the effort for
manually registering them, and making startup easier:
```cs
private static void Main()
{
    TypeRegistryLoader.Load();              //register type metadata
    Schema schema = SchemaLoader.Get();     //register components/arrays/tags
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
Values<char> many = world.CreateArray(entity, "Hello world".AsSpan());
many.Length = 5;
Assert.That(moreMany.AsSpan().ToString(), Is.EqualTo("Hello"));

many.AddRange(" there".AsSpan());
Assert.That(many.AsSpan().ToString(), Is.EqualTo("Hello there"));
```

### Tagging entities

Entities can be tagged with any type, and then queried for:
```cs
public struct IsThing
{
}

uint entity = world.CreateEntity();
world.AddTag<IsThing>(entity);

Assert.That(world.Contains<IsThing>(entity), Is.True);
```

### Fetching data and querying

Polling of components, and modifying them can be done through a few different ways.

**Manual**

This approach is the quickest:
```cs
uint sum = 0;

void Do()
{
    int componentType = world.Schema.GetComponentType<Fruit>();
    int tagType = world.Schema.GetTagType<IsThing>();
    foreach (Chunk chunk in world.Chunks)
    {
        if (chunk.Definition.ContainsComponent(componentType) && !chunk.Definition.ContainsTag(tagType))
        {
            Span<Fruit> components = chunk.GetComponents<Fruit>(componentType);
            ReadOnlySpan<uint> entities = chunk.Entities;
            for (int i = 0; i < entities.Length; i++)
            {
                uint entity = entities[i];
                ref Fruit component = ref components[i];
                component.value *= 2;
                sum += component.value;
            }
        }
    }
}
```

**ComponentQuery**

This approach is the next quicker, and requires way less code to write:
```cs
uint sum = 0;

void Do()
{
    ComponentQuery<Fruit> query = new(world);
    query.ExcludeTags<IsThing>();
    foreach (var x in query)
    {
        uint entity = x.entity;
        ref Fruit component = ref x.component1;
        component.value *= 2;
        sum += component.value;
    }
}
```

**Get methods**

Other approaches through extension methods like `GetAllContaining` don't lend themselves
to quicker runtimes, but are quicker to write:
```cs
uint sum;

void Do()
{
    foreach (uint entity in world.GetAllContaining<Fruit>())
    {
        if (world.ContainsTag<IsThing>(entity))
        {
            continue;
        }

        //this approach suffers from having to fetch each component individually
        ref Fruit component = ref world.GetComponent<Fruit>(entity);
        component.value *= 2;
        sum += component.value;
    }
}
```

### Relationship references to other entities

Components with `uint` values that are _meant_ to reference other entities will be
susceptible to drift after serialization. This is because the entity value represents
a position, that may be occupied by another existing entity.

This is solved by storing the references locally and accessing them with an `rint` index.
When worlds are appended to another world, those referenced entities can shift together
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

### The `Entity` wrapper

In addition to the original API, can also use `Entity` instances. Which wrap the
`uint` value for the entity and the `World` instance:
```cs
Entity entity = new(world);
entity.AddComponent(new Fruit(1337));

ref Fruit component = ref entity.GetComponent<Fruit>();
component.value *= 2;

Span<char> text = entity.CreateArray<char>("Hello world".AsSpan());
```

### Forming entity types

A commonly reused pattern with components is to formalize them into argued objects, where the
type is qualified by the data present on the entity. For example, if an entity
contains a `PlayerName`, then its a player entity. This design is supported with the
`IEntity` interface and its required `Describe()` method:
```cs
public struct PlayerName(ASCIIText32 name)
{
    public ASCIIText32 name = name;
}

public readonly partial struct Player : IEntity
{
    public readonly ref ASCIIText32 Name => ref GetComponent<PlayerName>().name;

    readonly void IEntity.Describe(ref Archetype archetype)
    {
        archetype.AddComponentType<PlayerName>();    
    }

    public Player(World world, ASCIIText32 name)
    {
        this.world = world;
        value = world.CreateEntity(new PlayerName(name));
    }
}

//creating a player using its type's constructor
Player player = new(world, "unnamed");
```
> Only entity types that are partial will have all of the world API available

These types can then be used to transform or interpret existing entities:
```cs
//creating an entity, and making it into a player
Entity supposedPlayer = new(world);
Assert.That(supposedPlayer.Is<Player>(), Is.False);
supposedPlayer.Become<Player>();
Assert.That(supposedPlayer.Is<Player>(), Is.True);

Player player = supposedPlayer.As<Player>();
Assert.That(player.IsCompliant, Is.True);
player.Name = "New name";
```

These entity types can be implicitly casted to `Entity`, and explicitly back:
```cs
Player player = new(world, "unnamed");
Entity entity = player;
player = entity.As<Player>();
```

### Serializing and appending

Each world instance is portable, and can be serialized and deserialized
in another executable:
```cs
Schema schema = SchemaLoader.Get();
using World prefabWorld = new(schema);
Entity entity = new(prefabWorld);
entity.AddComponent(new Fruit(1337));
entity.CreateArray<char>("Hello world".AsSpan());

using ByteWriter writer = new();
writer.WriteObject(prefabWorld);
ReadOnlySpan<byte> bytes = writer.AsSpan();

using ByteReader reader = new(bytes);
using World loadedWorld = World.Deserialize(reader);
using World anotherWorld = new(schema);
anotherWorld.Append(loadedWorld);
```

### Processing deserialized schemas

When worlds are serialized, they contain the original schema that was used. Storing
the original `TypeLayout` values for describing each component/array/tag type.
Allowing for them to be processed when loaded in a different executable, and rerouting types
to other types if the original isn't:
```cs
using World loadedWorld = World.Deserialize(reader, Process);

static TypeLayout Process(TypeLayout type, DataType.Kind dataType)
{
    if (type.Name.SequenceEquals("Fruit") && type.Size == sizeof(uint))
    {
        //Fruit type not in this project, change to uint
        return MetadataRegistry.GetType<uint>();
    }
    else
    {
        return type;
    }
}
```

### Contributing and design

This library implements the "[entity-component-system](https://en.wikipedia.org/wiki/Entity_component_system)" pattern
of the "archetype" variety. Created for building programs of whatever kind, with an open door for targeting
runtime efficiency. It's meant to be fast, though it's not quite there yet.

Contributions to this goal are welcome.
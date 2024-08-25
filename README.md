# Simulation
Simple library for implementing logic acting on data.

### Organizing data
Data is stored as _components_ and _arrays_, where both are found through an _entity_. Entities
are then stored in _worlds_, which can be serialized, deserialized and appended to other worlds:
```cs
using World world = new();
eint entity = world.CreateEntity();
world.AddComponent(entity, new MyComponent(25));

//component and arrays types are required to be true value types all the way down
public struct MyComponent(uint value)
{
    public uint value = value;
}

using BinaryWriter writer = new();
writer.WriteObject(world);
ReadOnlySpan<byte> worldBytes = writer.AsSpan();
using BinaryReader reader = new(worldBytes);
using World loadedWorld = reader.ReadObject<World>();
world.Append(loadedWorld); //duplicates data
```
> Only 1 component of each type can be on an entity

Arrays offer a way to store multiple of the same type, unlike components:
```cs
Span<char> many = world.CreateArray<char>(entity, "Hello world");
```

Both components and arrays can be fetched back from the entity directly:
```cs
ref MyComponent component = ref world.GetComponentRef<MyComponent>(entity);
component.value *= 2;
Assert.That(component.value, Is.EqualTo(50));

Span<char> many = world.GetArray<char>(entity);
Assert.That(many.ToString(), Is.EqualTo("Hello world"));
```

### Fetching data
Polling of components, and then modifying them can be done through multiple ways.
Simplest to write:
```cs
uint sum;

void Do()
{
    foreach (eint entity in world.GetAll<MyComponent>())
    {
        //this approach suffers from having to fetch each component individually
        ref MyComponent component = ref world.GetComponentRef<MyComponent>(entity);
        component.value *= 2;
        sum += component.value;
    }
}
```

The next approach is just as simple, but more efficient due to operating in bulk. With references
to each wanted component:
```cs
uint sum;

void Do()
{
    //downside is the lambda itself, and its rules for how field members are captured,
    //it can be remedied by first localizing them, then modifying inside the lambda,
    //then finally assigning it back at the end
    uint thisSum = sum;
    world.ForEach((in eint entity, ref MyComponent component) =>
    {
        component.value *= 2;
        thisSum += component.value;
    });

    sum = thisSum;
}
```

Even more efficiently without any allocations, is to manually iterate through the chunks of the components:
```cs
uint sum = 0;

void Do()
{
    UnmanagedDictionary<uint, ComponentChunk> chunks = world.ComponentChunks;
    for (int i = 0; i < chunks.Count; i++)
    {
        uint key = chunks.Keys[i];
        ComponentChunk chunk = chunks[key];
        if (chunk.ContainsTypes(typesSpan))
        {
            UnmanagedList<eint> entities = chunk.Entities;
            for (uint e = 0; e < entities.Count; e++)
            {
                //only downside here is a display of boilerplate
                eint entity = entities[e];
                ref MyComponent component = ref chunk.GetComponentRef<MyComponent>(e);
                component.value *= 2;
                sum += component.value;
            }
        }
    }
}
```

Last is the `Query` type, which buffers found components for later iteration with
references for each of the components. It's `Update` method is used to fill its internals with the
latest view of the world:
```cs
uint sum = 0;

void Do()
{
    //only downside i see is that its not "automatic" in the context of "i just want to iterate",
    //because `Update` must be called at least once (but thats also the benefit)
    using Query<MyComponent> query = new(world);
    query.Update();
    foreach (var x in query)
    {
        eint entity = x.entity;
        ref MyComponent component = ref x.Component1;
        component.value *= 2;
        sum += component.value;
    }
}
```

### Relationship references
Components with `eint` values that are _meant_ to reference other entities will be
susceptible to pointing to the wrong entity when the world is being appended to another.
Because the old original position may already be occupied by another entity.

Referencing is then solved by storing an `rint` that points to a local reference on
the referencing entity itself. Then when appending is performed, old relationships are
preserved (though the original `eint` may not be, so don't depend on them too hard):

```cs
public struct MyComponent(rint entityReference)
{
    public rint entityReference = entityReference;
}

using World dummyWorld = new();
eint firstEntity = dummyWorld.CreateEntity();
eint secondEntity = dummyWorld.CreateEntity();
rint entityReference = dummyWorld.AddReference(firstEntity, secondEntity);
dummyWorld.AddComponent(firstEntity, new MyComponent(entityReference));

//after appending, find the original first entity and its referenced second entity
world.Append(dummyWorld);
world.TryGetFirst(out eint oldFirstEntity, out MyComponent component);
eint oldSecondEntity = world.GetReference(oldFirstEntity, component.entityReference);
```

### Events and listeners
These allow for communicating between systems across assemblies. Events are first submitted,
then when `Poll` is called, it will will iterate through every event and call all listeners
in the order that they were submitted:
```cs
using Listener listener = world.Listen<MyEvent>(&ReceivedEvent);

world.Submit(new MyEvent(25));
world.Submit(new MyEvent(50));
world.Poll();

[UnmanagedCallersOnly]
static void ReceivedEvent(World world, Container message)
{
    MyEvent update = message.Read<MyEvent>();
    Console.WriteLine($"Received event: {update.data}");
}

public struct MyEvent(uint data)
{ 
    public uint data = data;
}
```

### Extending from `SystemBase`
Unmanaged listeners above require callbacks to be static. The included `SystemBase` type
implements event redirecting to instanced callbacks, allowing for a more traditional approach to
designing systems:
```cs
public class MySystem : SystemBase
{
    public MySystem(World world) : base(world)
    {
        Subscribe<MyEvent>(HandleMyEvent); //automatically unsubscribed when disposed
    }

    private void HandleMyEvent(MyEvent e)
    {
        Console.WriteLine($"data: {e.data}");
    }
}
```

### Contributing and Design
This library implements the "[entity-component-system](https://en.wikipedia.org/wiki/Entity_component_system)" pattern of the archetype variety. It's created to provide an open door
to authors for building programs of whatever kind, lightly, and efficiently.

Contributions to this are welcome.
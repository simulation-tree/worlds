# Simulation
Library for implementing logic acting on data stored as _components_ and _arrays_, where both can be found through _entities_.
Entities themselves are stored in _worlds_, which can be serialized, deserialized and appended to other worlds at runtime.

### Storing with components
```cs
using World world = new();
uint entity = world.CreateEntity();
world.AddComponent(entity, new MyComponent(25));

public struct MyComponent(uint value)
{
    public uint value = value;
}
```
> Only 1 component of each type can be on an entity

### Storing multiples with arrays
Unlike components, arrays offer a way to store multiple of the same type,
and can be resized:
```cs
Span<char> many = world.CreateArray<char>(entity, "Hello world");
Span<char> moreMany = world.ResizeArray<char>(entity, 5);
Assert.That(moreMany.ToString(), Is.EqualTo("Hello"));
```

### Fetching data
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

The next approach is just as simple, but more efficient due to operating in bulk. With references
to each wanted component:
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
    UnmanagedDictionary<uint, ComponentChunk> chunks = world.ComponentChunks;
    for (int i = 0; i < chunks.Count; i++)
    {
        uint key = chunks.Keys[i];
        ComponentChunk chunk = chunks[key];
        if (chunk.ContainsTypes(typesSpan))
        {
            UnmanagedList<uint> entities = chunk.Entities;
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

Last is the `Query` approach, which buffers found components for later iteration with
available references to each components. It's `Update` method is used to fill its internals with the
latest view of the world:
```cs
uint sum = 0;
Query<MyComponent> query;

void Do()
{
    //compared to most efficient approach, it only suffers from having to create an object
    query.Update();
    foreach (var x in query)
    {
        uint entity = x.entity;
        ref MyComponent component = ref x.Component1;
        component.value *= 2;
        sum += component.value;
    }
}
```

### Relationship references
Components with `uint` values that are _meant_ to reference other entities will be
susceptible to pointing to the wrong entity when worlds are appended. Because the value
represents a position that may already be occupied by another existing entity.

This is solved by storing an `rint` (from `AddReference()`) that points to a relative
position on the referencing entity. When worlds are then appended, the entities that they
would point to would shift together as they're added. Original `uint` values likely won't
be the same due to this, but the relationships are preserved.

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

### Static event messages and listeners
These allow for communicating between systems across assemblies. Messages are first submitted,
and then _polled_ for with the `Poll()` function. Which will invoke listeners subscribed to each
message type, in the order that they were submitted:
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

> Unsubscribing or removing a listener is done by disposing it

### Included `SystemBase` type
This included type implements redirecting of polled 
messages from the static context to instanced callbacks, allowing for a more traditional approach to writing code:
```cs
public class MySystem : SystemBase
{
    public MySystem(World world) : base(world)
    {
        //automatically unsubscribed when disposed
        Subscribe<MyEvent>(HandleMyEvent);
    }

    private void HandleMyEvent(MyEvent e)
    {
        //non static!
        Console.WriteLine($"data: {e.data}");
    }
}
```

### Contributing and Design
This library implements the "[entity-component-system](https://en.wikipedia.org/wiki/Entity_component_system)" pattern of the "archetype" variety. Created for building programs of whatever kind,
with an open door to the author for efficiency while using C#.

Contributions to this are welcome.
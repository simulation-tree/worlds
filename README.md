# Simulation
Simple library for implementing logic on data.

### Organizing data
Data is available as _components_ and _lists_, where both are found through an _entity_. Where only
one component of the same type is allowed:
```cs
using World world = new();
eint entity = world.CreateEntity();
world.AddComponent(entity, new MyComponent(25));

//component and list types are required to be value types all the way down
public struct MyComponent(uint value)
{
    public uint value = value;
}
```

Lists offer a way to store multiple of the same type.
```cs
UnmanagedList<char> many = world.CreateCollection<char>(entity);
many.AddRange("Hello world".AsSpan());
```

Both the components and lists can then be fetched back from the entity:
```cs
ref MyComponent component = ref world.GetComponentRef<MyComponent>(entity);
Assert.That(component.value, Is.EqualTo(25));

UnmanagedList<char> many = world.GetCollection<char>(entity);
Assert.That(many.AsSpan().ToString(), Is.EqualTo("Hello world"));
```

### Acting upon data
Polling of components, and then modifying them can be done through multiple ways. Simplest to write:
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

The next approach is just as simple, and more efficient due to operating in bulk. That also
allows for references to each wanted component:
```cs
uint sum;

void Do()
{
    //downside is the lambda itself, and its rules for what can be done inside them,
    //for editing fields, it can be remedied by first localizing it, then modifying it
    //inside the lambda, then finally assigning it back at the end
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

Lastly is the `Query` type, which buffers found components for later iteration with
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

### Events and listeners
Events are first submitted to `World`s, then when `Poll` is called which will iterate through
each event and call any listeners in the submitted order.
```cs
using Listener listener = world.Listen<MyEvent>(&ReceivedEvent);

world.Submit(new MyEvent(25));
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

<details>
  <summary>Barebones simulation machine example</summary>
    
The following is an example of a continous program that constantly dispatches an `Update`
until a `ShutdownSignal` event is submitted:
```cs
static bool stopped;

void Run()
{
    using (World world = new())
    {
        using Listener listener = world.Listen<ShutdownSignal>(&AskedToShutdown);

        //machine started, systems created here
        while (!stopped)
        {
            world.Submit(new Update());
            world.Poll();
        }

        //machine stopped, systems disposed here
    }

    [UnmanagedCallersOnly]
    static void AskedToShutdown(World world, Allocation message)
    {
        stopped = true;
    }
}

public struct ShutdownSignal { }
```

</details>

### Included SystemBase type
With unmanaged listeners it can be difficult to integrate with a codebase design that isn't static first.
It asks for the callbacks to be such, and unmanaged too. The included `SystemBase` type implements event
redirecting to action delegates, allowing for a more traditional approach to event handling:
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

### Contributing and Direction
This library is created to provide early definitions for building programs that _do_ upon data, lightly and efficiently.

Commonly referred to as the "[entity-component-system](https://en.wikipedia.org/wiki/Entity_component_system)" pattern.

Contributions to this are welcome.

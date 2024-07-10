# Simulation
Library for implementing logic on data.

### Systems and components
"Systems" and "components" can be thought of as the concept of verbs and nouns.
Where components must be defined as unmanaged `struct` types, and systems can be any code that
modifies components. These components are then attached onto entities, where one can store many but
not more than 1 of the same.
```cs
using World world = new();
EntityID entity = world.CreateEntity();
world.AddComponent(entity, new MyComponent(25));

public struct MyComponent(uint value)
{
    public uint value = value;
}
```

Polling of component data to modify it (a system/verb):
```cs
var query = world.Query<MyComponent>();
foreach (var x in query)
{
    EntityID entity = x.entity;
    ref MyComponent component = ref x.Component1;
    component.value++;
}
```

### Per entity collections
Each entity is able to contain lists of any kind and any capacity. This allows them to store more
than 1 of the same type of data:
```cs
EntityID textEntity = world.CreateEntity();
UnmanagedList<char> textChars = world.CreateCollection<char>(textEntity);
textChars.AddRange("Hello world");
```

Access time for this data isn't as quick as for components, polling of these isn't available
directly.

### Events and listeners
Events operate by first being submitted to `World` instances from any thread, and then calling the 
`Poll` method while on the main thread, with listeners added before.
```cs
using World world = new();
using Listener listener = world.Listen<MyEvent>(&ReceivedEvent);

world.Submit(new MyEvent(25));
world.Poll();

[UnmanagedCallersOnly]
private static void ReceivedEvent(World world, Container message)
{
    ref MyEvent update = ref message.AsRef<MyEvent>();
    Console.WriteLine($"Received event: {update.data}");
}

public struct MyEvent(uint data)
{ 
    public uint data = data;
}
```

### Examples
<details>
  <summary>Non static event listeners</summary>
    
With unmanaged listeners like above, it can be difficult to integrate with a codebase design
that isn't static first, as it asks for the callbacks to be such, and unmanaged too.
The following gets around this by making the static callback aware of what systems are interested.
```cs
//MyProgram.cs
public class MySystem : IDisposable
{
    private static readonly List<MySystem> systems = [];

    private readonly Listener listener;

    public unsafe MySystem(World world)
    {
        systems.Add(this);
        listener = world.Listen<MyEvent>(&StaticEvent);
    }

    public void Dispose()
    {
        listener.Dispose();
        systems.Remove(this);
    }

    private void HandleMyEvent(MyEvent e)
    {
        Console.WriteLine($"data: {e.data}");
    }

    [UnmanagedCallersOnly]
    private static void StaticEvent(World world, Container message)
    {
        MyEvent e = message.AsRef<MyEvent>();
        foreach (MySystem system in systems)
        {
            system.HandleMyEvent(e);
        }
    }
}
```

</details>

<details>
  <summary>Barebones bootstrap boilerplate</summary>
    
The following snippet is an example of a continous program that runs until a `Shutdown` event
is submitted, emerging a barebones game loop. On its own it doesn't perform anything other than
run forever, it requires a `Update` listeners to perform the remaining operations of the game:
```cs
//Program.cs
private static bool stopped;

using World world = new();
using Listener listener = world.Listen<Shutdown>(&AskedToShutdown);
while (!stopped)
{
    world.Submit(new Update());
    world.Poll();
}

[UnmanagedCallersOnly]
private static void AskedToShutdown(World world, Container message)
{
    stopped = true;
}
```

</details>

### Contributing and Direction
This library is created to provide the first layer for computer programs that aim to run
continously, like games and simulations, while taking advantage of what CPU's can do best.
Commonly referred to as the "[entity-component-system](https://en.wikipedia.org/wiki/Entity_component_system)" pattern.

Contributions to this are welcome.

# Game
Library for running logic on data through a `World` instance.

### Components and Systems
Components and systems form a union and can be closely thought of as verbs and nouns.
Components must be unmanaged structures, and there is no standardization for "systems", since any
style will work:
```cs
using World world = new();
EntityID entity = world.CreateEntity();
world.AddComponent(entity, new MyComponent(25));
world.QueryComponents((in EntityID entity, ref MyComponent value) =>
{
    value.value++;
});

public struct MyComponent(uint value)
{
    public uint value = value;
}
```

### Events and Listeners
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
    Console.WriteLine($"data: {update.data}");
}

public struct MyEvent(uint data)
{ 
    public uint data = data;
}
```

### Instance listeners
With unmanaged listeners it can be difficult to integrate with instance methods, as it asks for the
callbacks to be unmanaged static callers only. The following is an approach that can get around this
by making the static callback aware of what systems are interested.
```cs
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
The example above can then be composed into a `World` like so:
```cs
using World world = new();
using MySystem system = new(world);
```

### Running program setup
The following snippet is an example of a continous program that runs until a `Shutdown` event
is submitted, emerging a barebones game loop. On its own it doesn't perform anything other than
run forever, it requires a `Update` listeners to perform the remaining operations of the game:
```cs
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

### Contributing and Direction
This library is created to define a baseline layer for "programs" that often want to run
continously (like games or simulations), while taking the advantage of a CPU's ability to perform
more efficiently when data is laid out linearly. For this reason, the archetype component-system
pattern is at the root, and serves as a dependency for other projects.

Contributions to this are welcome.
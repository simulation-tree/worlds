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

### Events
Events operate by first being submitted to `World` instances from any thread, and then calling the 
`Poll` method while on the main thread (or the thread that created the instance).
```cs
using World world = new();
world.Listen<MyEvent>(&OnUpdate);

world.Submit(new MyEvent(25));
world.Poll();

[UnmanagedCallersOnly]
static void OnMyEvent(World world, Container message)
{
    ref MyEvent update = ref message.AsRef<MyEvent>();
    Console.WriteLine($"data: {update.data}");
}

public struct MyEvent(uint data)
{ 
    public uint data = data;
}
```

With unmanaged listeners it can be difficult to integrate with a design that encourages instance methods, as it asks for the callbacks to be unmanaged static callers only. The following is an
approach that can get around this design by making the static callback aware of what systems
are interested in the event.
```cs
public class MySystem : IDisposable
{
    private static readonly List<MySystem> systems = [];

    public unsafe MySystem(World world)
    {
        systems.Add(this);
        world.Listen<MyEvent>(&StaticEvent);
    }

    public void Dispose()
    {
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
### Virtual Machines
These contain `World` instances and are the shell of program logic, allowing additional
capabilities like 3D rendering to be written as individual objects. Their intended use
is to call `Update` to advance the state forward. Either as a single instruction or inside
a `while (vm.Update())` loop where the exit condition is the submission of a `Shutdown` event.

Example of a program that runs 1000 times and exits:
```cs
using (VirtualMachine vm = new())
{
    using (MyProgram myProgram = new())
    {
        vm.Add(myProgram);
        while (vm.Update()) { }
        vm.Remove(myProgram);

        Debug.WriteLine($"exited after {myProgram.iterations} iterations");
    }
}

public class MyProgram : IDisposable, IListener<Update>
{
    public uint iterations;

    public MyProgram()
    {
        Debug.WriteLine("program started");
    }

    public void Dispose()
    {
        Debug.WriteLine("program finished");
    }

    void IListener<Update>.Receive(World world, ref Update e)
    {
        iterations++;
        if (iterations > 1000)
        {
            world.SubmitEvent(new Shutdown());
        }
    }
}
```


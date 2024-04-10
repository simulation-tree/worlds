# Game
Library for running composable logic on data.

### Worlds
`World` instances provide an API to interact with entities, components
and events. These are present inside `VirtualMachine` instances as well.
```cs
using World world = new();
EntityID entity = world.CreateEntity();
world.AddComponent(entity, new MyComponent(25));
world.QueryComponents((in EntityID entity, ref MyComponent value) =>
{

});

public struct MyComponent(uint value)
{
    public uint value = value;
}
```

### Events
Dispatching events and listening to them as a feature exists. Submitting the
message is thread safe, but getting the listeners to invoke should occur from
the thread a world was created in (usually main thread):
```cs
using World world = new();
world.AddListener<Update>(OnUpdate);
world.SubmitEvent(new Update());
world.PollListeners();

void OnUpdate(ref Update update)
{

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


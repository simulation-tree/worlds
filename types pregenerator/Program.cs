using System;

if (args.Length == 0)
{
    Console.WriteLine("Expected 1 argument referring to generation mode:");
    Console.WriteLine("  0 = ComponentQuery<>");
    Console.WriteLine("  1 = Entity<>");
    return;
}

string firstInput = args[0];
if (int.TryParse(firstInput, out int mode))
{
    Console.WriteLine($"Generating mode {mode}");
    switch (mode)
    {
        case 0:
            ComponentQuery.Generate();
            break;
        case 1:
            Entity.Generate();
            break;
        default:
            Console.WriteLine($"Invalid generation mode {mode}");
            break;
    }
}
else
{
    Console.WriteLine($"Invalid generation mode {firstInput}, expected a number");
}
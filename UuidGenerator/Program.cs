using System;

class Program
{
    static void Main()
    {
        Guid randomUuid = Guid.NewGuid();
        Console.WriteLine($"Generated UUID v4: {randomUuid}");
    }
}
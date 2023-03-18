// See https://aka.ms/new-console-template for more information

using System;
using Monolith;

public class Program
{
    public static void Main()
    {
        var game = new Game();
        for (int i = 0; i < 1000; i++)
        {
            game.Start();
        }
        Console.WriteLine("Finished");
    }
}
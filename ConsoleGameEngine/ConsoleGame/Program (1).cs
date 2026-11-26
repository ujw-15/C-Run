using System;

namespace ConsoleGameEngineDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            BaseGameEngine engine = new RogueGameEngine();
            engine.Run();
        }
    }
}

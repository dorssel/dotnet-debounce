using System;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("UnitTestProject1")]

namespace ConsoleApp1
{
    static class Program
    {
        internal static void Main()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Hello World! Привет мир! Γειά σου Κόσμε!");
        }
    }
}

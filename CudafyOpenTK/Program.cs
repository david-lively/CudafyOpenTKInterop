using System;

namespace Common
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Cudafy OpenTK Interop Example");
            Console.WriteLine("See README.txt for details");

            using (var window = new RayCastWindow())
            {
                window.Run();
            }

        }
    }
}

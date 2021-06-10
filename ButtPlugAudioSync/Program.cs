using System;
using System.Threading.Tasks;

namespace ButtPlugAudioSync
{
    class Program
    {
        private static SoundCapture capture;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            capture = new SoundCapture();
            Console.CursorVisible = false;

            Task.Run(DoStuff);

            Console.ReadKey();
        }

        static async Task DoStuff()
        {
            while (true)
            {
                capture.ComputeData();

                await Task.Delay(10);
            }
        }
    }
}

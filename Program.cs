using System;

namespace numBlock
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (NumBlockGame game = new NumBlockGame())
            {
                game.Run();
            }
        }
    }
}


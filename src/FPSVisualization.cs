using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Diagnostics;

namespace Zpg.src
{
    // Třída pro vizualizaci a výpočet FPS (Frames Per Second)
    public class FPSVisualization
    {
        private int frameCount;       // Počet snímků za aktuální měřené období
        private int currentFps;       // Aktuální vypočtené FPS
        private double lastTime;      // Čas poslední aktualizace FPS
        private readonly Stopwatch timer = Stopwatch.StartNew(); // Stopwatch pro měření času

        // Volat každým snímkem pro aktualizaci počítadla FPS
        public void Update()
        {
            frameCount++; // zvýšení počtu snímků

            double currentTime = timer.Elapsed.TotalSeconds; // aktuální čas od spuštění

            // Každou sekundu aktualizuj aktuální FPS
            if (currentTime - lastTime >= 1.0)
            {
                // Vypočti FPS jako počet snímků za uplynulou sekundu
                currentFps = (int)Math.Round(frameCount / (currentTime - lastTime));

                frameCount = 0;      // resetuj počítadlo snímků
                lastTime = currentTime; // aktualizuj čas poslední aktualizace
            }
        }

        // Vrací aktuální FPS
        public int GetCurrentFps()
        {
            return currentFps;
        }

        // Zapíše aktuální FPS do konzole
        public void WriteToConsole()
        {
            Console.WriteLine($"FPS: {currentFps}");
        }

        // Nastaví titul okna hry na aktuální FPS
        public void WriteToTitle(Game game)
        {
            game.Title = $"FPS: {currentFps}";
        }
    }
}

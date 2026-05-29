using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zpg.src.Models
{
    public class MapLevel
    {
        public int width;
        public int height;
        public char[,] mapMatrix;



        public MapLevel(int width, int height)
        {
            this.width = width;
            this.height = height;
            mapMatrix = new char[width, height];
        }
    }
}

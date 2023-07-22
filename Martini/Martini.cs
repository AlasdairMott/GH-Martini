using System;

namespace Martini
{
    public class Martini
    {
        private readonly int gridSize;
        private readonly int numTriangles;
        private readonly int numParentTriangles;
        private readonly int[] indices;
        private readonly ushort[] coords;

        public int GridSize => this.gridSize;
        public int NumTriangles => this.numTriangles;
        public int NumParentTriangles => this.numParentTriangles;
        public ushort[] Coords => this.coords;
        public int[] Indices => this.indices;

        public Martini(int gridSize = 257)
        {
            this.gridSize = gridSize;
            int tileSize = gridSize - 1;

            if ((tileSize & (tileSize - 1)) != 0)
                throw new Exception($"Expected grid size to be 2^n+1, got {gridSize}.");

            this.numTriangles = tileSize * tileSize * 2 - 2;
            this.numParentTriangles = this.numTriangles - tileSize * tileSize;

            this.indices = new int[this.gridSize * this.gridSize];

            // coordinates for all possible triangles in an RTIN tile
            this.coords = new ushort[this.numTriangles * 4];

            // get triangle coordinates from its index in an implicit binary tree
            for (int i = 0; i < this.numTriangles; i++)
            {
                int id = i + 2;
                int ax = 0, ay = 0, bx = 0, by = 0, cx = 0, cy = 0;
                if ((id & 1) != 0)
                {
                    bx = by = cx = tileSize; // bottom-left triangle
                }
                else
                {
                    ax = ay = cy = tileSize; // top-right triangle
                }
                while ((id >>= 1) > 1)
                {
                    int mx = (ax + bx) >> 1;
                    int my = (ay + by) >> 1;

                    if ((id & 1) != 0) // left half
                    {
                        bx = ax; by = ay;
                        ax = cx; ay = cy;
                    }
                    else // right half
                    {
                        ax = bx; ay = by;
                        bx = cx; by = cy;
                    }
                    cx = mx; cy = my;
                }
                int k = i * 4;
                this.coords[k + 0] = (ushort)ax;
                this.coords[k + 1] = (ushort)ay;
                this.coords[k + 2] = (ushort)bx;
                this.coords[k + 3] = (ushort)by;
            }
        }

        public Tile CreateTile(float[] terrain)
        {
            return new Tile(terrain, this);
        }
    }
}


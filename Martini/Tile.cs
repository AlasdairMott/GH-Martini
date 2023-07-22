using System;
using Rhino.Render.DataSources;

namespace Martini
{
    public class MeshData
    {
        public double[] Vertices { get; set; }
        public int[] Triangles { get; set; }
    }

    public class Tile
    {
        private readonly float[] terrain;
        private readonly Martini martini;
        private readonly double[] errors;

        public Tile(float[] terrain, Martini martini)
        {
            int size = martini.GridSize;
            if (terrain.Length != size * size)
                throw new Exception($"Expected terrain data of length {size * size} ({size} x {size}), got {terrain.Length}.");

            this.terrain = terrain;
            this.martini = martini;
            this.errors = new double[terrain.Length];
            this.Update();
        }

        private void Update()
        {
            int numTriangles = this.martini.NumTriangles;
            int numParentTriangles = this.martini.NumParentTriangles;
            ushort[] coords = this.martini.Coords;
            int size = this.martini.GridSize;
            float[] terrain = this.terrain;
            double[] errors = this.errors;

            // Iterate over all possible triangles, starting from the smallest level
            for (int i = numTriangles - 1; i >= 0; i--)
            {
                int k = i * 4;
                int ax = coords[k + 0];
                int ay = coords[k + 1];
                int bx = coords[k + 2];
                int by = coords[k + 3];
                int mx = (ax + bx) >> 1;
                int my = (ay + by) >> 1;
                int cx = mx + my - ay;
                int cy = my + ax - mx;

                // Calculate error in the middle of the long edge of the triangle
                float interpolatedHeight = (terrain[ay * size + ax] + terrain[by * size + bx]) / 2;
                int middleIndex = my * size + mx;
                float middleError = Math.Abs(interpolatedHeight - terrain[middleIndex]);

                errors[middleIndex] = Math.Max(errors[middleIndex], middleError);

                if (i < numParentTriangles) // Bigger triangles; accumulate error with children
                {
                    int leftChildIndex = ((ay + cy) >> 1) * size + ((ax + cx) >> 1);
                    int rightChildIndex = ((by + cy) >> 1) * size + ((bx + cx) >> 1);
                    errors[middleIndex] = Math.Max(Math.Max(errors[middleIndex], errors[leftChildIndex]), errors[rightChildIndex]);
                }
            }
        }

        public MeshData GetMesh(double maxError = 0)
        {
            int size = this.martini.GridSize;
            int[] indices = this.martini.Indices;
            double[] errors = this.errors;
            int numVertices = 0;
            int numTriangles = 0;
            int max = size - 1;

            // Use an index grid to keep track of vertices that were already used to avoid duplication
            Array.Clear(indices, 0, indices.Length);

            // Retrieve the mesh in two stages that both traverse the error map:
            // - CountElements: find used vertices (and assign each an index), and count triangles (for minimum allocation)
            // - ProcessTriangle: fill the allocated vertices & triangles typed arrays

            void CountElements(int ax, int ay, int bx, int by, int cx, int cy)
            {
                int mx = (ax + bx) >> 1;
                int my = (ay + by) >> 1;

                if (Math.Abs(ax - cx) + Math.Abs(ay - cy) > 1 && errors[my * size + mx] > maxError)
                {
                    CountElements(cx, cy, ax, ay, mx, my);
                    CountElements(bx, by, cx, cy, mx, my);
                }
                else
                {
                    indices[ay * size + ax] = indices[ay * size + ax] == 0 ? ++numVertices : indices[ay * size + ax];
                    indices[by * size + bx] = indices[by * size + bx] == 0 ? ++numVertices : indices[by * size + bx];
                    indices[cy * size + cx] = indices[cy * size + cx] == 0 ? ++numVertices : indices[cy * size + cx];
                    numTriangles++;
                }
            }

            CountElements(0, 0, max, max, max, 0);
            CountElements(max, max, 0, 0, 0, max);

            double[] vertices3d = new double[numVertices * 3];
            int[] triangles = new int[numTriangles * 3];
            int triIndex = 0;

            void ProcessTriangle(int ax, int ay, int bx, int by, int cx, int cy)
            {
                int mx = (ax + bx) >> 1;
                int my = (ay + by) >> 1;

                if (Math.Abs(ax - cx) + Math.Abs(ay - cy) > 1 && errors[my * size + mx] > maxError)
                {
                    // Triangle doesn't approximate the surface well enough; drill down further
                    ProcessTriangle(cx, cy, ax, ay, mx, my);
                    ProcessTriangle(bx, by, cx, cy, mx, my);
                }
                else
                {
                    // Add a triangle
                    int a = indices[ay * size + ax] - 1;
                    int b = indices[by * size + bx] - 1;
                    int c = indices[cy * size + cx] - 1;

                    vertices3d[3 * a] = ax;
                    vertices3d[3 * a + 1] = ay;
                    vertices3d[3 * a + 2] = this.terrain[ay * size + ax];

                    vertices3d[3 * b] = bx;
                    vertices3d[3 * b + 1] = by;
                    vertices3d[3 * b + 2] = this.terrain[by * size + bx];
                    
                    vertices3d[3 * c] = cx;
                    vertices3d[3 * c + 1] = cy;
                    vertices3d[3 * c + 2] = this.terrain[cy * size + cx];

                    triangles[triIndex++] = a;
                    triangles[triIndex++] = b;
                    triangles[triIndex++] = c;
                }
            }

            ProcessTriangle(0, 0, max, max, max, 0);
            ProcessTriangle(max, max, 0, 0, 0, max);

            return new MeshData { Vertices = vertices3d, Triangles = triangles };
        }
    }
}


using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using static Martini.Utils;

namespace Martini
{
    public class MartiniComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public MartiniComponent()
          : base("Martini", "Martini",
            "Creates a mesh from a (2^k+1) × (2^k+1) heightmap using the Martini algorithm.",
            "Mesh", "Martini")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Path to image", GH_ParamAccess.item);
            pManager.AddNumberParameter("Error", "E", "Error amount", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Output mesh", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = String.Empty;
            if (!DA.GetData("Path", ref path))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Path is required.");
                return;
            }

            double error = 500;;
            DA.GetData("Error", ref error);
 
            if (!System.IO.File.Exists(path))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File does not exist.");
                return;
            }

            var png = new Bitmap(path);
            var method = "DEM";

            HeightFormula heightMethod;
            if (method == "DEM")
            {
                heightMethod = (r, g, b) => (r * 256 * 256 + g * 256.0f + b) / 10.0f - 10000.0f;
            }
            else
            {
                heightMethod = (r, g, b) => (r + g + b) / 3.0f;
            }

            float[] terrain = Utils.MapboxTerrainToGrid(png, heightMethod);

            var martini = new Martini(png.Width + 1);
            var tile = martini.CreateTile(terrain);
            var meshData = tile.GetMesh(error);

            var mesh = new Mesh();

            for (int i = 0; i < meshData.Vertices.Length; i += 3)
            {
                mesh.Vertices.Add(meshData.Vertices[i], meshData.Vertices[i + 1], meshData.Vertices[i + 2]);
            }
            
            for (int i = 0; i < meshData.Triangles.Length; i += 3)
            {
                int a = meshData.Triangles[i];
                int b = meshData.Triangles[i + 1];
                int c = meshData.Triangles[i + 2];
                mesh.Faces.AddFace(a, b, c);
            }

            DA.SetData("Mesh", mesh);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("4DADC0F9-B59B-4626-98C2-47197401E564");
    }
}

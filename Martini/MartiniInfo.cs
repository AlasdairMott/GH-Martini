using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace Martini
{
  public class MartiniInfo : GH_AssemblyInfo
  {
    public override string Name => "Martini Info";

    //Return a 24x24 pixel bitmap to represent this GHA library.
    public override Bitmap Icon => null;

    //Return a short string describing the purpose of this GHA library.
    public override string Description => "";

    public override Guid Id => new Guid("F0F75938-8A04-4305-B55D-7B40E0E64A7D");

    //Return a string identifying you or your company.
    public override string AuthorName => "";

    //Return a string representing your preferred contact details.
    public override string AuthorContact => "";
  }
}

// See https://aka.ms/new-console-template for more information
using BundleCrafter;
static class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: RemoveTypeTree <bundleFilePath>  <newBundleFilePath>(option)");
            return;
        }
        if (args.Length < 2)
        {
            args = new string[] { args[0], args[0] + ".noTypeTree" };
        }
        BundleModifier.RemoveBundleTypeTree(args[0] as string, args[1] as string);
    }
}
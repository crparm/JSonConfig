using System;
using System.IO;
namespace JSonConfig
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
                Console.WriteLine("Missing Arguments");
            else
            {
                if(!File.Exists(args[0]))
                    Console.WriteLine("Invalid File Path");
                else if (!Directory.Exists(args[1]))
                    Console.WriteLine("Invalid Folder Path");
                else
                {
                    JSonConfigurator jc = new JSonConfigurator();
                    jc.Parse(args[0], args[1], args[2]);
                }
            }
        }
    }
}

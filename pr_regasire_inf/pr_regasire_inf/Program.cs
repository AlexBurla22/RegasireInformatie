using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace pr_regasire_inf
{
    class Program
    {
        static void Main(string[] args)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            string dirPath = "Files\\Reuters_34";
            string fileExtension = "*.xml";

            InfoExtracter ext = new InfoExtracter();

            ext.ProcessFilesInDirectory(dirPath, fileExtension);

            watch.Stop();

            Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");
            Console.WriteLine("Press Q to query from file, ENTER to query from console, or any other key to exit...");
            ConsoleKey keyPressed = Console.ReadKey().Key;
            if (keyPressed == ConsoleKey.Q)
            {
                string[] queries = System.IO.File.ReadAllLines("Files\\query_34.txt");

                foreach (var q in queries)
                {
                    ext.QueryFile(q);
                }
                Console.WriteLine("\nQuery results are written in folder Output");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            else if (keyPressed == ConsoleKey.Enter)
            {
                Console.WriteLine("\nWrite your query: ");
                string q = Console.ReadLine();
                
                Console.WriteLine("\nHere are the results:\n");
                ext.QueryConsole(q);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }  
        }
    }
}


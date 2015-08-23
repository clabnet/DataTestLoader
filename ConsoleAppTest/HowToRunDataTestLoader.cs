using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DataTestLoader
{
    /// <summary>
    /// Sample to start DataTestLoader
    /// </summary>
    class HowToRunDataTestLoader
    {
        // Press F5 to RUN; the log will be print also on C:\Logs\DataTestLoader folder.
        static void Main(string[] args)
        {
            // Please note: 
            // 1. all files into DataTestFiles and DatabaseScripts folder (and subfolders) MUST be set with attribute "CopyToOutput = true"
            // 2. if data already found on database, and specified true only loadJsonData, will be occurs duplicate key errors.

            // Arguments explaination.

            // refreshSchema = true will export the shema from remote database; false = will be reuse the schema files located into FolderSchema path (default : false) 
            // initDatabase = true will drop, create, apply schema (default : false) 
            // loadJsonData = true will add the tables with data present in DataTestFiles folder. (default : false) 
            // testSuite = will be add .json data files from this subfolder of DataTestFiles (default : "") 

            new DataTestLoader(refreshSchema: true, initDatabase: true, loadJsonData: true, testSuite: "");

            Console.WriteLine("Press any key to exit ...");

            Console.ReadKey();

        }
    }
}

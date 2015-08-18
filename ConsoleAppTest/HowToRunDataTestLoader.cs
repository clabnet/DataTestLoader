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
        // Press F5 to RUN; the output log will be named DTL-DataTestLoader.log into C:\Temp\DataTestLoader\log folder.
        static void Main(string[] args)
        {
            // refreshSchema = true will export the shema from remote database; false = will be reuse the schema files located into FolderSchema path 
            // initDatabase = true will drop, create, apply schema
            // loadJsonData = true will add the tables with data present in DataTestFiles folder. 
            // Please note: 
            // 1. all files into DataTestFiles and DatabaseScripts folder MUST be set with attribute "CopyToOutput = true"
            // 2. if data already found on database, and specified true only loadJsonData, will be occurs duplicate key errors.
            new DataTestLoader(refreshSchema : false, initDatabase: false, loadJsonData: false);
        }
    }
}

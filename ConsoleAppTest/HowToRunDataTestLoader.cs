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
        // Press F5 to RUN; the output log will be named DTL-DataTestLoader.log into C:\Temp\log\SchemaDB folder.
        static void Main(string[] args)
        {
            // refreshSchema = when True will export the schema from remote database; false = will be reuse the file schema located into FolderSchema path 
            // initDatabase = when True will drop, create, apply schema
            // loadJsonData = when True will be added the tables with data located on DataTestFiles folder.
            // Please remember: all files into DataTestFiles and DatabaseScripts folder MUST be set with attribute "CopyToOutput = true"
            new DataTestLoader(refreshSchema : true, initDatabase: true, loadJsonData: true);
        }
    }
}

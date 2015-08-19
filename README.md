![](http://icons.iconarchive.com/icons/gakuseisean/ivista-2/64/Misc-New-Database-icon.png)

# DataTestLoader #

----------


To obtain **Integration Test** effective and consistent, they must always be performed in isolated environments, in order to ensure the existence of the expected data and to grant the possibility to modify them if necessary.

Unfortunately, the creation of databases for tests is a time-consuming and repetitive activity. This project was created **to facilitate the creation of database test**.

The idea is simple: the data are exported from the development database based on .json format and then recreated in another database created ad-hoc.
The new database will have an identical structure to the source database and will contain only the data you need to test. This can be reproduced several times, since the test database is deleted, created and reloaded with all data provided by the developer. 

###Prerequisites###
1. .NET Framework 4.5
2. Postgresql DB server 9.4+ x64

###Third parts libraries###

The code is based mainly up two libraries:

1. [Dapper.net](https://github.com/StackExchange/dapper-dot-net) 
2. Dapper.SimpleCRUD **[clabnet edition](https://github.com/clabnet/Dapper.SimpleCRUD)**
 
Other references are **Nunit e FluentAssertions** for Unit Test and .NET driver **Npgsql** for access to Postgresql database.  


###Configuration###

> **Important** Please pay attention to the settings of connection strings, in particular to that related to the Test database. 
> 
**The test database will be deleted and recreated** during the initialization phase of DataTestLoader. By convention, the name of the test database must end with "_test" word.

1.  **DBSource** - Database source from which to infer the schema that will be used in the creation of the test database.

2.  **DbTest** - Connection string of test database.

3.  **DBPostgres** - Connection string to Postgres database. It is required to drop the test database.
 
4.  **FileSchemaPreData** - Name of the file required for initial definition of test db (used only in case of re-use of an existing schema, for performance reasons. This is the file returned by *pg_dump* command with *section=pre-data* arguments.)

5.  **FileSchemaPostData** - Name of the file required for post definition of test db (used only in case of re-use of an existing schema, for performance reasons. file returned by *pg_dump* command with *section=post-data* arguments.)

6.  **AssemblyModel** - Name of the library that contains the external .dll [POCO classes](https://en.wikipedia.org/wiki/Plain_Old_CLR_Object) corresponding to the entities to be created in the database.
 
7. **AssemblyModelNamespace** - Namespace of POCO classes contained into AssemblyModel.

> The AssemblyModel assembly contains the POCO classes for tables to be loaded. *The name of these classes must be equal to the table name to be loaded, with Public Properties corresponding to the table structure*.

> This assembly is necessary only if the loading data from .json files in the folder  **DataTestFiles**. (loadJsonData flag = true) is required.

> This .dll **must be located in the .bin DataTestLoader.**


###POCO class##

This is a simple example of [POCO class](https://en.wikipedia.org/wiki/Plain_Old_CLR_Object) .

        public class Customer
		{
			public int Id { get; set; }
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public string Address { get; set; }
			...
		}


>To define automagically a POCO class in C# language you can use also the online tool [json2csharp](http://json2csharp.com/)

>**To massive generation of POCO class** corresponding to database entities, it is useful to use MS technology [**Text Translate Transformation Toolkit**](https://en.wikipedia.org/wiki/Text_Template_Transformation_Toolkit) also known as **T4**.

 
#####Automatic regeneration of POCO class#####

To manually define the POCO classes corresponding to database entities is a long and time-consuming activity.

Several types of code generators are available on the market, including the excellent [**CodeSmith**](http://www.codesmithtools.com/product/generator) and the most popular [**T4**](https://en.wikipedia.org/wiki/Text_Template_Transformation_Toolkit) (because integrated in the Visual Studio).

You can auto-generate POCO classes directly from the database by executing a T4 Template. The operation to generate (or regenerate) the POCO classes of an entire database is extremely simple and easy: right-click the template T4 (.tt extension) and run Run Custom Tool.


###SampleModel project
 
The sample project contains a T4 OrmLite based template and its EntityModel generated from the database specified in the configuration file. It can be used to automatically create a model from the database on the *ConnectionStringDBSource*.
The example database is a Northwind mini version, you can find the creation script on \SampleModel\DatabaseScript folder.

###ConsoleApptest project
 
This project contains an example to instantiate correctly DataTestLoader.

###DataTestFiles folder ###

This folder contains all .JSON files with the data to be inserted on test database.

> By convention, *the name of this file must correspond to table name where data will be inserted* (with .json extension). 

> The names of these files are case-sensitive.

> **TablesToLoad.json** contains the *sequence order to loading the tables*. See this example:

    [
    	"Customer",
		"Order",
		"OrderItem",
		...
	]


> In order to extract data from the database and export .json format easily, you can also use [Database.NET](http://fishcodelib.com/database.htm). Any other way to create these files .json, manual or automatic mode is admitted.
 
> In order to generate automatically data files .json you can also use the online tool [JSON generator](http://www.json-generator.com/).

> To load data in ByteArray type format in entities that require it, you can use the online tool [AJAX ByteChar Converter](http://tools.thebuzzmedia.com/bytechar)


###How to use DataTestLoader

Here are described a few ways to use the project:

**1. Using ConsoleAppTest project**

- Open project. 
- Set properly the connections strings and set it as the Default Startup Project. 
- Press F5. On the console you will see the messages Log. (see also log files on *C:\Temp\DataTestLoader* folder).

**2. Run Unit Test**

The source project is provided with a set of unit tests NUnit 2.6. Set the connection strings properly. Run Test. See the log on Debug Window.
(see also log files on *C:\Temp\DataTestLoader* folder)

**3. Using NuGet packages**

Main prerequisites : PostgreSql v9.4+ database instance.

- Open a new console application project C# on Visual Studio 2013. 
- Run Enable NuGet Package Restore.
- Launch Manage NuGet packages for solution. 
- Find and install DataTestLoader project on NuGet.org repository. 
- Copy SampleModel.dll assembly from packages\lib\net45\ to bin\Debug folder.
- Copy AppConfigSample\app.config.txt\app.config file to replace app.config file; update the configuration values.
- Add this command to Main method of Program.cs:
- **new DataTestLoader(refreshSchema : true, initDatabase: true, loadJsonData: true);**
- Set CopyToOutputDirectoy = CopyAlways on all files contained on DatabaseScripts and DataTestFiles folder. 
- Build current project and execute it. 
- See DTL-DataTestLoader.log on C:\temp\DataTestLoader\log folder.
- Finished, that's all.

> **DataTestLoader method arguments**
> 
> - **refreshSchema** = If true will export the shema from remote database; false = will be reuse the file schema located into FolderSchema path 
> - **initDatabase** = If true will drop, create and apply the schema.
> - **loadJsonData** = If true will add data present in DataTestFiles folder to tables specified into TablesToLoad.json file. If data are already found on database, and loadJsonData is the unique value setting as true, will be occurs a duplicate key errors.

###Tips - Quick fix

> 1. The most common mistake that happens is to forget to put in **CopyToOutput all files into DataTestFiles and DatabaseScripts folders**.

> 2. **Message Assembly Model SampleModel was not found**. This message indicates that you must specify the assembly containing the POCO classes. If you don't need the loading of data, please disable the loadJsonData argument.

> 3. **Type 'xxx' or assembly 'yyy' or namespace 'www' not found on ....**
The specified type cannot be found into assembly specified, or that assemby was not found into bin directory, or related namespace was not correctly specified into .config file.
You can automatically regenerated the model using Right-Click -> Run Custom Tool on SampleModel.tt. 

Change log
=========

##Version 1.1.0 - 19/8/2015
 
- New. All errors returned by Process.Start are now redirect into main log file. 
- New. Removed timestamp on database schema filename. Also the config key FolderSchema was removed.
- New. Added [NLog](http://nlog-project.org/) logging system. See NLog.config file for logging configuration options.
- Fix. *"Failed to find or load the registered .Net Framework Data Provider"* error message on ConsoleAppTest execution. On a machine where Npgsql was not yet installed on GAC, the Npgsql driver *must be present on bin folder*.
- Fix. The service name used for finding the native PG commands was changed to select the instance of server required: now it is **postgresql-x64-9.4**. 
- New. Refactoring creation schema of database. Now we use the *--section=pre-data* and *--section=post-data* arguments. [See Postgresql documentation for details](http://www.postgresql.org/docs/9.4/static/app-pgdump.html)
- New. Refactoring CheckValidSettings. The keys of config file are now required only when are used, based on command line arguments of DataTestLoader.

##Version 1.0.0 - 15/7/2015

- First release on public repositories **[NuGet](http://www.nuget.org/packages/DataTestLoader/)** and **[GitHub.](https://github.com/clabnet/DataTestLoader)**


------
In case of translation or coding errors, please feel free to contact me.

Claudio Barca 
c.barca at gmail dot com

Last revision document : 8/19/2015 9:48:27 PM 

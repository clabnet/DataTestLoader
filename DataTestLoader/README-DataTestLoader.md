![](http://icons.iconarchive.com/icons/gakuseisean/ivista-2/64/Misc-New-Database-icon.png)

# DataTestLoader #

----------


To obtain **Integration Test** effective and consistent, these must always be performed in isolated environments, in order to ensure the existence of the expected data and to ensure the possibility to modify them if necessary.

Unfortunately, the creation of databases for tests is an activity time-consuming and repetitive. This project was created **to facilitate the creation of database test**.

The idea is simple: the data is exported from the development database based on .json format and then recreated in another database created ad-hoc.
The new database will have an identical structure to source database and will contain only the data you need to test. This can be reproduced several times, since the test database, if any, is deleted, created and loaded with all data provided by the developer. 

###Prerequisites###
1. .NET Framework 4.5
2. NpgSql
3. Postgresql DB server 9.4+

###Third parts libraries###

The code is based on two libraries:

1. [Dapper.net](https://github.com/StackExchange/dapper-dot-net) 
2. Dapper.SimpleCRUD **[clabnet edition](https://github.com/clabnet/Dapper.SimpleCRUD)**
 
Other references are **Nunit e FluentAssertions** for Unit Test and driver .NET **Npgsql** for access to Postgresql database.  


###Configuration###

> **Important** Please attention to settings of connection strings, in particular that relating to the database of Test. 
> 
**The test database will be deleted and recreated** during the initialization phase of DataTestLoader. For convention, the name of the test database must end with "_test" word.

1.  **DBSource** - Database source from which to infer the schema that will be used in the creation of the test database.

2.  **DbTest** - Connection string of test database.

3.  **DBPostgres** - Connection string to Postgres databse. It is required to drop of the test database.
 
4.  **FileSchema** - Name of the file required for definition of test db (valid only in case of re-use of an existing schema, for performance reasons.)

5.  **FolderSchema** - The file with source database schema will be saved on this folder. using name as {server}-{dbname}-{YYYYMMDD-HHMMSS}.sql. 

6.  **AssemblyModel** - Name of the library that contains the external .dll [POCO classes](https://en.wikipedia.org/wiki/Plain_Old_CLR_Object) corresponding to the entity to be created in the database.
 
7. **AssemblyModelNamespace** - Namespace of POCO classes.

> The AssemblyModel assembly contains the POCO classes for tables to be loaded. *The name of these classes must be equal to the table name to be loaded, with Public Properties corresponding to the table structure*.

> This assembly is only necessary if required loading data from files in the folder .json  **DataTestFiles**. (loadJsonData flag = true)

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

>**To massive generation of POCO class** corresponding to database entities it is useful use a MS technology [**Text Template Transformation Toolkit**](https://en.wikipedia.org/wiki/Text_Template_Transformation_Toolkit) also know as **T4**.

 
#####Automatic regeneration of POCO class#####

Manually define the POCO classes corresponding to database entities is an operation laborious and unnecessary.

From time ago are present on the market several types of code generators,
including the excellent [**CodeSmith**](http://www.codesmithtools.com/product/generator) and the most common [**T4**](https://en.wikipedia.org/wiki/Text_Template_Transformation_Toolkit) (because integrated in the Visual Studio).

You can auto-generate POCO classes directly from the database by executing a T4 Template. The operation to generate (or regenerate) the POCO classes of an entire database is extremely simple and easy: right-click the template T4 (.tt extension) and run Run Custom Tool.


###SampleModel project
 
The sample project contains a T4 OrmLite based template and its EntityModel generated from the database specified in the configuration file. It can be used to automatically create a model from the database on the *ConnectionStringDBSource*.
The example database is a Northwind mini version, you can find the creation script on \SampleModel\DatabaseScript folder.

###ConsoleApptest project
 
This project contains an example to instantiate correctly DataTestLoader.

###DataTestFiles folder ###

This folder contains all .JSON files with the data to insert on test database.

> For convention, *the name of this file must be corresponding to table name where insert the data* (with .json extension). 

> The names of these files are case-sensitive.

> **TablesToLoad.json** contains the *sequence order to loading the tables*. See this example:

    [
    	"Customer",
		"Order",
		"OrderItem",
		...
	]


> In order to easily extract data from the database and export .json format, you can also use [Database.NET](http://fishcodelib.com/database.htm). It is admitted any other way to create these files .json, manual or automatic mode.
 
> To generate automatically format data files .json you can also use the online tool [JSON generator](http://www.json-generator.com/).

> Entity with "Identity" key value in the .json file is not required; if this key it is present, the value will be discarded.

> To load data in ByteArray in entities that require it, you can use the online tool [AJAX ByteChar Converter](http://tools.thebuzzmedia.com/bytechar)


###How to use DataTestLoader

Here they are described in a couple of ways to run the project:

1. **Console Application**

Open ConsoleApptest project. Set properly the connections strings and set it as the Default Startup Project. Press F5. On the console you will see the messages Log. 
(see also log files on C:\Temp\SchemaDB\ folder)

2. **Run Unit Test**

The project is accompanied by a set of unit tests NUnit format 2.6. Properly set the connection strings. Run Test. See the log on Debug Window.
(see also log files on C:\Temp\SchemaDB\ folder)



###Tips - Quick fix

> 1. The most common mistake that happens you forget to put in CopyToOutput all files contained into DataTestFiles and DatabaseScripts folders.

> 2. **Message Assembly Model SampleModel was not found**. This message indicates that you must specify the assembly containing the POCO classes. if not used in loading data format .json, disable the flag loadJsonData.


------
In case of translation or coding errors, please contact me.

Claudio Barca 
c.barca at gmail dot com


![](http://icons.iconarchive.com/icons/gakuseisean/ivista-2/64/Misc-New-Database-icon.png)

# DataTestLoader #

Utility to create test database for integration tests

----------

Sorry, the english translation it is coming soon...


Per ottenere **Integration Test** efficaci e consistenti, questi devono essere eseguiti sempre in ambienti isolati, in modo da assicurare l'esistenza dei dati previsti e garantire la possibilità di modificarli all'occorrenza.

Purtroppo, creare basi dati di test è un'operazione lunga e ripetitiva. Questo progetto è stato creato per **facilitare la creazione di basi dati di test.**

L'idea è semplice: i dati vengono esportati dal database di sviluppo in formato .JSON e ricreati in un altro database creato ad-hoc.  
Il nuovo database avrà struttura identica al database di origine e conterrà solo i dati che servono ai test. Questa operazione è rieseguibile più volte, in quanto il database di test, se presente, viene eliminato, creato e ricaricato con i dati previsti dallo sviluppatore.  

###Prerequisiti###
1. .NET Framework 4.5
2. NpgSql
3. Postgresql DB server 

###Librerie di terze parti###

Il codice si basa fondamentalmente su due librerie:

1. [Dapper.net](https://github.com/StackExchange/dapper-dot-net) 
2. Dapper.SimpleCRUD **[clabnet edition](https://github.com/clabnet/Dapper.SimpleCRUD)**
 
Gli altri componenti sono **Nunit e FluentAssertions** per gli Unit Test ed il driver .NET **Npgsql** per l'accesso al database Postgresql.  


###Configurazione###

> **Attenzione** alla codifica delle connection strings, in particolar modo quella relativa al database di Test. 
> 
**Il database di test verrà eliminato e ricreato** durante la fase di inizializzazione di DataTestLoader.
Per convenzione, il nome del database di test deve terminare con "_test". 

1. **DBSource** - Database origine da cui inferire lo schema che sarà  usato nella creazione del database di test.

2. **DBTest** - Stringa di connessione del database di test.

3. **DBPostgres** - Stringa di connessione del database di Postgres. Serve per poter eseguire la drop del database di test.
 
4. **FileSchema** - Nome del file schema da usare per la definizione del db di test (valido soltanto in caso di riutilizzo di uno schema esistente)  

5. **FolderSchema** - In questa folder viene creato lo schema del database "source". Il file viene salvato con nome {server}-{dbname}-{YYYYMMDD}-{HHMMSS}.sql
 (es.TIERRA-PGSQL-94-DEV-tsshield-20150616-101815.sql). Il nuovo file viene creato soltanto in caso di riutilizzo di uno schema esistente. 

6. **AssemblyModel** - Nome della libreria .dll esterna che contiene le [classi POCO](https://en.wikipedia.org/wiki/Plain_Old_CLR_Object) corrispondenti alle entità da creare nel database. 
 
	>Questo assembly contiene le classi POCO relative alle tabelle da caricare. Il nome di queste classi *deve essere uguale al nome della tabella da caricare, con Public Properties corrispondenti alla struttura della tabella*.

	> Questo assembly è necessario solo se richiesto il caricamento dei dati dai files .JSON nella folder **DataTestFiles**. (flag loadJsonData = true)
	
	>Questa .dll **deve trovarsi nella cartella .bin di DataTestLoader.** 
7. **AssemblyModelNamespace** - Namespace delle classi POCO.

###POCO class##

Questo è un esempio di [classe POCO](https://en.wikipedia.org/wiki/Plain_Old_CLR_Object) .

        public class Customer
		{
			public int Id { get; set; }
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public string Address { get; set; }
			...
		}


>Per definire automaticamente una classe POCO si può usare anche il tool online [json2csharp](http://json2csharp.com/)

>**Per rigenerare automaticamente tutte le classi POCO** corrispondenti alle Entità di un database è possibile usare la tecnica Microsoft [**Text Template Transformation Toolkit**](https://en.wikipedia.org/wiki/Text_Template_Transformation_Toolkit) detta anche **T4**.

 
#####Rigenerazione automatica POCO class#####

*Definire manualmente le classi POCO corrispondenti alle entità presenti in un database è operazione laboriosa ed inutile.*

Da tempo sono presenti sul mercato svariati tipi di generatori di codice,
tra cui l'ottimo [CodeSmith](http://www.codesmithtools.com/product/generator) ed il più diffuso [**T4**](https://en.wikipedia.org/wiki/Text_Template_Transformation_Toolkit) (perchè integrato nell'IDE Visual Studio).

Puoi **autogenerare le classi POCO** direttamente dal database eseguendo un Template T4. L'operazione di generare (o rigenerare) le classi POCO di un intero database è estremamente semplice e veloce: Tasto destro sul template T4 (con estensione .tt) ed eseguire Run Custom Tool. 

###SampleModel project
 
Il progetto di esempio contiene un template T4 e relativo EntityModel generato a partire dal database indicato nel file di configurazione. Può essere utilizzato per creare automaticamente un model dal database relativo alla *ConnectionStringDBSource* .

###ConsoleApptest project
 
Il progetto contiene la **classe di esempio** per instanziare ed eseguire DataTestLoader.

###Contenuto della cartella *DataTestFiles*###

Questa cartella contiene i files .JSON con i dati da inserire nel database di test.

Per convenzione *il nome di questi files deve corrispondere al nome della tabella in cui si vuole inserire i dati* (seguito da .json come estensione). 

> I nomi di questi files sono case-sensitive.



**TablesToLoad.json** contiene le indicazioni per *caricare le tabelle nella corretta sequenza*. 

    [
    	"Customer",
		"Order",
		"OrderItem",
		...
	]

> Per estrarre facilmente i dati dal database ed esportarli in formato .JSON, è possibile anche utilizzare [Database.NET](http://fishcodelib.com/database.htm). E' ammesso ogni altro modo per creare questi files .json, manuale o automatico. 
 
> Per generare automaticamente file dati in formato .JSON è possibile anche usare il tool online [JSON generator](http://www.json-generator.com/).

> Nelle entità con chiave "Identity" il valore della chiave Id nel file .JSON  non è richiesto; nel caso in cui questo fosse presente, il valore sarà scartato.

> Per caricare dati in formato byteArray nelle entità che lo richiedono, è possibile usare il tool online [AJAX ByteChar Converter](http://tools.thebuzzmedia.com/bytechar)


###How to use

Qui sono descritti un paio di modi per eseguire il progetto :

1. **Console Application**

Aprire il progetto ConsoleApptest. Impostare opportunamente le stringhe di connessione ed impostarlo come Default Startup Project. Premere F5. Sulla console si vedranno i messaggi di Log. (vedi anche file c:\temp\SchemaDB\DTL-DataTestLoader.log)

2. **Run Unit Test**

Il progetto è corredato di una serie di unit test in formato NUnit
2.6. Impostare opportunamente le stringhe di connessione. Eseguire il test "*When_I_need_a_new_database_test_it_should_be_good*". Nella finestra di Debug saranno visualizzati i messaggi di Log.  

----------

###Enhancements

Sono previste le seguenti features aggiuntive:

1. La cartella *DataTestFiles* dovrà contenere *n* sottocartelle con le testsuite da eseguire: ogni testsuite inserirà nel database i dati desiderati.
2. Anche il file *TablesToLoad.json* dovrà contenere l'indicazione delle testsuite.
3. Le tabelle devono essere inizializzate con alcuni dati di base, quindi è necessario prevedere un sistema simile a quello del file TablesToLoad per caricare gli scripts utente.

----------

###Tips - Quick fix

> L'errore più comune che capita è dimenticarsi di mettere in CopyToOutput tutti i files delle cartelle DataTestFiles e DatabaseScripts.

> Messaggio *Assembly Model SampleModel was not found on D:\svn-TIERRA\TSShield\TokyoAccess\trunk\TokyoAccess.UnitTest\bin\Debug* 
> Il messaggio indica che è necessario specificare l'assembly contenente le classi POCO. se non viene usato il caricamento dei dati in formato .JSON, disattivare il flag loadJsonData. 

----------------
Questo documento è stato creato nel formato [Markdown](http://en.wikipedia.org/wiki/Markdown). 
Per modificare questo file potete usare Notepad oppure installare [MarkdownPad](http://markdownpad.com/)


----------------
Last revision document: 7/9/2015 10:19:24 AM 


using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using FluentAssertions;
using System.Diagnostics;
using System;

namespace DataTestLoader
{
    class DataTestLoaderTests
    {

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
        }

        [Test]
        public void When_reading_dataTestConfig_it_should_be_good()
        {
            var dtm = new DataTestLoader();

            List<string> tablesToLoad = dtm.RetrieveTablesList();
            tablesToLoad.Count().Should().BeGreaterThan(0, "tables to load must be declared");
        }

        [Test]
        public void When_reading_dataTestFiles_all_files_must_be_found_on_disk()
        {
            var dtm = new DataTestLoader();

            bool isFoundFiles = dtm.CheckFilesOnDisk();
            isFoundFiles.Should().BeTrue("all files declared into TablesToLoad.json must be found on DataTestFiles folder");
        }

        [Test]
        public void When_loading_a_table_it_should_be_good()
        {
            var dtm = new DataTestLoader(initDatabase: true);

            // this name must be equal to class name found on model assembly
            string tableName = "product";

            int recordsAdded = dtm.AddRows(tableName);
            recordsAdded.Should().BeGreaterThan(0, "at least one record must be inserted on the table");
        }

        [Test]
        public void When_I_need_a_new_database_test_it_should_be_good()
        {
            var dtm = new DataTestLoader(initDatabase: true, loadJsonData: true);
            dtm.TotalRecordsAdded.Should().Be(176, "all records must be inserted");
        }

    }
}

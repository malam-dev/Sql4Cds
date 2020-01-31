﻿using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using FakeXrmEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Metadata;

namespace MarkMpn.Sql4Cds.Engine.Tests
{
    [TestClass]
    public class Sql2FetchXmlTests
    {
        [TestMethod]
        public void SimpleSelect()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name FROM account";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' />
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void SelectSameFieldMultipleTimes()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name, name FROM account";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' />
                    </entity>
                </fetch>
            ");

            CollectionAssert.AreEqual(new[]
            {
                "accountid",
                "name",
                "name"
            }, ((SelectQuery)queries[0]).ColumnSet);
        }

        [TestMethod]
        public void SelectStar()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT * FROM account";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch>
                    <entity name='account'>
                        <all-attributes />
                    </entity>
                </fetch>
            ");

            CollectionAssert.AreEqual(new[]
            {
                "accountid",
                "name"
            }, ((SelectQuery)queries[0]).ColumnSet);
        }

        [TestMethod]
        public void SelectStarAndField()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT *, name FROM account";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch>
                    <entity name='account'>
                        <all-attributes />
                    </entity>
                </fetch>
            ");

            CollectionAssert.AreEqual(new[]
            {
                "accountid",
                "name",
                "name"
            }, ((SelectQuery)queries[0]).ColumnSet);
        }

        [TestMethod]
        public void SimpleFilter()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name FROM account WHERE name = 'test'";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' />
                        <filter>
                            <condition attribute='name' operator='eq' value='test' />
                        </filter>
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void NestedFilters()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name FROM account WHERE name = 'test' OR (accountid is not null and name like 'foo%')";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' />
                        <filter type='or'>
                            <condition attribute='name' operator='eq' value='test' />
                            <filter type='and'>
                                <condition attribute='accountid' operator='not-null' />
                                <condition attribute='name' operator='like' value='foo%' />
                            </filter>
                        </filter>
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void Sorts()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name FROM account ORDER BY name DESC, accountid";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' />
                        <order attribute='name' descending='true' />
                        <order attribute='accountid' />
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void SortByColumnIndex()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name FROM account ORDER BY 2 DESC, 1";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' />
                        <order attribute='name' descending='true' />
                        <order attribute='accountid' />
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void SortByAliasedColumn()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name as accountname FROM account ORDER BY name";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' alias='accountname' />
                        <attribute name='name' />
                        <order attribute='name' />
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void Top()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT TOP 10 accountid, name FROM account";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch top='10'>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' />
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void NoLock()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name FROM account (NOLOCK)";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch no-lock='true'>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' />
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void Distinct()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT DISTINCT accountid, name FROM account";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch distinct='true'>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' />
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void Offset()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name FROM account ORDER BY name OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch count='50' page='3'>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' />
                        <order attribute='name' />
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void SimpleJoin()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name FROM account INNER JOIN contact ON accountid = parentcustomerid";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='inner' alias='contact'>
                        </link-entity>
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void SelfReferentialJoin()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT contact.contactid, contact.firstname, manager.firstname FROM contact LEFT OUTER JOIN contact AS manager ON contact.parentcustomerid = manager.contactid";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch>
                    <entity name='contact'>
                        <attribute name='contactid' />
                        <attribute name='firstname' />
                        <link-entity name='contact' from='contactid' to='parentcustomerid' link-type='outer' alias='manager'>
                            <attribute name='firstname' />
                        </link-entity>
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void AdditionalJoinCriteria()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name FROM account INNER JOIN contact ON accountid = parentcustomerid AND (firstname = 'Mark' OR lastname = 'Carrington')";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='inner' alias='contact'>
                            <filter type='or'>
                                <condition attribute='firstname' operator='eq' value='Mark' />
                                <condition attribute='lastname' operator='eq' value='Carrington' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedQueryFragmentException))]
        public void InvalidAdditionalJoinCriteria()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name FROM account INNER JOIN contact ON accountid = parentcustomerid OR (firstname = 'Mark' AND lastname = 'Carrington')";

            sql2FetchXml.Convert(query);
        }

        [TestMethod]
        public void SortOnLinkEntity()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name FROM account INNER JOIN contact ON accountid = parentcustomerid ORDER BY name, firstname";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch>
                    <entity name='account'>
                        <attribute name='accountid' />
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='inner' alias='contact'>
                            <order attribute='firstname' />
                        </link-entity>
                        <order attribute='name' />
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedQueryFragmentException))]
        public void InvalidSortOnLinkEntity()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT accountid, name FROM account INNER JOIN contact ON accountid = parentcustomerid ORDER BY firstname, name";

            sql2FetchXml.Convert(query);
        }

        [TestMethod]
        public void SimpleAggregate()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT count(*), count(name), count(DISTINCT name), max(name), min(name), avg(name) FROM account";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch aggregate='true'>
                    <entity name='account'>
                        <attribute name='accountid' aggregate='count' alias='accountid_count' />
                        <attribute name='name' aggregate='countcolumn' alias='name_countcolumn' />
                        <attribute name='name' aggregate='countcolumn' distinct='true' alias='name_countcolumn_2' />
                        <attribute name='name' aggregate='max' alias='name_max' />
                        <attribute name='name' aggregate='min' alias='name_min' />
                        <attribute name='name' aggregate='avg' alias='name_avg' />
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void GroupBy()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT name, count(*) FROM account GROUP BY name";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch aggregate='true'>
                    <entity name='account'>
                        <attribute name='name' groupby='true' alias='name' />
                        <attribute name='accountid' aggregate='count' alias='accountid_count' />
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void GroupBySorting()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT name, count(*) FROM account GROUP BY name ORDER BY name, count(*)";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch aggregate='true'>
                    <entity name='account'>
                        <attribute name='name' groupby='true' alias='name' />
                        <attribute name='accountid' aggregate='count' alias='accountid_count' />
                        <order alias='name' />
                        <order alias='accountid_count' />
                    </entity>
                </fetch>
            ");
        }

        [TestMethod]
        public void GroupBySortingOnLinkEntity()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var sql2FetchXml = new Sql2FetchXml(metadata, true);

            var query = "SELECT name, firstname, count(*) FROM account INNER JOIN contact ON parentcustomerid = account.accountid GROUP BY name, firstname ORDER BY name, firstname";

            var queries = sql2FetchXml.Convert(query);

            AssertFetchXml(queries, @"
                <fetch aggregate='true'>
                    <entity name='account'>
                        <attribute name='name' groupby='true' alias='name' />
                        <attribute name='accountid' aggregate='count' alias='accountid_count' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='inner' alias='contact'>
                            <attribute name='firstname' groupby='true' alias='firstname' />
                            <order alias='firstname' />
                        </link-entity>
                        <order alias='name' />
                    </entity>
                </fetch>
            ");
        }

        private void AssertFetchXml(Query[] queries, string fetchXml)
        {
            Assert.AreEqual(1, queries.Length);
            Assert.IsInstanceOfType(queries[0], typeof(SelectQuery));

            var serializer = new XmlSerializer(typeof(FetchXml.FetchType));
            using (var reader = new StringReader(fetchXml))
            {
                var fetch = (FetchXml.FetchType)serializer.Deserialize(reader);
                PropertyEqualityAssert.Equals(fetch, ((SelectQuery)queries[0]).FetchXml);
            }
        }
    }
}
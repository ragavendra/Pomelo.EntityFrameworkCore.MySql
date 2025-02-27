﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.FunctionalTests.TestUtilities.DebugServices;
using Xunit;
using Xunit.Abstractions;

namespace Pomelo.EntityFrameworkCore.MySql.FunctionalTests.Query
{
    // We use our custom fixture here, to inject a custom debug services.
    public class NorthwindSplitIncludeNoTrackingQueryMySqlTest : NorthwindSplitIncludeNoTrackingQueryTestBase<NorthwindSplitIncludeNoTrackingQueryMySqlTest.NorthwindSplitIncludeNoTrackingQueryMySqlFixture>
    {
        public NorthwindSplitIncludeNoTrackingQueryMySqlTest(NorthwindSplitIncludeNoTrackingQueryMySqlFixture fixture, ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        public override Task Include_duplicate_collection_result_operator2(bool async)
        {
            // The order of `Orders` can be different, because it is not explicitly sorted.
            // The order of the end result can be different as well.
            // This is the case on MariaDB.
            return AssertQuery(
                async,
                ss => (from c1 in ss.Set<Customer>().Include(c => c.Orders).OrderBy(c => c.CustomerID).ThenBy(c => c.Orders.FirstOrDefault() != null ? c.Orders.FirstOrDefault().CustomerID : null).Take(2)
                    from c2 in ss.Set<Customer>().OrderBy(c => c.CustomerID).Skip(2).Take(2)
                    select new { c1, c2 }).OrderBy(t => t.c1.CustomerID).ThenBy(t => t.c2.CustomerID).Take(1),
                elementSorter: e => (e.c1.CustomerID, e.c2.CustomerID),
                elementAsserter: (e, a) =>
                {
                    AssertInclude(e.c1, a.c1, new ExpectedInclude<Customer>(c => c.Orders));
                    AssertEqual(e.c2, a.c2);
                });
        }

        public override Task Repro9735(bool async)
        {
            return AssertQuery(
                async,
                ss => ss.Set<Order>()
                    .Include(b => b.OrderDetails)
                    .OrderBy(b => b.Customer.CustomerID != null)
                    .ThenBy(b => b.Customer != null ? b.Customer.CustomerID : string.Empty)
                    .ThenBy(b => b.EmployeeID) // Needs to be explicitly ordered by EmployeeID as well
                    .Take(2));
        }

        public override Task Include_collection_with_multiple_conditional_order_by(bool async)
        {
            return AssertQuery(
                async,
                ss => ss.Set<Order>()
                    .Include(c => c.OrderDetails)
                    .OrderBy(o => o.OrderID > 0)
                    .ThenBy(o => o.Customer != null ? o.Customer.City : string.Empty)
                    .ThenBy(b => b.EmployeeID) // Needs to be explicitly ordered by EmployeeID as well
                    .Take(5),
                elementAsserter: (e, a) => AssertInclude(e, a, new ExpectedInclude<Order>(o => o.OrderDetails)));
        }

        // Used to track down a bug in Oracle's MySQL implementation, related to `SELECT ... ORDER BY (SELECT 1)`.
        public override Task Include_collection_OrderBy_empty_list_contains(bool async)
            => base.Include_collection_OrderBy_empty_list_contains(async);

        [ConditionalTheory(Skip = "https://github.com/dotnet/efcore/issues/21202")]
        public override Task Include_collection_skip_no_order_by(bool async)
        {
            return base.Include_collection_skip_no_order_by(async);
        }

        [ConditionalTheory(Skip = "https://github.com/dotnet/efcore/issues/21202")]
        public override Task Include_collection_skip_take_no_order_by(bool async)
        {
            return base.Include_collection_skip_take_no_order_by(async);
        }

        [ConditionalTheory(Skip = "https://github.com/dotnet/efcore/issues/21202")]
        public override Task Include_collection_take_no_order_by(bool async)
        {
            return base.Include_collection_take_no_order_by(async);
        }

        [ConditionalTheory(Skip = "https://github.com/dotnet/efcore/issues/21202")]
        public override Task Include_duplicate_collection_result_operator(bool async)
        {
            return base.Include_duplicate_collection_result_operator(async);
        }

        public class NorthwindSplitIncludeNoTrackingQueryMySqlFixture : NorthwindQueryMySqlFixture<NoopModelCustomizer>
        {
            // We used our `DebugRelationalCommandBuilderFactory` implementation to track down a
            // bug in Oracle's MySQL implementation, related to `SELECT ... ORDER BY (SELECT 1)`.
            protected override IServiceCollection AddServices(IServiceCollection serviceCollection)
                => base.AddServices(serviceCollection)
                    .AddSingleton<IRelationalCommandBuilderFactory, DebugRelationalCommandBuilderFactory>();
        }
    }
}

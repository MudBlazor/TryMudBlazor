namespace Tests
{
    using System.Linq;
    using Microsoft.AspNetCore.Mvc;
    using MudBlazor.Examples.Data.Models;
    using NUnit.Framework;
    using TryMudBlazor.Server.Controllers;

    /// <summary>
    /// The MudBlazor docs multi level grouping example loads its rows from webapi/company, so TryMudBlazor has to serve
    /// the same records as the docs site.
    /// </summary>
    public class CompanyControllerTests
    {
        [Test]
        public void ServesTheSameRoutesAsTheOtherExampleControllers()
        {
            var routes = typeof(CompanyController).GetCustomAttributes(typeof(RouteAttribute), false)
                .Cast<RouteAttribute>()
                .Select(r => r.Template);

            Assert.That(routes, Is.EquivalentTo(new[] { "webapi/[controller]", "wasm/webapi/[controller]" }));
            Assert.That(typeof(CompanyController).GetCustomAttributes(typeof(ApiControllerAttribute), false), Is.Not.Empty);
        }

        [Test]
        public void ReturnsTheTwelveDocsRecords()
        {
            var records = new CompanyController().Get().ToList();

            Assert.That(records, Has.Count.EqualTo(12));
            Assert.That(records.Select(r => r.Name), Is.EqualTo(new[]
            {
                "Alice", "Bob", "Carol", "Dave", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack", "Kelly", "Liam",
            }));
            Assert.That(records.Select(r => r.Department).Distinct(), Is.EqualTo(new[] { "Engineering", "Sales", "Support" }));
            Assert.That(records.All(r => r.Team is not null && r.Role is not null && r.Score > 0), Is.True);
        }

        [Test]
        public void RecordsKeepTheirGroupingValues()
        {
            var alice = new CompanyController().Get().Single(r => r.Name == "Alice");

            Assert.That(alice, Is.InstanceOf<Company>());
            Assert.That((alice.Department, alice.Team, alice.Role, alice.Score), Is.EqualTo(("Engineering", "Platform", "Backend", 120)));
        }
    }
}

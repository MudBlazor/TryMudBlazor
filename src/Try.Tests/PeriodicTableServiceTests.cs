namespace Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using MudBlazor.Examples.Data;
    using MudBlazor.Examples.Data.Models;
    using NUnit.Framework;

    public class PeriodicTableServiceTests
    {
        [Test]
        public async Task TheTableIsParsedOnceAndSharedBetweenCalls()
        {
            var first = (await new PeriodicTableService().GetElements()).ToList();
            var second = (await new PeriodicTableService().GetElements()).ToList();

            Assert.That(first, Is.Not.Empty);
            Assert.That(second, Has.Count.EqualTo(first.Count));
            Assert.That(first.Zip(second).All(pair => ReferenceEquals(pair.First, pair.Second)), Is.True, "a second call must reuse the parsed elements");
        }

        [TestCase("")]
        [TestCase("he")]
        [TestCase("GOLD")]
        [TestCase("Au")]
        [TestCase("no such element")]
        public async Task SearchMatchesTheOriginalSignPlusNameRule(string search)
        {
            var service = new PeriodicTableService();
            var all = (await service.GetElements()).ToList();
            var expected = all.Where(elm => (elm.Sign + elm.Name).Contains(search, StringComparison.InvariantCultureIgnoreCase)).ToList();

            var actual = (await service.GetElements(search)).ToList();

            Assert.That(actual, Is.EqualTo(expected));
            if (search == "he")
            {
                Assert.That(actual.Select(e => e.Name), Does.Contain("Helium"));
            }
        }

        [Test]
        public async Task ConcurrentReadersSeeTheSameTable()
        {
            var reads = await Task.WhenAll(Enumerable.Range(0, 50)
                .Select(_ => Task.Run(async () => (await new PeriodicTableService().GetElements()).ToList())));

            Assert.That(reads[0], Is.Not.Empty);
            Assert.That(reads.Select(r => r.Count), Has.All.EqualTo(reads[0].Count));
            Assert.That(reads.Select(r => r[0]).Distinct().Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task ResultsCannotBeUsedToChangeTheSharedTable()
        {
            var elements = await new PeriodicTableService().GetElements();

            Assert.That(elements, Is.Not.InstanceOf<List<Element>>());
            Assert.Throws<NotSupportedException>(() => ((IList<Element>)elements).Add(new Element { Name = "Unobtainium" }));
            Assert.Throws<NotSupportedException>(() => ((IList<Element>)elements).Clear());

            var hydrogen = elements.First();
            Assert.That(hydrogen.Electrons, Is.InstanceOf<ImmutableArray<int>>());
            Assert.That(typeof(Element).GetProperties().Select(p => p.SetMethod).Where(m => m is not null)
                .All(m => m.ReturnParameter.GetRequiredCustomModifiers().Any(t => t.Name == "IsExternalInit")), Is.True, "every Element property must be init-only");
        }
    }
}

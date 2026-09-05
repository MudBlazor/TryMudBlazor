using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using MudBlazor.Examples.Data.Models;

namespace MudBlazor.Examples.Data
{
    public class PeriodicTableService : IPeriodicTableService
    {
        // The table is an embedded resource that never changes, so parse it once for the lifetime of the process.
        // Every caller gets the same immutable array of immutable elements, so nothing a consumer does to a
        // result can leak into the next request.
        private static readonly Lazy<ImmutableArray<Element>> Elements = new(LoadElements);

        public Task<IEnumerable<Element>> GetElements()
        {
            return GetElements(string.Empty);
        }

        public Task<IEnumerable<Element>> GetElements(string search = "")
        {
            IEnumerable<Element> elements = Elements.Value;
            if (!string.IsNullOrEmpty(search))
            {
                elements = elements.Where(elm => (elm.Sign + elm.Name).Contains(search, StringComparison.InvariantCultureIgnoreCase));
            }

            return Task.FromResult(elements);
        }

        public static string GetResourceKey(Assembly assembly, string embeddedFile)
        {
            return assembly.GetManifestResourceNames().FirstOrDefault(x => x.Contains(embeddedFile));
        }

        private static ImmutableArray<Element> LoadElements()
        {
            var assembly = typeof(PeriodicTableService).Assembly;
            using var stream = assembly.GetManifestResourceStream(GetResourceKey(assembly, "Elements.json"));
            var table = JsonSerializer.Deserialize<Table>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return table.ElementGroups.SelectMany(elementGroup => elementGroup.Elements).ToImmutableArray();
        }
    }
}

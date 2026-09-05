using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace MudBlazor.Examples.Data.Models
{
    /// <summary>
    /// A row of the periodic table. Values are set once when the table is deserialised and never change, which
    /// lets PeriodicTableService hand the same instances to every request.
    /// </summary>
    public class Element
    {
        public string Group { get; init; }
        public int Position { get; init; }
        public string Name { get; init; }
        public int Number { get; init; }

        [JsonPropertyName("small")]
        public string Sign { get; init; }
        public double Molar { get; init; }
        public ImmutableArray<int> Electrons { get; init; }

        /// <summary>
        /// Overriding Equals is essential for use with Select and Table because they use HashSets internally
        /// </summary>
        public override bool Equals(object obj) => object.Equals(GetHashCode(), obj?.GetHashCode());

        /// <summary>
        /// Overriding GetHashCode is essential for use with Select and Table because they use HashSets internally
        /// </summary>
        public override int GetHashCode() => Name?.GetHashCode() ?? 0;

        public override string ToString() => $"{Sign} - {Name}";
    }
}

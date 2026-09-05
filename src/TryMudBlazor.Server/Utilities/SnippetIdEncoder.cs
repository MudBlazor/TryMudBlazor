namespace TryMudBlazor.Server.Utilities;

public static class SnippetsEncoder
{
    private static readonly IDictionary<char, char> LetterToDigitIdMappings = new Dictionary<char, char>
    {
        ['a'] = '0',
        ['k'] = '0',
        ['u'] = '0',
        ['E'] = '0',
        ['O'] = '0',
        ['Y'] = '0',
        ['b'] = '1',
        ['l'] = '1',
        ['v'] = '1',
        ['F'] = '1',
        ['P'] = '1',
        ['c'] = '2',
        ['m'] = '2',
        ['w'] = '2',
        ['G'] = '2',
        ['Q'] = '2',
        ['d'] = '3',
        ['n'] = '3',
        ['x'] = '3',
        ['H'] = '3',
        ['R'] = '3',
        ['e'] = '4',
        ['o'] = '4',
        ['y'] = '4',
        ['I'] = '4',
        ['S'] = '4',
        ['f'] = '5',
        ['p'] = '5',
        ['z'] = '5',
        ['J'] = '5',
        ['T'] = '5',
        ['g'] = '6',
        ['q'] = '6',
        ['A'] = '6',
        ['K'] = '6',
        ['U'] = '6',
        ['h'] = '7',
        ['r'] = '7',
        ['B'] = '7',
        ['L'] = '7',
        ['V'] = '7',
        ['i'] = '8',
        ['s'] = '8',
        ['C'] = '8',
        ['M'] = '8',
        ['W'] = '8',
        ['j'] = '9',
        ['t'] = '9',
        ['D'] = '9',
        ['N'] = '9',
        ['X'] = '9',
        ['Z'] = '9',
    };

    // Index 0-9: every letter that stands for that digit.
    private static readonly string[] LettersForDigit = Enumerable.Range(0, 10)
        .Select(digit => new string(LetterToDigitIdMappings.Where(kv => kv.Value == (char)('0' + digit)).Select(kv => kv.Key).ToArray()))
        .ToArray();

    public static string EncodeSnippetId(string snippetId)
    {
        return string.Create(snippetId.Length, snippetId, static (encoded, digits) =>
        {
            for (var i = 0; i < digits.Length; i++)
            {
                var letters = LettersForDigit[digits[i] - '0'];
                encoded[i] = letters[Random.Shared.Next(letters.Length)];
            }
        });
    }

    public const int SnippetIdLength = 16;

    /// <summary>
    /// Decodes a public snippet ID back to the 16-digit storage ID.
    /// </summary>
    /// <exception cref="InvalidDataException">The ID has the wrong length or contains characters outside the alphabet.</exception>
    public static string DecodeSnippetId(string encoded)
    {
        if (encoded is null || encoded.Length != SnippetIdLength)
        {
            throw new InvalidDataException("Invalid snippet ID");
        }

        var decoded = new char[SnippetIdLength];
        for (var i = 0; i < encoded.Length; i++)
        {
            if (!LetterToDigitIdMappings.TryGetValue(encoded[i], out decoded[i]))
            {
                throw new InvalidDataException("Invalid snippet ID");
            }
        }

        return new string(decoded);
    }
}
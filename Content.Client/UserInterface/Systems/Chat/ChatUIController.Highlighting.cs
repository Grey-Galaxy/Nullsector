using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.CharacterInfo;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using static System.Text.RegularExpressions.Regex;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client.UserInterface.Systems.Chat;

/// <summary>
/// A partial class of ChatUIController that handles the saving and loading of highlights for the chatbox.
/// It also makes use of the CharacterInfoSystem to optionally generate highlights based on the character's info.
/// </summary>
[SuppressMessage("Usage", "RA0026:Use of uncached static Regex function")]
[SuppressMessage("Performance", "SYSLIB1045:Convert to \'GeneratedRegexAttribute\'.")]
public sealed partial class ChatUIController : IOnSystemChanged<CharacterInfoSystem>
{
    [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;

    /// <summary>
    ///     The list of words to be highlighted in the chatbox.
    /// </summary>
    private readonly List<string> _highlights = [];

    /// <summary>
    ///     The string holding the hex color used to highlight words.
    /// </summary>
    private string? _highlightsColor;

    /// <summary>
    /// A list of effectively-static special entries that have unique colours
    /// </summary>
    private Dictionary<string, string> _specialHighlights = [];

    private bool _autoFillHighlightsEnabled;

    /// <summary>
    ///     The boolean that keeps track of the 'OnCharacterUpdated' event, whenever it's a player attaching or opening the character info panel.
    /// </summary>
    private bool _charInfoIsAttach = false;

    public event Action<string>? HighlightsUpdated;

    private void InitializeHighlights()
    {
        _config.OnValueChanged(CCVars.ChatAutoFillHighlights, (value) => { _autoFillHighlightsEnabled = value; }, true);

        _config.OnValueChanged(CCVars.ChatHighlightsColor, (value) => { _highlightsColor = value; }, true);

        // Load highlights if any were saved.
        string highlights = _config.GetCVar(CCVars.ChatHighlights);

        if (!string.IsNullOrEmpty(highlights))
        {
            UpdateHighlights(highlights, true);
        }
    }

    public void OnSystemLoaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate += OnCharacterUpdated;
        _specialHighlights = InitializeSpecialHighlights();
    }

    public void OnSystemUnloaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate -= OnCharacterUpdated;
        _specialHighlights.Clear();
    }

    private void UpdateAutoFillHighlights()
    {
        if (!_autoFillHighlightsEnabled)
            return;

        // If auto highlights are enabled generate a request for new character info
        // that will be used to determine the highlights.
        _charInfoIsAttach = true;
        _characterInfo.RequestCharacterInfo();
    }

    public void UpdateHighlights(string newHighlights, bool firstLoad = false)
    {
        // Do nothing if the provided highlights are the same as the old ones and it is not the first time.
        if (!firstLoad && _config.GetCVar(CCVars.ChatHighlights)
                .Equals(newHighlights, StringComparison.CurrentCultureIgnoreCase))
            return;

        _config.SetCVar(CCVars.ChatHighlights, newHighlights);
        _config.SaveToFile();

        _highlights.Clear();

        // We first subdivide the highlights based on newlines to prevent replacing
        // a valid "\n" tag and adding it to the final regex.
        var splitHighlights =
            newHighlights.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var splitHighlight in splitHighlights)
        {
            // Replace every "\" character with a "\\" to prevent "\n", "\0", etc...
            var keyword = splitHighlight.Replace(@"\", @"\\");

            // Escape the keyword to prevent special characters like "(" and ")" to be considered valid regex.
            keyword = Escape(keyword);

            // 1. Since the "["s in WrappedMessage are already sanitized, add 2 extra "\"s
            // to make sure it matches the literal "\" before the square bracket.
            keyword = keyword.Replace(@"\[", @"\\\[");

            // If present, replace the double quotes at the edges with tags
            // that make sure the words to match are separated by spaces or punctuation.
            // NOTE: The reason why we don't use \b tags is that \b doesn't match reverse slash characters "\" so
            // a pre-sanitized (see 1.) string like "\[test]" wouldn't get picked up by the \b.
            if (keyword.Any(c => c == '"'))
            {
                // Matches the last double quote character.
                keyword = Replace(keyword, "\"$", "(?!\\w)");
                // When matching for the first double quote character we also consider the possibility
                // of the double quote being preceded by a @ character.
                keyword = Replace(keyword, "^\"|(?<=^@)\"", "(?<!\\w)");
            }

            // Make sure any name tagged as ours gets highlighted only when others say it.
            keyword = Replace(keyword, "^@", "(?<=(?<=/name.*)|(?<=,.*\"\".*))");

            _highlights.Add(keyword);
        }

        // Arrange the list of highlights in descending order so that when highlighting,
        // the full word (e.g. "Security") gets picked before the abbreviation (e.g. "Sec").
        _highlights.Sort((x, y) => y.Length.CompareTo(x.Length));
    }

    private void OnCharacterUpdated(CharacterData data)
    {
        // If _charInfoIsAttach is false then the opening of the character panel was the one
        // to generate the event, dismiss it.
        if (!_charInfoIsAttach)
            return;

        var (_, job, _, _, entityName) = data;

        // Mark this entity's name as our character name for the "UpdateHighlights" function.
        var newHighlights = "@" + entityName;

        // Subdivide the character's name based on spaces or hyphens so that every word gets highlighted.
        if (newHighlights.Count(c => (c == ' ' || c == '-')) == 1)
            newHighlights = newHighlights.Replace("-", "\n@").Replace(" ", "\n@");

        // If the character has a name with more than one hyphen assume it is a lizard name and extract the first and
        // last name eg. "Eats-The-Food" -> "@Eats" "@Food"
        if (newHighlights.Count(c => c == '-') > 1)
            newHighlights = newHighlights.Split('-')[0] + "\n@" + newHighlights.Split('-')[^1];

        // Convert the job title to kebab-case and use it as a key for the loc file.
        string jobKey = job.Replace(' ', '-').ToLower();

        if (Loc.TryGetString($"highlights-{jobKey}", out var jobMatches))
            newHighlights += '\n' + jobMatches.Replace(", ", "\n");

        UpdateHighlights(newHighlights);
        HighlightsUpdated?.Invoke(newHighlights);
        _charInfoIsAttach = false;
    }

    // Null Sector Implementation -Z
    /// <summary>
    /// Gets all entries of Locale/en-US/_Null/chat/highlights_special.ftl and parses the file for highlighting entries.
    /// </summary>
    private Dictionary<string, string> InitializeSpecialHighlights()
    {
        Dictionary<string, string> highlights = [];
        /* Generic Incrementer up to 100. Short-circuits but *will* break if the actual list is larger than 100.
         * Note: It may not be expandable, but it suffices for the Null Sector, for now. The solution is to increment
         * this hypothetical "max limit" even further, or simply define the amount elsewhere like how SS14
         * does so as of 20250713. This usually takes the form of a CVar, but it makes little difference here.
         */
        for (var i = 0; i < 100; i++)
        {
            // Check if the localization actually exists.
            if (!Loc.TryGetString($"highlights-special-{i}", out var unfilteredLocale))
                break; // If the highlight isn't found, it's probably because the incrementation is above the actual amount.
            var match = Match(unfilteredLocale, @"\[(.*?)\]"); // Select HEX value in localization's brackets.
            var cleanedLocale = Replace(unfilteredLocale, @"\[.*", ""); // Clean up the hex.
            var hex = match.Success ? match.Groups[1].Value : "FFFFFF"; // White by default.
            highlights.Add(cleanedLocale, hex);
        }
        return highlights;
    }
}

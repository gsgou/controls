using System.Collections.Frozen;
using System.Text;

namespace Shiny.Maui.Controls;

/// <summary>
/// The passwords that turn up at the top of every credential dump, and the arithmetic for spotting
/// one wearing a disguise.
/// </summary>
/// <remarks>
/// <para>
/// This is deliberately a short list rather than a bundled ten-million-line wordlist: a package that
/// every MAUI and Blazor app links has no business carrying tens of megabytes, and the long tail is
/// better served by a real breach corpus. The list here catches the passwords people actually pick
/// when left to themselves, which is the overwhelming majority of weak ones.
/// </para>
/// <para>
/// For the long tail, implement <see cref="IPasswordStrengthEvaluator"/> over a Have I Been Pwned
/// range query — see that interface's remarks for the one rule that matters when you do.
/// </para>
/// </remarks>
public static class CommonPasswords
{
    /// <summary>The shortest run that is worth treating as a known word rather than a coincidence.</summary>
    const int MinimumMatchLength = 4;

    static readonly FrozenSet<string> words = new[]
    {
        // straight from the top of the annual "worst passwords" lists
        "123456", "password", "123456789", "12345678", "12345", "1234567", "1234567890", "qwerty",
        "abc123", "111111", "123123", "1234", "iloveyou", "000000", "picture1", "senha", "1234561",
        "123321", "654321", "666666", "7777777", "123qwe", "qwertyuiop", "121212", "1q2w3e4r",
        "1qaz2wsx", "qwerty123", "zxcvbnm", "asdfghjkl", "qazwsx", "1q2w3e", "qwe123", "aa123456",
        "555555", "112233", "888888", "159753", "987654321", "12341234", "11111111", "222222",
        "999999", "333333", "444444", "777777", "101010", "252525", "696969", "123abc", "a123456",
        "asdasd", "asdfgh", "zaq12wsx", "1qazxsw2", "q1w2e3r4", "qwertyui", "qwer1234", "qweasdzxc",
        "poiuytrewq", "mnbvcxz", "1111", "0000", "2000", "1212", "6969", "4321", "1122", "2020",
        "2021", "2022", "2023", "2024", "2025",

        // names, words and pop culture
        "monkey", "dragon", "letmein", "trustno1", "sunshine", "master", "shadow", "michael",
        "superman", "batman", "football", "baseball", "soccer", "hockey", "basketball", "jordan",
        "harley", "ranger", "hunter", "buster", "thomas", "robert", "matthew", "daniel", "andrew",
        "joshua", "jessica", "ashley", "amanda", "jennifer", "michelle", "nicole", "hannah",
        "samantha", "charlie", "george", "william", "richard", "patrick", "anthony", "maggie",
        "summer", "winter", "spring", "autumn", "orange", "purple", "yellow", "silver", "golden",
        "flower", "butterfly", "princess", "angel", "chocolate", "cookie", "peanut", "pepper",
        "ginger", "cheese", "banana", "apple", "mustang", "corvette", "ferrari", "porsche",
        "harleydavidson", "yamaha", "starwars", "startrek", "pokemon", "minecraft", "fortnite",
        "naruto", "gandalf", "matrix", "hello", "welcome", "freedom", "whatever", "computer",
        "internet", "samsung", "google", "facebook", "linkedin", "twitter", "myspace", "yahoo",
        "hotmail", "gmail", "android", "iphone", "windows", "microsoft", "oracle", "adobe",

        // the ones sysadmins leave behind
        "admin", "administrator", "root", "guest", "user", "test", "demo", "default", "changeme",
        "temp", "temporary", "password1", "passw0rd", "pass", "passwd", "secret", "login",
        "access", "manager", "service", "system", "backup", "database", "server", "public",
        "private", "toor", "letmein1", "welcome1", "admin123", "root123", "test123", "pass123",
        "abcd1234", "abcdefg", "abcdef", "abcdefgh", "aaaaaa", "aaaaaaaa", "asdf1234",

        // profanity and mashing, which is what people reach for when told "add a symbol"
        "fuckyou", "fuckoff", "bullshit", "asshole", "biteme", "whatever1", "nothing", "blahblah",
        "asdfasdf", "qweqwe", "zxczxc", "lolololo", "hahaha", "trustme", "iamgod", "hello123",
        "hellothere", "nopassword", "noneofyourbusiness",

        // things people think are clever
        "correcthorsebatterystaple", "letmeinplease", "ilovemyself", "thisismypassword",
        "mypassword", "newpassword", "oldpassword", "notmypassword", "supersecret", "topsecret",
        "opensesame", "abracadabra", "iloveyou1", "loveme", "lovely", "forever", "together"
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Look-alike characters, so <c>P@ssw0rd</c> is recognised as <c>password</c>.</summary>
    static readonly Dictionary<char, char> leet = new()
    {
        ['@'] = 'a', ['4'] = 'a', ['8'] = 'b', ['('] = 'c', ['<'] = 'c', ['3'] = 'e', ['6'] = 'g',
        ['9'] = 'g', ['1'] = 'i', ['!'] = 'i', ['|'] = 'i', ['0'] = 'o', ['$'] = 's', ['5'] = 's',
        ['7'] = 't', ['+'] = 't', ['2'] = 'z'
    };

    /// <summary>
    /// Whether the password is one of the known-bad values, allowing for case, look-alike characters
    /// and the year or exclamation mark bolted on the end to satisfy a composition rule.
    /// </summary>
    public static bool IsCompromised(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        foreach (var candidate in Variants(password))
        {
            if (words.Contains(candidate))
                return true;
        }
        return false;
    }

    /// <summary>
    /// The longest known-bad word buried inside the password, or null. Used for scoring rather than
    /// for the pass/fail rule: <c>monkey7391</c> is not literally a breached password, but the six
    /// characters at the front of it are worth nothing.
    /// </summary>
    public static string? FindLongestMatch(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return null;

        var haystack = Unleet(password.ToLowerInvariant());
        string? longest = null;

        foreach (var word in words)
        {
            if (word.Length < MinimumMatchLength || word.Length <= (longest?.Length ?? 0))
                continue;

            if (haystack.Contains(word, StringComparison.Ordinal))
                longest = word;
        }
        return longest;
    }

    /// <summary>Every spelling of the password worth checking against the list.</summary>
    static IEnumerable<string> Variants(string password)
    {
        var lower = password.ToLowerInvariant();
        yield return lower;

        var unleeted = Unleet(lower);
        if (!string.Equals(unleeted, lower, StringComparison.Ordinal))
            yield return unleeted;

        // "password2024!" and "P@ssw0rd1" are the same password with policy tax attached
        var trimmed = TrimDecoration(lower);
        if (trimmed.Length > 0 && !string.Equals(trimmed, lower, StringComparison.Ordinal))
        {
            yield return trimmed;

            var trimmedUnleeted = Unleet(trimmed);
            if (!string.Equals(trimmedUnleeted, trimmed, StringComparison.Ordinal))
                yield return trimmedUnleeted;
        }

        var unleetedTrimmed = TrimDecoration(unleeted);
        if (unleetedTrimmed.Length > 0 && !string.Equals(unleetedTrimmed, unleeted, StringComparison.Ordinal))
            yield return unleetedTrimmed;
    }

    static string Unleet(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
            builder.Append(leet.TryGetValue(c, out var replacement) ? replacement : c);

        return builder.ToString();
    }

    /// <summary>Strips the digits and punctuation people bolt onto either end of a real word.</summary>
    static string TrimDecoration(string value)
    {
        var start = 0;
        var end = value.Length;

        while (start < end && !char.IsLetter(value[start]))
            start++;

        while (end > start && !char.IsLetter(value[end - 1]))
            end--;

        return value[start..end];
    }
}

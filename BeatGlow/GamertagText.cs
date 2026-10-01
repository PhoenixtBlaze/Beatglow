namespace BeatGlow
{
    /// <summary>
    /// One line of plain text. Markup characters are removed so the name cannot change size or color through tags.
    /// </summary>
    internal static class GamertagText
    {
        internal const int MaxLength = 32;

        internal static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            char[] chars = new char[value.Length];
            int count = 0;
            for (int i = 0; i < value.Length && count < MaxLength; i++)
            {
                char c = value[i];
                if (c < ' ' || c == '<' || c == '>')
                    continue;
                chars[count++] = c;
            }

            return new string(chars, 0, count).Trim();
        }
    }
}

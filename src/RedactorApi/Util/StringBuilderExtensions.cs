namespace RedactorApi.Util;

/// <summary>
/// THis class trims String builders and should only be used when the length of the string builder
/// Is greater than +500 chars
/// </summary>
public static class StringBuilderExtensions
{

    public static string Trim(this StringBuilder sb)
    {
        sb.TrimEnd();

        switch (sb.Length)
        {
            case > 0 when char.IsWhiteSpace(sb[0]):
            {
                for (var i = 0; i < sb.Length; i++)
                {
                    if (!char.IsWhiteSpace(sb[i]))
                    {
                        return sb.ToString(i, sb.Length - i);
                    }
                }

                return string.Empty;
            }
            default:
                return sb.ToString();
        }
    }

    public static StringBuilder TrimStart(this StringBuilder sb)
    {
        var count = 0;

        for (var i = 0; i < sb.Length; i++)
        {
            if (!char.IsWhiteSpace(sb[i]))
            {
                break;
            }

            count++;
        }

        if (count > 0)
        {
            sb.Remove(0, count);
        }
        return sb;
    }

    public static StringBuilder TrimEnd(this StringBuilder sb)
    {
        var i = sb.Length - 1;
        for (; i >= 0; i--)
        {
            if (!char.IsWhiteSpace(sb[i]))
            {
                break;
            }
        }
        if (i < sb.Length - 1)
        {
            sb.Length = i + 1;
        }

        return sb;
    }
}

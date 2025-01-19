namespace RedactorApi.Util;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// namespace System.Text
/// <summary>Provide a cached reusable instance of stringbuilder per thread.</summary>
public static class StringBuilderCache
{
    // The value 360 was chosen in discussion with performance experts as a compromise between using
    // as little memory per thread as possible and still covering a large part of short-lived
    // StringBuilder creations on the startup path of VS designers.
    internal const int MaxBuilderSize = 4096; //360;
    private const int DefaultCapacity = 2048; //16; // == StringBuilder.DefaultCapacity

    [ThreadStatic] private static StringBuilder? _tCachedInstance;

    /// <summary>Get a StringBuilder for the specified capacity.</summary>
    /// <remarks>If a StringBuilder of an appropriate size is cached, it will be returned and the cache emptied.</remarks>
    public static StringBuilder Acquire(int capacity = DefaultCapacity)
    {
        if (capacity <= MaxBuilderSize)
        {
            var sb = _tCachedInstance;
            if (sb != null)
            {
                // Avoid stringbuilder block fragmentation by getting a new StringBuilder
                // when the requested size is larger than the current capacity
                if (capacity <= sb.Capacity)
                {
                    _tCachedInstance = null;
                    sb.Clear();
                    return sb;
                }
            }
        }

        return new StringBuilder(capacity);
    }

    /// <summary>Place the specified builder in the cache if it is not too big.</summary>
    public static void Release(StringBuilder sb)
    {
        if (sb.Capacity <= MaxBuilderSize)
        {
            _tCachedInstance = sb;
        }
    }

    /// <summary>ToString() the string builder, Release it to the cache, and return the resulting string.</summary>
    public static string GetStringAndRelease(this StringBuilder sb)
    {
        var result = sb.ToString();
        Release(sb);
        return result;
    }

    public static string TrimAndRelease(this StringBuilder sb)
    {
        var result = sb.Trim();
        Release(sb);
        return result;
    }
    public static string StartTrimAndRelease(this StringBuilder sb)
    {
        var result = sb.TrimStart().ToString();
        Release(sb);
        return result;
    }
    public static string EndTrimAndRelease(this StringBuilder sb)
    {
        var result = sb.TrimEnd().ToString();
        Release(sb);
        return result;
    }
}

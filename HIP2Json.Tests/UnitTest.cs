using System;
using System.Collections.Generic;

namespace HIP2Json.Tests;

public enum FailKind
{
    BinaryPack,
    Serialization,
    Syntax,
    Crash,
}

public sealed class UnitTest
{
    public string Name;
    public int Passed;
    public int Failed;
    public List<string> Failures = new();
    public List<FailKind> FailKinds = new();

    public UnitTest(string name)
    {
        Name = name;
    }

    public void Assert(bool condition, string what, FailKind kind)
    {
        if (condition)
        {
            Passed++;
        }
        else
        {
            Failed++;
            Failures.Add(what);
            FailKinds.Add(kind);
        }
    }

    public void AssertEqual<T>(T expected, T actual, string what, FailKind kind)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Passed++;
        }
        else
        {
            Failed++;
            Failures.Add($"{what}: expected <{expected}> but got <{actual}>");
            FailKinds.Add(kind);
        }
    }

    public bool AllPass => Failed == 0;
}
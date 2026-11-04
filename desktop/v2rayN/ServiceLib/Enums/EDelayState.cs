namespace ServiceLib.Enums;

/// <summary>Latency category used to colour ping results (see branding/DESIGN.md).</summary>
public enum EDelayState
{
    /// <summary>Never tested (n/a).</summary>
    Unknown = 0,

    Testing = 1,

    /// <summary>&lt; 300 ms.</summary>
    Good = 2,

    /// <summary>300-800 ms.</summary>
    Warning = 3,

    /// <summary>&gt; 800 ms.</summary>
    Bad = 4,

    Timeout = 5,
}

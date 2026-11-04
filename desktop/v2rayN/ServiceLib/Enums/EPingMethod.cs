namespace ServiceLib.Enums;

public enum EPingMethod
{
    /// <summary>Real delay through the core, HTTP GET to the test URL (default).</summary>
    ProxyGet = 0,

    /// <summary>Real delay through the core, HTTP HEAD to the test URL.</summary>
    ProxyHead = 1,

    /// <summary>Plain TCP connect to the server address.</summary>
    Tcp = 2,
}

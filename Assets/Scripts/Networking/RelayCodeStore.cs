using UnityEngine;

/// <summary>
/// Static class to persist relay join code across scene changes.
/// The relay connection remains active via NetworkManager (DontDestroyOnLoad),
/// but the join code needs to be stored separately for UI display.
/// </summary>
public static class RelayCodeStore
{
    // The current relay join code
    private static string _currentJoinCode = string.Empty;
    
    // Whether a relay session is currently active
    private static bool _isRelayActive = false;

    /// <summary>
    /// Store the join code when relay is created
    /// </summary>
    public static void SetJoinCode(string joinCode)
    {
        _currentJoinCode = joinCode;
        _isRelayActive = !string.IsNullOrEmpty(joinCode);
        Debug.Log($"RelayCodeStore: Join code stored: {joinCode}");
    }

    /// <summary>
    /// Get the current join code
    /// </summary>
    public static string GetJoinCode()
    {
        return _currentJoinCode;
    }

    /// <summary>
    /// Check if there's an active relay session with a valid code
    /// </summary>
    public static bool HasActiveRelay()
    {
        return _isRelayActive && !string.IsNullOrEmpty(_currentJoinCode);
    }

    /// <summary>
    /// Clear the join code (when relay is shut down)
    /// </summary>
    public static void Clear()
    {
        _currentJoinCode = string.Empty;
        _isRelayActive = false;
        Debug.Log("RelayCodeStore: Join code cleared");
    }
}

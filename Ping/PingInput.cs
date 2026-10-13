using UnityEngine;
using UnityEngine.InputSystem;

namespace Ping;

/// <summary>Detects the configured key. Tries the legacy Input class first and falls back to the Input System if the game disabled the legacy one.</summary>
public static class PingInput
{
    private static bool _legacyBroken;

    public static bool Pressed(KeyCode key)
    {
        if (!_legacyBroken)
        {
            try { return Input.GetKeyDown(key); }
            catch (System.Exception) { _legacyBroken = true; }
        }
        return InputSystemPressed(key);
    }

    private static bool InputSystemPressed(KeyCode key)
    {
        var mouse = Mouse.current;
        switch (key)
        {
            case KeyCode.Mouse0: return mouse != null && mouse.leftButton.wasPressedThisFrame;
            case KeyCode.Mouse1: return mouse != null && mouse.rightButton.wasPressedThisFrame;
            case KeyCode.Mouse2: return mouse != null && mouse.middleButton.wasPressedThisFrame;
            case KeyCode.Mouse3: return mouse != null && mouse.backButton.wasPressedThisFrame;
            case KeyCode.Mouse4: return mouse != null && mouse.forwardButton.wasPressedThisFrame;
        }
        var kb = Keyboard.current;
        if (kb != null && System.Enum.TryParse<Key>(key.ToString(), true, out var k) && k != Key.None)
            return kb[k].wasPressedThisFrame;
        return false;
    }
}

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Yangi Input System ham, eski Input Manager ham ishlaydi — qaysi biri yoqilgan bo'lsa.
public static class GameInput
{
#if ENABLE_INPUT_SYSTEM
    static bool IsHeld(Key key)
    {
        return Keyboard.current != null && Keyboard.current[key].isPressed;
    }

    static bool IsDown(Key key)
    {
        return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
    }

    public static Vector2 Move()
    {
        float x = (IsHeld(Key.D) ? 1f : 0f) - (IsHeld(Key.A) ? 1f : 0f);
        float y = (IsHeld(Key.W) ? 1f : 0f) - (IsHeld(Key.S) ? 1f : 0f);
        return new Vector2(x, y);
    }

    public static Vector2 Look()
    {
        if (Mouse.current == null) return Vector2.zero;
        return Mouse.current.delta.ReadValue() * 0.1f;
    }

    public static bool Run()
    {
        return IsHeld(Key.LeftShift);
    }

    public static bool InteractPressed()
    {
        bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return mouse || IsDown(Key.E);
    }

    public static bool PausePressed()
    {
        return IsDown(Key.Escape);
    }

    public static bool HelpPressed()
    {
        return IsDown(Key.H);
    }
#else
    public static Vector2 Move()
    {
        float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        float y = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        return new Vector2(x, y);
    }

    public static Vector2 Look()
    {
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
    }

    public static bool Run()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }

    public static bool InteractPressed()
    {
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E);
    }

    public static bool PausePressed()
    {
        return Input.GetKeyDown(KeyCode.Escape);
    }

    public static bool HelpPressed()
    {
        return Input.GetKeyDown(KeyCode.H);
    }
#endif
}

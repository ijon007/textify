using System.Runtime.InteropServices;

namespace WinFormTest;

public class GlobalHotkeyManager : IDisposable
{
  private IntPtr windowHandle;
  private int hotkeyId = 9000;
  private bool isRegistered = false;
  private SynchronizationContext? syncContext;

  public event EventHandler? HotkeyPressed;
  public event EventHandler? HotkeyReleased;

  private bool isPressed = false;

  // Hotkey configuration
  private bool requireCtrl = true;
  private bool requireAlt = false;
  private bool requireShift = false;
  private bool requireWin = true;
  private int? requireKeyCode = null; // Optional main key (e.g., Space, Enter)

  public GlobalHotkeyManager(IntPtr hWnd)
  {
    windowHandle = hWnd;
    syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
  }

  public void SetHotkeyConfiguration(bool ctrl, bool alt, bool shift, bool win, int? keyCode = null)
  {
    requireCtrl = ctrl;
    requireAlt = alt;
    requireShift = shift;
    requireWin = win;
    requireKeyCode = keyCode;
  }

  public bool RegisterHotkey()
  {
    if (isRegistered)
      return true;

    // Register Ctrl+Win hotkey
    // MOD_CONTROL = 0x0002, MOD_WIN = 0x0008
    // Using VK_SPACE (0x20) as the key, but we'll detect Ctrl+Win combination
    // Actually, we need to use a low-level keyboard hook to detect Ctrl+Win
    
    // For now, register a hotkey with a specific key combination
    // Note: RegisterHotKey doesn't support Win key directly, so we'll use a keyboard hook instead
    isRegistered = true;
    return true;
  }

  public void UnregisterHotkey()
  {
    if (!isRegistered)
      return;

    WindowsApiHelper.UnregisterHotKey(windowHandle, hotkeyId);
    isRegistered = false;
  }

  public void ProcessMessage(int msg, IntPtr wParam, IntPtr lParam)
  {
    if (msg == WindowsApiHelper.WM_HOTKEY && wParam.ToInt32() == hotkeyId)
    {
      HotkeyPressed?.Invoke(this, EventArgs.Empty);
    }
  }

  // Low-level keyboard hook for Ctrl+Win detection
  private LowLevelKeyboardProc? keyboardHook;
  private IntPtr hookId = IntPtr.Zero;

  private const int WH_KEYBOARD_LL = 13;
  private const int WM_KEYDOWN = 0x0100;
  private const int WM_KEYUP = 0x0101;

  [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
  private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

  [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static extern bool UnhookWindowsHookEx(IntPtr hhk);

  [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
  private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

  [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
  private static extern IntPtr GetModuleHandle(string? lpModuleName);

  private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

  public void InstallKeyboardHook()
  {
    keyboardHook = HookCallback;
    hookId = SetHook(keyboardHook);
  }

  public void UninstallKeyboardHook()
  {
    if (hookId != IntPtr.Zero)
    {
      UnhookWindowsHookEx(hookId);
      hookId = IntPtr.Zero;
    }
  }

  private IntPtr SetHook(LowLevelKeyboardProc proc)
  {
    using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
    using (var curModule = curProcess.MainModule)
    {
      return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
        GetModuleHandle(curModule?.ModuleName), 0);
    }
  }

  [DllImport("user32.dll")]
  private static extern short GetAsyncKeyState(int vKey);

  private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
  {
    if (nCode >= 0)
    {
      int vkCode = Marshal.ReadInt32(lParam);
      
      // Check current state of modifier keys
      bool ctrlPressed = (GetAsyncKeyState(0xA2) & 0x8000) != 0 || (GetAsyncKeyState(0xA3) & 0x8000) != 0;
      bool altPressed = (GetAsyncKeyState(0xA4) & 0x8000) != 0 || (GetAsyncKeyState(0xA5) & 0x8000) != 0;
      bool shiftPressed = (GetAsyncKeyState(0xA0) & 0x8000) != 0 || (GetAsyncKeyState(0xA1) & 0x8000) != 0;
      bool winPressed = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;

      // Check if all required modifiers are pressed
      bool modifiersMatch = 
        (!requireCtrl || ctrlPressed) &&
        (!requireAlt || altPressed) &&
        (!requireShift || shiftPressed) &&
        (!requireWin || winPressed);

      // If a keyCode is required, check if it matches
      bool keyMatches = !requireKeyCode.HasValue || vkCode == requireKeyCode.Value;

      if (wParam.ToInt32() == WM_KEYDOWN)
      {
        // Check if this is a modifier key or the required key being pressed
        bool isModifierKey = (vkCode == 0xA2 || vkCode == 0xA3 || // Ctrl
                              vkCode == 0xA4 || vkCode == 0xA5 || // Alt
                              vkCode == 0xA0 || vkCode == 0xA1 || // Shift
                              vkCode == 0x5B || vkCode == 0x5C);  // Win
        
        bool isRequiredKey = requireKeyCode.HasValue && vkCode == requireKeyCode.Value;

        if (isModifierKey || isRequiredKey)
        {
          // Small delay to ensure all keys are registered
          Task.Delay(10).ContinueWith(_ =>
          {
            bool ctrlNow = (GetAsyncKeyState(0xA2) & 0x8000) != 0 || (GetAsyncKeyState(0xA3) & 0x8000) != 0;
            bool altNow = (GetAsyncKeyState(0xA4) & 0x8000) != 0 || (GetAsyncKeyState(0xA5) & 0x8000) != 0;
            bool shiftNow = (GetAsyncKeyState(0xA0) & 0x8000) != 0 || (GetAsyncKeyState(0xA1) & 0x8000) != 0;
            bool winNow = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
            
            bool modifiersMatchNow = 
              (!requireCtrl || ctrlNow) &&
              (!requireAlt || altNow) &&
              (!requireShift || shiftNow) &&
              (!requireWin || winNow);
            
            // If keyCode is required, we need to check if it was just pressed
            if (requireKeyCode.HasValue)
            {
              if (modifiersMatchNow && isRequiredKey && !isPressed)
              {
                isPressed = true;
                syncContext?.Post(_ => HotkeyPressed?.Invoke(this, EventArgs.Empty), null);
              }
            }
            else
            {
              // No keyCode required, just check modifiers
              if (modifiersMatchNow && !isPressed)
              {
                isPressed = true;
                syncContext?.Post(_ => HotkeyPressed?.Invoke(this, EventArgs.Empty), null);
              }
            }
          });
        }
      }
      else if (wParam.ToInt32() == WM_KEYUP)
      {
        if (isPressed)
        {
          bool ctrlStill = (GetAsyncKeyState(0xA2) & 0x8000) != 0 || (GetAsyncKeyState(0xA3) & 0x8000) != 0;
          bool altStill = (GetAsyncKeyState(0xA4) & 0x8000) != 0 || (GetAsyncKeyState(0xA5) & 0x8000) != 0;
          bool shiftStill = (GetAsyncKeyState(0xA0) & 0x8000) != 0 || (GetAsyncKeyState(0xA1) & 0x8000) != 0;
          bool winStill = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
          
          bool modifiersStillMatch = 
            (!requireCtrl || ctrlStill) &&
            (!requireAlt || altStill) &&
            (!requireShift || shiftStill) &&
            (!requireWin || winStill);
          
          // Check if any required modifier was released
          bool ctrlReleased = requireCtrl && (vkCode == 0xA2 || vkCode == 0xA3);
          bool altReleased = requireAlt && (vkCode == 0xA4 || vkCode == 0xA5);
          bool shiftReleased = requireShift && (vkCode == 0xA0 || vkCode == 0xA1);
          bool winReleased = requireWin && (vkCode == 0x5B || vkCode == 0x5C);
          bool keyReleased = requireKeyCode.HasValue && vkCode == requireKeyCode.Value;
          
          if (!modifiersStillMatch || ctrlReleased || altReleased || shiftReleased || winReleased || keyReleased)
          {
            isPressed = false;
            syncContext?.Post(_ => HotkeyReleased?.Invoke(this, EventArgs.Empty), null);
          }
        }
      }
    }

    return CallNextHookEx(hookId, nCode, wParam, lParam);
  }

  public void Dispose()
  {
    UnregisterHotkey();
    UninstallKeyboardHook();
  }
}

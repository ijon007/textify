using System.Runtime.InteropServices;

namespace WinFormTest;

public static class WindowsApiHelper
{
  // Hotkey registration
  [DllImport("user32.dll")]
  public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

  [DllImport("user32.dll")]
  public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

  // Window management
  [DllImport("user32.dll")]
  public static extern IntPtr GetForegroundWindow();

  [DllImport("user32.dll")]
  public static extern bool SetForegroundWindow(IntPtr hWnd);

  [DllImport("user32.dll")]
  public static extern short GetAsyncKeyState(int vKey);

  // SendInput for text injection
  [DllImport("user32.dll", SetLastError = true)]
  public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

  [StructLayout(LayoutKind.Sequential)]
  public struct INPUT
  {
    public uint type;
    public INPUTUNION U;
  }

  [StructLayout(LayoutKind.Explicit)]
  public struct INPUTUNION
  {
    [FieldOffset(0)]
    public KEYBDINPUT ki;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct KEYBDINPUT
  {
    public ushort wVk;
    public ushort wScan;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
  }

  public const uint INPUT_KEYBOARD = 1;
  public const uint KEYEVENTF_KEYUP = 0x0002;
  public const uint KEYEVENTF_UNICODE = 0x0004;

  // Modifier keys
  public const uint MOD_CONTROL = 0x0002;
  public const uint MOD_WIN = 0x0008;
  public const int WM_HOTKEY = 0x0312;

  // Window dragging
  [DllImport("user32.dll")]
  public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

  [DllImport("user32.dll")]
  public static extern bool ReleaseCapture();

  public const int WM_NCLBUTTONDOWN = 0xA1;
  public const int HT_CAPTION = 0x2;
  public const int WM_NCACTIVATE = 0x0086;

  // Window region for rounded corners
  [DllImport("gdi32.dll")]
  public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

  [DllImport("user32.dll")]
  public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

  [DllImport("gdi32.dll")]
  public static extern bool DeleteObject(IntPtr hObject);
}

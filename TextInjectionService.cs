using System.Windows.Forms;

namespace WinFormTest;

public class TextInjectionService
{
  private IntPtr originalForegroundWindow = IntPtr.Zero;

  public void SaveForegroundWindow(IntPtr hWnd)
  {
    originalForegroundWindow = hWnd;
  }

  public void InjectText(string text)
  {
    if (string.IsNullOrEmpty(text))
      return;

    try
    {
      // Restore focus to the original window
      if (originalForegroundWindow != IntPtr.Zero)
      {
        WindowsApiHelper.SetForegroundWindow(originalForegroundWindow);
        System.Threading.Thread.Sleep(200); // Give window time to receive focus
      }

      // Try clipboard method first (more reliable across apps)
      InjectViaClipboard(text);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"InjectText error: {ex.Message}");
      // Last resort: just set clipboard
      try
      {
        Clipboard.SetText(text);
      }
      catch
      {
        // Ignore clipboard errors
      }
    }
  }

  private void InjectViaClipboard(string text)
  {
    try
    {
      // Save current clipboard
      string? originalClipboard = null;
      try
      {
        originalClipboard = Clipboard.GetText();
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Failed to read clipboard: {ex.Message}");
      }

      // Clear and set text to clipboard
      Clipboard.Clear();
      System.Threading.Thread.Sleep(50);
      Clipboard.SetText(text);
      System.Threading.Thread.Sleep(50);
      
      // Verify clipboard was set
      string? verifyClipboard = null;
      try
      {
        verifyClipboard = Clipboard.GetText();
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Failed to verify clipboard: {ex.Message}");
      }

      if (verifyClipboard != text)
      {
        // Retry setting clipboard
        Clipboard.Clear();
        System.Threading.Thread.Sleep(50);
        Clipboard.SetText(text);
        System.Threading.Thread.Sleep(100);
      }

      // Ensure window still has focus
      if (originalForegroundWindow != IntPtr.Zero)
      {
        WindowsApiHelper.SetForegroundWindow(originalForegroundWindow);
        System.Threading.Thread.Sleep(100);
      }

      // Send Ctrl+V to paste
      SendKeys.SendWait("^v");
      System.Threading.Thread.Sleep(100);

      // Restore original clipboard after a delay
      Task.Delay(500).ContinueWith(_ =>
      {
        try
        {
          if (!string.IsNullOrEmpty(originalClipboard))
          {
            Clipboard.SetText(originalClipboard);
          }
          else
          {
            Clipboard.Clear();
          }
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine($"Failed to restore clipboard: {ex.Message}");
        }
      });
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"InjectViaClipboard error: {ex.Message}");
      // If clipboard method fails, just set clipboard as fallback
      try
      {
        Clipboard.SetText(text);
      }
      catch (Exception ex2)
      {
        System.Diagnostics.Debug.WriteLine($"Failed to set clipboard as fallback: {ex2.Message}");
      }
    }
  }

  public void RestoreFocus()
  {
    try
    {
      if (originalForegroundWindow != IntPtr.Zero)
      {
        WindowsApiHelper.SetForegroundWindow(originalForegroundWindow);
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to restore focus: {ex.Message}");
    }
  }
}

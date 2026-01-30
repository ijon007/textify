using Microsoft.Win32;
using System.Windows.Forms;

namespace WinFormTest;

public class StartupManager
{
  private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
  private const string AppName = "WinFormTest";

  public static bool IsStartupEnabled()
  {
    try
    {
      using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
      {
        if (key == null)
          return false;
        
        string? value = key.GetValue(AppName) as string;
        return !string.IsNullOrEmpty(value);
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to check startup status: {ex.Message}");
      return false;
    }
  }

  public static void SetStartup(bool enable)
  {
    try
    {
      using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
      {
        if (key == null)
          return;

        if (enable)
        {
          string exePath = Application.ExecutablePath;
          key.SetValue(AppName, exePath);
        }
        else
        {
          key.DeleteValue(AppName, false);
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to set startup: {ex.Message}");
      throw;
    }
  }
}

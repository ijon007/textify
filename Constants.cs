namespace WinFormTest;

public static class StylePreferences
{
  public const string Formal = "formal";
  public const string Casual = "casual";
  public const string VeryCasual = "very_casual";
  public const string Default = Formal;
}

public static class UIColors
{
  // Background colors
  public static readonly Color LightGrayBackground = Color.FromArgb(245, 245, 245);
  public static readonly Color HoverGray = Color.FromArgb(230, 230, 230);
  public static readonly Color White = Color.White;
  
  // Text colors
  public static readonly Color DarkText = Color.FromArgb(45, 45, 48);
  public static readonly Color SecondaryText = Color.FromArgb(100, 100, 100);
  
  // Interactive colors
  public static readonly Color DarkPrimary = Color.FromArgb(45, 45, 48);
  public static readonly Color DarkHover = Color.FromArgb(35, 35, 38);
  public static readonly Color LightButtonBackground = Color.FromArgb(235, 235, 235);
  public static readonly Color LightButtonHover = Color.FromArgb(220, 220, 220);
  
  // Border colors
  public static readonly Color BorderGray = Color.FromArgb(200, 200, 200);
}

public static class UIFonts
{
  public const string FontFamily = "Poppins";
  
  public static Font Title => new Font(FontFamily, 24F, FontStyle.Bold, GraphicsUnit.Point);
  public static Font Heading => new Font(FontFamily, 16F, FontStyle.Bold, GraphicsUnit.Point);
  public static Font Body => new Font(FontFamily, 11F, FontStyle.Regular, GraphicsUnit.Point);
  public static Font Small => new Font(FontFamily, 9F, FontStyle.Regular, GraphicsUnit.Point);
  public static Font Medium => new Font(FontFamily, 10F, FontStyle.Regular, GraphicsUnit.Point);
  public static Font Large => new Font(FontFamily, 12F, FontStyle.Regular, GraphicsUnit.Point);
  public static Font CloseButton => new Font(FontFamily, 18F, FontStyle.Regular, GraphicsUnit.Point);
}

public static class UILayout
{
  // Button sizes
  public static readonly Size IconButtonSize = new Size(30, 30);
  public static readonly Size StandardButtonSize = new Size(100, 35);
  public static readonly Size DialogButtonSize = new Size(80, 28);
  public static readonly Size DialogSaveButtonSize = new Size(100, 28);
  
  // Padding
  public static readonly Padding PagePadding = new Padding(40, 60, 40, 50);
  
  // Spacing
  public const int SpacingSmall = 10;
  public const int SpacingMedium = 15;
  public const int SpacingLarge = 20;
  public const int SpacingXLarge = 30;
  public const int SpacingXXLarge = 40;
  public const int SpacingXXXLarge = 50;
  public const int SpacingHuge = 60;
  
  // Row heights
  public const int FixedRowHeight = 60;
  
  // Border radius
  public const int BorderRadius = 10;
}

public static class DatabaseConstants
{
  public const string ConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=WinFormTest;Integrated Security=True;TrustServerCertificate=True;";
}

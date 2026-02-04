using System.Runtime.InteropServices;

namespace WinFormTest.Services;

public static class UIStylingService
{
    [DllImport("user32.dll")]
    private static extern int ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

    private const int SB_VERT = 1;

    public static void ApplyRoundedCorners(Panel panel, int radius)
    {
        if (panel.Width <= 0 || panel.Height <= 0) return;
        
        System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
        path.AddArc(panel.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
        path.AddArc(panel.Width - radius * 2, panel.Height - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(0, panel.Height - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseAllFigures();
        panel.Region = new System.Drawing.Region(path);
    }

    public static void DrawRoundedBorder(Panel panel, Graphics g, int radius, Color borderColor)
    {
        using (Pen pen = new Pen(borderColor, 1))
        {
            // Draw rounded rectangle border
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            Rectangle rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
            
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();
            
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.DrawPath(pen, path);
        }
    }

    public static void HideScrollbar(Panel panel)
    {
        if (panel.IsHandleCreated)
        {
            ShowScrollBar(panel.Handle, SB_VERT, false);
        }
    }
}

using Microsoft.Data.SqlClient;

namespace WinFormTest;

public partial class Form1 : Form
{
  // SQL Server connection string with Windows Authentication
  private string connectionString = DatabaseConstants.ConnectionString;

  public Form1()
  {
    InitializeComponent();
    SetupCustomControls();
    SetupWindowControls();
    
    // Set custom icon from assets folder
    string iconPath = Path.Combine(Application.StartupPath, "assets", "cp-black.ico");
    if (File.Exists(iconPath))
    {
      this.Icon = new Icon(iconPath);
    }
  }
  
  private Point mouseOffset;
  private bool isDragging = false;
  
  private void SetupWindowControls()
  {
    // Enable window dragging
    this.MouseDown += Form1_MouseDown;
    this.MouseMove += Form1_MouseMove;
    this.MouseUp += Form1_MouseUp;
  }
  
  private void Form1_MouseDown(object? sender, MouseEventArgs e)
  {
    // Only allow dragging from the top area (title bar area)
    if (e.Button == MouseButtons.Left && e.Y <= 40)
    {
      mouseOffset = new Point(e.X, e.Y);
      isDragging = true;
    }
  }
  
  private void Form1_MouseMove(object? sender, MouseEventArgs e)
  {
    if (isDragging)
    {
      Point mousePos = MousePosition;
      mousePos.Offset(-mouseOffset.X, -mouseOffset.Y);
      this.Location = mousePos;
    }
  }
  
  private void Form1_MouseUp(object? sender, MouseEventArgs e)
  {
    isDragging = false;
  }
  
  private void btnClose_Click(object? sender, EventArgs e)
  {
    this.Close();
  }
  
  private void btnClose_MouseEnter(object? sender, EventArgs e)
  {
    btnClose.ForeColor = Color.White;
    btnClose.BackColor = Color.FromArgb(232, 17, 35);
  }
  
  private void btnClose_MouseLeave(object? sender, EventArgs e)
  {
    btnClose.ForeColor = Color.FromArgb(100, 100, 100);
    btnClose.BackColor = Color.Transparent;
  }
  
  private void btnMinimize_Click(object? sender, EventArgs e)
  {
    this.WindowState = FormWindowState.Minimized;
  }
  
  private void btnMinimize_MouseEnter(object? sender, EventArgs e)
  {
    btnMinimize.ForeColor = Color.FromArgb(45, 45, 48);
    btnMinimize.BackColor = Color.FromArgb(240, 240, 240);
  }
  
  private void btnMinimize_MouseLeave(object? sender, EventArgs e)
  {
    btnMinimize.ForeColor = Color.FromArgb(100, 100, 100);
    btnMinimize.BackColor = Color.Transparent;
  }

  private void SetupCustomControls()
  {
    // Center all controls horizontally
    CenterControl(lblTitle);
    CenterControl(lblUsername);
    CenterControl(txtUsername);
    CenterControl(underlineUsername);
    CenterControl(lblPassword);
    CenterControl(txtPassword);
    CenterControl(underlinePassword);
    CenterControl(btnLogin);
  }

  private void CenterControl(Control control)
  {
    control.Left = (this.ClientSize.Width - control.Width) / 2;
  }

  private bool TestConnection()
  {
    try
    {
      SqlConnection connection = new SqlConnection(connectionString);
      connection.Open();
      return true;
    }
    catch (SqlException ex)
    {
      MessageBox.Show($"Connection failed: {ex.Message}", "Database Error", 
        MessageBoxButtons.OK, MessageBoxIcon.Error);
      return false;
    }
  }

  private bool AuthenticateUser(string username, string password)
  {
    try
    {
      SqlConnection connection = new SqlConnection(connectionString);
      connection.Open();
      
      string query = "SELECT COUNT(*) FROM Users WHERE username = @u AND userPass = @p";
      
      SqlCommand command = new SqlCommand(query, connection);
      command.Parameters.AddWithValue("@u", username);
      command.Parameters.AddWithValue("@p", password);
      
      int count = (int)command.ExecuteScalar();
      return count > 0;
    }
    catch (SqlException ex)
    {
      MessageBox.Show($"Authentication error: {ex.Message}", "Database Error", 
        MessageBoxButtons.OK, MessageBoxIcon.Error);
      return false;
    }
  }

  private void btnLogin_Click(object sender, EventArgs e)
  {
    string username = txtUsername.Text;
    string password = txtPassword.Text;

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
      MessageBox.Show("Please enter both username and password.", "Error", 
        MessageBoxButtons.OK, MessageBoxIcon.Warning);
      return;
    }

    // Test connection first
    if (!TestConnection())
    {
      return;
    }

    // Authenticate user against SQL Server
    if (AuthenticateUser(username, password))
    {
      // Hide login form
      this.Hide();
      
      // Open dashboard form
      DashboardForm dashboard = new DashboardForm(username);
      dashboard.Show();
      
      // Close login form when dashboard closes
      dashboard.FormClosed += (s, args) => this.Close();
    }
    else
    {
      MessageBox.Show("Invalid username or password.", "Error", 
        MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
  }

  private void btnLogin_MouseEnter(object sender, EventArgs e)
  {
    btnLogin.BackColor = Color.FromArgb(35, 35, 38);
  }

  private void btnLogin_MouseLeave(object sender, EventArgs e)
  {
    btnLogin.BackColor = Color.FromArgb(45, 45, 48);
  }
}

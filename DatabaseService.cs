using Microsoft.Data.SqlClient;
using System.Linq;

namespace WinFormTest;

public class DatabaseService
{
  private string connectionString;

  public DatabaseService()
  {
    connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=WinFormTest;Integrated Security=True;TrustServerCertificate=True;";
  }

  public void SaveSpeech(string username, string speechText, int? duration = null)
  {
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(speechText))
      return;

    try
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        
        string query = "INSERT INTO Speeches (Username, SpeechText, CreatedAt, Duration) VALUES (@username, @speechText, @createdAt, @duration)";
        
        using (SqlCommand command = new SqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@username", username);
          command.Parameters.AddWithValue("@speechText", speechText);
          command.Parameters.AddWithValue("@createdAt", DateTime.Now);
          
          if (duration.HasValue)
            command.Parameters.AddWithValue("@duration", duration.Value);
          else
            command.Parameters.AddWithValue("@duration", DBNull.Value);
          
          command.ExecuteNonQuery();
        }
      }
    }
    catch (Exception ex)
    {
      // Log error but don't throw - we don't want to break the app if DB fails
      System.Diagnostics.Debug.WriteLine($"Failed to save speech to database: {ex.Message}");
    }
  }

  public List<(int id, string time, string text)> GetSpeeches(string username, int limit = 50)
  {
    var speeches = new List<(int id, string time, string text)>();

    try
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        
        string query = @"
          SELECT TOP (@limit) Id, CreatedAt, SpeechText 
          FROM Speeches 
          WHERE Username = @username 
          ORDER BY CreatedAt DESC";
        
        using (SqlCommand command = new SqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@username", username);
          command.Parameters.AddWithValue("@limit", limit);
          
          using (SqlDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              int id = reader.GetInt32(0);
              DateTime createdAt = reader.GetDateTime(1);
              string text = reader.GetString(2);
              string time = createdAt.ToString("hh:mm tt");
              
              speeches.Add((id, time, text));
            }
          }
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to load speeches from database: {ex.Message}");
    }

    return speeches;
  }

  public int GetConsecutiveWeeksStreak(string username)
  {
    try
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        
        string query = @"
          SELECT DISTINCT CAST(CreatedAt AS DATE) as SpeechDate
          FROM Speeches
          WHERE Username = @username
          ORDER BY SpeechDate DESC";
        
        using (SqlCommand command = new SqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@username", username);
          
          var dates = new List<DateTime>();
          using (SqlDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              dates.Add(reader.GetDateTime(0));
            }
          }
          
          if (dates.Count == 0)
            return 0;
          
          // Group dates by week (Monday as start of week)
          var weeks = dates
            .Select(d => GetWeekStart(d))
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();
          
          if (weeks.Count == 0)
            return 0;
          
          // Count consecutive weeks starting from the most recent
          int streak = 1;
          DateTime currentWeek = weeks[0];
          
          for (int i = 1; i < weeks.Count; i++)
          {
            DateTime previousWeek = weeks[i];
            DateTime expectedPreviousWeek = currentWeek.AddDays(-7);
            
            // Check if weeks are consecutive (within 1 day tolerance for edge cases)
            if (Math.Abs((previousWeek - expectedPreviousWeek).TotalDays) <= 1)
            {
              streak++;
              currentWeek = previousWeek;
            }
            else
            {
              break; // Streak broken
            }
          }
          
          return streak;
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to get consecutive weeks streak: {ex.Message}");
      return 0;
    }
  }

  private DateTime GetWeekStart(DateTime date)
  {
    int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
    return date.AddDays(-1 * diff).Date;
  }

  public int GetTotalWords(string username)
  {
    try
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        
        string query = @"
          SELECT SpeechText
          FROM Speeches
          WHERE Username = @username AND SpeechText IS NOT NULL";
        
        using (SqlCommand command = new SqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@username", username);
          
          int totalWords = 0;
          using (SqlDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              string text = reader.GetString(0);
              if (!string.IsNullOrWhiteSpace(text))
              {
                // Count words by splitting on whitespace
                totalWords += text.Split(new[] { ' ', '\t', '\n', '\r' }, 
                  StringSplitOptions.RemoveEmptyEntries).Length;
              }
            }
          }
          
          return totalWords;
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to get total words: {ex.Message}");
      return 0;
    }
  }

  public int GetAverageWPM(string username)
  {
    try
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        
        // Include records with NULL duration - we'll estimate them
        string query = @"
          SELECT SpeechText, Duration
          FROM Speeches
          WHERE Username = @username 
            AND SpeechText IS NOT NULL";
        
        using (SqlCommand command = new SqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@username", username);
          
          var wpmValues = new List<double>();
          const double averageSpeakingRateWPM = 150.0; // Average speaking rate for estimation
          
          using (SqlDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              string text = reader.GetString(0);
              
              if (string.IsNullOrWhiteSpace(text))
                continue;
              
              // Count words
              int wordCount = text.Split(new[] { ' ', '\t', '\n', '\r' }, 
                StringSplitOptions.RemoveEmptyEntries).Length;
              
              if (wordCount == 0)
                continue;
              
              // Get duration (may be NULL)
              int? durationMs = reader.IsDBNull(1) ? null : reader.GetInt32(1);
              
              double durationMinutes;
              
              if (durationMs.HasValue && durationMs.Value > 0)
              {
                // Use actual recorded duration
                durationMinutes = durationMs.Value / 60000.0;
              }
              else
              {
                // Estimate duration based on average speaking rate (150 WPM)
                // durationMinutes = wordCount / averageSpeakingRateWPMj
                durationMinutes = wordCount / averageSpeakingRateWPM;
              }
              
              // Calculate WPM: (words / duration_in_minutes)
              double wpm = wordCount / durationMinutes;
              wpmValues.Add(wpm);
            }
          }
          
          if (wpmValues.Count == 0)
            return 0;
          
          // Return average WPM rounded to nearest integer
          return (int)Math.Round(wpmValues.Average());
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to get average WPM: {ex.Message}");
      return 0;
    }
  }

  public int AddDictionaryEntry(string username, string word)
  {
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(word))
      throw new ArgumentException("Username and word cannot be empty.");

    try
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        
        // Check for duplicate word for this user
        string checkQuery = "SELECT COUNT(*) FROM Dictionary WHERE Username = @username AND Word = @word";
        using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
        {
          checkCommand.Parameters.AddWithValue("@username", username);
          checkCommand.Parameters.AddWithValue("@word", word.Trim());
          
          int count = (int)checkCommand.ExecuteScalar();
          if (count > 0)
          {
            throw new InvalidOperationException("This word already exists in your dictionary.");
          }
        }
        
        // Insert new entry
        string insertQuery = "INSERT INTO Dictionary (Username, Word, CreatedAt) OUTPUT INSERTED.Id VALUES (@username, @word, @createdAt)";
        
        using (SqlCommand command = new SqlCommand(insertQuery, connection))
        {
          command.Parameters.AddWithValue("@username", username);
          command.Parameters.AddWithValue("@word", word.Trim());
          command.Parameters.AddWithValue("@createdAt", DateTime.Now);
          
          int newId = (int)command.ExecuteScalar();
          return newId;
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to add dictionary entry: {ex.Message}");
      throw;
    }
  }

  public List<(int id, string word, DateTime createdAt)> GetDictionaryEntries(string username)
  {
    var entries = new List<(int id, string word, DateTime createdAt)>();

    try
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        
        string query = @"
          SELECT Id, Word, CreatedAt 
          FROM Dictionary 
          WHERE Username = @username 
          ORDER BY CreatedAt DESC";
        
        using (SqlCommand command = new SqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@username", username);
          
          using (SqlDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              int id = reader.GetInt32(0);
              string word = reader.GetString(1);
              DateTime createdAt = reader.GetDateTime(2);
              
              entries.Add((id, word, createdAt));
            }
          }
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to load dictionary entries: {ex.Message}");
    }

    return entries;
  }

  public void UpdateDictionaryEntry(int id, string username, string word)
  {
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(word))
      throw new ArgumentException("Username and word cannot be empty.");

    try
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        
        // Verify entry belongs to user
        string verifyQuery = "SELECT COUNT(*) FROM Dictionary WHERE Id = @id AND Username = @username";
        using (SqlCommand verifyCommand = new SqlCommand(verifyQuery, connection))
        {
          verifyCommand.Parameters.AddWithValue("@id", id);
          verifyCommand.Parameters.AddWithValue("@username", username);
          
          int count = (int)verifyCommand.ExecuteScalar();
          if (count == 0)
          {
            throw new InvalidOperationException("Dictionary entry not found or does not belong to user.");
          }
        }
        
        // Check for duplicate word (excluding current entry)
        string checkQuery = "SELECT COUNT(*) FROM Dictionary WHERE Username = @username AND Word = @word AND Id != @id";
        using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
        {
          checkCommand.Parameters.AddWithValue("@username", username);
          checkCommand.Parameters.AddWithValue("@word", word.Trim());
          checkCommand.Parameters.AddWithValue("@id", id);
          
          int count = (int)checkCommand.ExecuteScalar();
          if (count > 0)
          {
            throw new InvalidOperationException("This word already exists in your dictionary.");
          }
        }
        
        // Update entry
        string updateQuery = "UPDATE Dictionary SET Word = @word, UpdatedAt = @updatedAt WHERE Id = @id AND Username = @username";
        
        using (SqlCommand command = new SqlCommand(updateQuery, connection))
        {
          command.Parameters.AddWithValue("@id", id);
          command.Parameters.AddWithValue("@username", username);
          command.Parameters.AddWithValue("@word", word.Trim());
          command.Parameters.AddWithValue("@updatedAt", DateTime.Now);
          
          int rowsAffected = command.ExecuteNonQuery();
          if (rowsAffected == 0)
          {
            throw new InvalidOperationException("Failed to update dictionary entry.");
          }
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to update dictionary entry: {ex.Message}");
      throw;
    }
  }

  public void DeleteDictionaryEntry(int id, string username)
  {
    if (string.IsNullOrWhiteSpace(username))
      throw new ArgumentException("Username cannot be empty.");

    try
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        
        // Verify entry belongs to user
        string verifyQuery = "SELECT COUNT(*) FROM Dictionary WHERE Id = @id AND Username = @username";
        using (SqlCommand verifyCommand = new SqlCommand(verifyQuery, connection))
        {
          verifyCommand.Parameters.AddWithValue("@id", id);
          verifyCommand.Parameters.AddWithValue("@username", username);
          
          int count = (int)verifyCommand.ExecuteScalar();
          if (count == 0)
          {
            throw new InvalidOperationException("Dictionary entry not found or does not belong to user.");
          }
        }
        
        // Delete entry
        string deleteQuery = "DELETE FROM Dictionary WHERE Id = @id AND Username = @username";
        
        using (SqlCommand command = new SqlCommand(deleteQuery, connection))
        {
          command.Parameters.AddWithValue("@id", id);
          command.Parameters.AddWithValue("@username", username);
          
          int rowsAffected = command.ExecuteNonQuery();
          if (rowsAffected == 0)
          {
            throw new InvalidOperationException("Failed to delete dictionary entry.");
          }
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to delete dictionary entry: {ex.Message}");
      throw;
    }
  }

  // Snippets Page Methods

  public int AddSnippet(string username, string shortcut, string replacement)
  {
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(shortcut) || string.IsNullOrWhiteSpace(replacement))
      throw new ArgumentException("Username, shortcut, and replacement cannot be empty.");

    try
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        
        // Check for duplicate shortcut for this user
        string checkQuery = "SELECT COUNT(*) FROM Snippets WHERE Username = @username AND Shortcut = @shortcut";
        using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
        {
          checkCommand.Parameters.AddWithValue("@username", username);
          checkCommand.Parameters.AddWithValue("@shortcut", shortcut.Trim());
          
          int count = (int)checkCommand.ExecuteScalar();
          if (count > 0)
          {
            throw new InvalidOperationException("This shortcut already exists in your snippets.");
          }
        }
        
        // Insert new snippet
        string insertQuery = "INSERT INTO Snippets (Username, Shortcut, Replacement, CreatedAt) OUTPUT INSERTED.Id VALUES (@username, @shortcut, @replacement, @createdAt)";
        
        using (SqlCommand command = new SqlCommand(insertQuery, connection))
        {
          command.Parameters.AddWithValue("@username", username);
          command.Parameters.AddWithValue("@shortcut", shortcut.Trim());
          command.Parameters.AddWithValue("@replacement", replacement.Trim());
          command.Parameters.AddWithValue("@createdAt", DateTime.Now);
          
          int newId = (int)command.ExecuteScalar();
          return newId;
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to add snippet: {ex.Message}");
      throw;
    }
  }

  public List<(int id, string shortcut, string replacement, DateTime createdAt)> GetSnippets(string username)
  {
    var snippets = new List<(int id, string shortcut, string replacement, DateTime createdAt)>();

    try
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        
        string query = @"
          SELECT Id, Shortcut, Replacement, CreatedAt 
          FROM Snippets 
          WHERE Username = @username 
          ORDER BY CreatedAt DESC";
        
        using (SqlCommand command = new SqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@username", username);
          
          using (SqlDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              int id = reader.GetInt32(0);
              string shortcut = reader.GetString(1);
              string replacement = reader.GetString(2);
              DateTime createdAt = reader.GetDateTime(3);
              
              snippets.Add((id, shortcut, replacement, createdAt));
            }
          }
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to load snippets: {ex.Message}");
    }

    return snippets;
  }

  public void UpdateSnippet(int id, string username, string shortcut, string replacement)
  {
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(shortcut) || string.IsNullOrWhiteSpace(replacement))
      throw new ArgumentException("Username, shortcut, and replacement cannot be empty.");

    try
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        
        // Verify entry belongs to user
        string verifyQuery = "SELECT COUNT(*) FROM Snippets WHERE Id = @id AND Username = @username";
        using (SqlCommand verifyCommand = new SqlCommand(verifyQuery, connection))
        {
          verifyCommand.Parameters.AddWithValue("@id", id);
          verifyCommand.Parameters.AddWithValue("@username", username);
          
          int count = (int)verifyCommand.ExecuteScalar();
          if (count == 0)
          {
            throw new InvalidOperationException("Snippet not found or does not belong to user.");
          }
        }
        
        // Check for duplicate shortcut (excluding current entry)
        string checkQuery = "SELECT COUNT(*) FROM Snippets WHERE Username = @username AND Shortcut = @shortcut AND Id != @id";
        using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
        {
          checkCommand.Parameters.AddWithValue("@username", username);
          checkCommand.Parameters.AddWithValue("@shortcut", shortcut.Trim());
          checkCommand.Parameters.AddWithValue("@id", id);
          
          int count = (int)checkCommand.ExecuteScalar();
          if (count > 0)
          {
            throw new InvalidOperationException("This shortcut already exists in your snippets.");
          }
        }
        
        // Update snippet
        string updateQuery = "UPDATE Snippets SET Shortcut = @shortcut, Replacement = @replacement, UpdatedAt = @updatedAt WHERE Id = @id AND Username = @username";
        
        using (SqlCommand command = new SqlCommand(updateQuery, connection))
        {
          command.Parameters.AddWithValue("@id", id);
          command.Parameters.AddWithValue("@username", username);
          command.Parameters.AddWithValue("@shortcut", shortcut.Trim());
          command.Parameters.AddWithValue("@replacement", replacement.Trim());
          command.Parameters.AddWithValue("@updatedAt", DateTime.Now);
          
          int rowsAffected = command.ExecuteNonQuery();
          if (rowsAffected == 0)
          {
            throw new InvalidOperationException("Failed to update snippet.");
          }
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to update snippet: {ex.Message}");
      throw;
    }
  }

  public void DeleteSnippet(int id, string username)
  {
    if (string.IsNullOrWhiteSpace(username))
      throw new ArgumentException("Username cannot be empty.");

    try
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        
        // Verify entry belongs to user
        string verifyQuery = "SELECT COUNT(*) FROM Snippets WHERE Id = @id AND Username = @username";
        using (SqlCommand verifyCommand = new SqlCommand(verifyQuery, connection))
        {
          verifyCommand.Parameters.AddWithValue("@id", id);
          verifyCommand.Parameters.AddWithValue("@username", username);
          
          int count = (int)verifyCommand.ExecuteScalar();
          if (count == 0)
          {
            throw new InvalidOperationException("Snippet not found or does not belong to user.");
          }
        }
        
        // Delete snippet
        string deleteQuery = "DELETE FROM Snippets WHERE Id = @id AND Username = @username";
        
        using (SqlCommand command = new SqlCommand(deleteQuery, connection))
        {
          command.Parameters.AddWithValue("@id", id);
          command.Parameters.AddWithValue("@username", username);
          
          int rowsAffected = command.ExecuteNonQuery();
          if (rowsAffected == 0)
          {
            throw new InvalidOperationException("Failed to delete snippet.");
          }
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to delete snippet: {ex.Message}");
      throw;
    }
  }
}

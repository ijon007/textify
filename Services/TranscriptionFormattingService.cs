using System.Text.RegularExpressions;

namespace WinFormTest;

public class TranscriptionFormattingService
{
  private static readonly string[] FillerWords = new[]
  {
    "um", "uh", "like", "you know", "well", "so", "actually", "basically", "literally",
    "kind of", "sort of", "you see", "I mean", "right", "okay", "ok"
  };

  public string FormatTranscription(string text, string stylePreference)
  {
    if (string.IsNullOrWhiteSpace(text))
      return text;

    // Normalize style preference
    string style = (stylePreference ?? "formal").ToLower();
    if (style != "formal" && style != "casual" && style != "very_casual")
      style = "formal";

    // Step 1: Remove filler words
    string formatted = RemoveFillerWords(text);

    // Step 2: Apply punctuation based on style
    formatted = ApplyPunctuation(formatted, style);

    // Step 3: Apply capitalization based on style
    formatted = ApplyCapitalization(formatted, style);

    return formatted.Trim();
  }

  private string RemoveFillerWords(string text)
  {
    if (string.IsNullOrWhiteSpace(text))
      return text;

    string result = text;

    // Remove filler words using word boundaries to avoid false positives
    foreach (string filler in FillerWords)
    {
      // Use word boundaries to match whole words only
      // Handle both standalone and with punctuation
      string pattern = @"\b" + Regex.Escape(filler) + @"\b";
      result = Regex.Replace(result, pattern, "", RegexOptions.IgnoreCase);
    }

    // Clean up multiple spaces
    result = Regex.Replace(result, @"\s+", " ");

    return result.Trim();
  }

  private string ApplyPunctuation(string text, string style)
  {
    if (string.IsNullOrWhiteSpace(text))
      return text;

    string result = text.Trim();

    // Preserve existing question marks
    bool hasQuestion = result.Contains('?');

    // Remove existing punctuation (except question marks) for processing
    if (style == "very_casual")
    {
      // Very casual: remove periods and commas, keep question marks
      result = Regex.Replace(result, @"[.,!;:]", "");
    }
    else
    {
      // For formal and casual, we'll add punctuation intelligently
      // First, normalize existing punctuation
      result = Regex.Replace(result, @"[.!]+", ".");
      result = Regex.Replace(result, @"[,]+", ",");
    }

    // Detect sentence boundaries and add punctuation
    if (style == "formal" || style == "casual")
    {
      // Add periods at sentence ends if missing
      result = Regex.Replace(result, @"([a-z])\s+([A-Z])", match =>
      {
        // Check if there's already punctuation
        string before = match.Groups[1].Value;
        string after = match.Groups[2].Value;
        
        // If no punctuation before, add period
        if (!Regex.IsMatch(before, @"[.!?]$"))
        {
          return before + ". " + after;
        }
        return match.Value;
      });

      // Ensure sentence ends with punctuation
      if (!Regex.IsMatch(result, @"[.!?]$"))
      {
        result = result.TrimEnd() + ".";
      }

      // Add commas for formal style (more commas)
      if (style == "formal")
      {
        // Add comma before conjunctions (and, but, or, so)
        result = Regex.Replace(result, @"\s+(and|but|or|so)\s+", ", $1 ", RegexOptions.IgnoreCase);
        
        // Add comma after introductory phrases (if, when, after, before)
        result = Regex.Replace(result, @"^(if|when|after|before|although|because)\s+", "$1, ", RegexOptions.IgnoreCase);
      }
      else // casual - fewer commas
      {
        // Only add comma before conjunctions in longer sentences
        if (result.Length > 50)
        {
          result = Regex.Replace(result, @"\s+(and|but|or)\s+", ", $1 ", RegexOptions.IgnoreCase);
        }
      }
    }
    else // very_casual
    {
      // Very casual: minimal punctuation, only question marks if it was a question
      if (hasQuestion)
      {
        // Ensure question mark at end if it was a question
        result = result.TrimEnd('.', '!', ',', ';', ':');
        if (!result.EndsWith("?"))
        {
          result = result.TrimEnd() + "?";
        }
      }
      else
      {
        // Remove all ending punctuation
        result = result.TrimEnd('.', '!', ',', ';', ':');
      }
    }

    // Clean up multiple spaces
    result = Regex.Replace(result, @"\s+", " ");

    return result.Trim();
  }

  private string ApplyCapitalization(string text, string style)
  {
    if (string.IsNullOrWhiteSpace(text))
      return text;

    string result = text.Trim();

    if (style == "very_casual")
    {
      // Very casual: all lowercase
      result = result.ToLower();
    }
    else
    {
      // Formal and Casual: proper capitalization
      // Capitalize first letter of the text
      if (result.Length > 0)
      {
        result = char.ToUpper(result[0]) + result.Substring(1);
      }

      // Capitalize after sentence endings (periods, question marks, exclamation marks)
      result = Regex.Replace(result, @"([.!?])\s+([a-z])", match =>
      {
        string charStr = match.Groups[2].Value;
        return match.Groups[1].Value + " " + (charStr.Length > 0 ? char.ToUpper(charStr[0]).ToString() : charStr);
      });

      // Capitalize after newlines
      result = Regex.Replace(result, @"\n\s*([a-z])", match =>
      {
        string charStr = match.Groups[1].Value;
        return "\n" + (charStr.Length > 0 ? char.ToUpper(charStr[0]).ToString() : charStr);
      });
    }

    return result;
  }
}

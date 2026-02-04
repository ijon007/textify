using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace WinFormTest;

public class TranscriptionCorrectionService
{
  private readonly DatabaseService databaseService;
  private readonly Dictionary<string, List<string>> dictionaryCache;
  private readonly Dictionary<string, List<(string shortcut, string replacement)>> snippetsCache;
  private readonly object cacheLock = new object();
  private const double SimilarityThreshold = 0.60; // 60% similarity required for correction (lowered for better matching)
  private const double PhoneticSimilarityThreshold = 0.40; // Lower threshold for phonetic matches
  private const int MinWordLengthForStrictMatching = 4; // Words shorter than this use more lenient matching

  public TranscriptionCorrectionService(DatabaseService databaseService)
  {
    this.databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    this.dictionaryCache = new Dictionary<string, List<string>>();
    this.snippetsCache = new Dictionary<string, List<(string shortcut, string replacement)>>();
  }

  public string CorrectTranscription(string text, string username)
  {
    if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(username))
      return text;

    string correctedText = text;

    // Step 1: Apply snippet replacements first (before dictionary corrections)
    correctedText = ApplySnippetReplacements(correctedText, username);

    // Step 2: Apply dictionary corrections
    correctedText = ApplyDictionaryCorrections(correctedText, username);

    return correctedText;
  }

  private string ApplySnippetReplacements(string text, string username)
  {
    if (string.IsNullOrWhiteSpace(text))
      return text;

    var snippets = GetSnippetsForUser(username);
    if (snippets.Count == 0)
      return text;

    // Sort by shortcut length (descending) to match longer shortcuts first
    var sortedSnippets = snippets.OrderByDescending(s => s.shortcut.Length).ToList();

    string result = text;

    foreach (var snippet in sortedSnippets)
    {
      if (string.IsNullOrWhiteSpace(snippet.shortcut))
        continue;

      // Use word boundaries to avoid partial matches
      // Escape special regex characters in the shortcut
      string escapedShortcut = Regex.Escape(snippet.shortcut);
      
      // Match whole words/phrases (case-insensitive)
      string pattern = @"\b" + escapedShortcut + @"\b";
      result = Regex.Replace(result, pattern, snippet.replacement, RegexOptions.IgnoreCase);
    }

    return result;
  }

  private string ApplyDictionaryCorrections(string text, string username)
  {
    if (string.IsNullOrWhiteSpace(text))
      return text;

    var dictionaryWords = GetDictionaryForUser(username);
    if (dictionaryWords.Count == 0)
      return text;

    // Split text into words while preserving whitespace
    var words = Regex.Split(text, @"(\s+)");
    var result = new System.Text.StringBuilder();
    var processedIndices = new HashSet<int>();

    // First pass: try to match multi-word combinations (check adjacent words together)
    // This handles cases like "shad cn" -> "ShadCN"
    for (int i = 0; i < words.Length; i++)
    {
      if (processedIndices.Contains(i))
        continue;

      string currentWord = words[i];
      
      // Add whitespace tokens directly to result
      if (string.IsNullOrWhiteSpace(currentWord))
      {
        result.Append(currentWord);
        processedIndices.Add(i);
        continue;
      }

      bool foundMatch = false;

      // Try matching 1-word, 2-word, and 3-word combinations
      for (int wordCount = 1; wordCount <= 3 && !foundMatch; wordCount++)
      {
        // Build combination by looking ahead
        var wordTokens = new List<string>();
        var indices = new List<int>();
        int tokenIndex = i;
        int wordsFound = 0;
        
        while (wordsFound < wordCount && tokenIndex < words.Length)
        {
          if (!string.IsNullOrWhiteSpace(words[tokenIndex]))
          {
            wordTokens.Add(words[tokenIndex]);
            indices.Add(tokenIndex);
            wordsFound++;
          }
          tokenIndex++;
        }

        if (wordTokens.Count == 0)
          continue;

        string combinedWord = string.Join("", wordTokens);
        string combinedNormalized = Regex.Replace(combinedWord, @"[^\w]", "").Replace(" ", "").ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(combinedNormalized))
          continue;

        // Check for exact match
        string? exactMatch = dictionaryWords.FirstOrDefault(dw =>
        {
          string dictWordNormalized = dw.Replace(" ", "").ToLowerInvariant();
          return string.Equals(dictWordNormalized, combinedNormalized, StringComparison.OrdinalIgnoreCase);
        });

        if (exactMatch != null)
        {
          // Replace the combined words with dictionary word
          string replacement = ReplaceWordPreservingFormat(combinedWord, combinedNormalized, exactMatch);
          result.Append(replacement);
          
          foreach (var idx in indices)
            processedIndices.Add(idx);
          
          // Add whitespace tokens between words to result and mark as processed
          if (indices.Count > 1)
          {
            for (int j = indices[0] + 1; j < indices[indices.Count - 1]; j++)
            {
              if (string.IsNullOrWhiteSpace(words[j]))
              {
                result.Append(words[j]);
                processedIndices.Add(j);
              }
            }
          }
          
          foundMatch = true;
          break;
        }
      }

      // If no multi-word match found, process as single word
      if (!foundMatch)
      {
        // Remove punctuation and spaces for matching (but preserve punctuation in output)
        string wordWithoutPunctuation = Regex.Replace(currentWord, @"[^\w]", "");
        string wordNormalized = wordWithoutPunctuation.Replace(" ", "").ToLowerInvariant();
        
        if (string.IsNullOrWhiteSpace(wordNormalized))
        {
          result.Append(currentWord);
          processedIndices.Add(i);
          continue;
        }

        // Check if word exactly matches a dictionary word (case-insensitive, ignoring spaces)
        string? exactDictWord = dictionaryWords.FirstOrDefault(dw =>
        {
          string dictWordNormalized = dw.Replace(" ", "").ToLowerInvariant();
          return string.Equals(dictWordNormalized, wordNormalized, StringComparison.OrdinalIgnoreCase);
        });

        if (exactDictWord != null)
        {
          // Replace with dictionary word, preserving punctuation and capitalization style
          string corrected = ReplaceWordPreservingFormat(currentWord, wordWithoutPunctuation, exactDictWord);
          result.Append(corrected);
          processedIndices.Add(i);
          continue;
        }

        // Find best fuzzy match (use normalized version without spaces)
        string? bestMatch = FindBestFuzzyMatch(wordNormalized, dictionaryWords);
        
        if (bestMatch != null)
        {
          // Replace the word, preserving punctuation and capitalization
          string corrected = ReplaceWordPreservingFormat(currentWord, wordWithoutPunctuation, bestMatch);
          result.Append(corrected);
        }
        else
        {
          // No good match found, keep original
          result.Append(currentWord);
        }
        
        processedIndices.Add(i);
      }
    }

    return result.ToString();
  }

  private string? FindBestFuzzyMatch(string word, List<string> dictionaryWords)
  {
    if (string.IsNullOrWhiteSpace(word) || dictionaryWords.Count == 0)
      return null;

    string? bestMatch = null;
    double bestSimilarity = 0;
    double threshold = SimilarityThreshold;

    // Use more lenient threshold for shorter words
    if (word.Length < MinWordLengthForStrictMatching)
    {
      threshold = 0.50; // 50% for short words
    }

    foreach (string dictWord in dictionaryWords)
    {
      if (string.IsNullOrWhiteSpace(dictWord))
        continue;

      // Check if dictionary word contains the transcribed word (or vice versa) for better matching
      // This helps with cases like "shadcn" vs "ShadCN" or "shad cn" vs "ShadCN"
      string wordLower = word.ToLowerInvariant();
      string dictWordLower = dictWord.ToLowerInvariant();
      
      // Remove spaces from both for comparison (handles "shad cn" -> "shadcn")
      string wordNoSpaces = wordLower.Replace(" ", "");
      string dictWordNoSpaces = dictWordLower.Replace(" ", "");

      // Check for substring matches (one contains the other)
      bool containsMatch = wordNoSpaces.Contains(dictWordNoSpaces) || dictWordNoSpaces.Contains(wordNoSpaces);
      
      if (containsMatch && dictWordNoSpaces.Length >= Math.Max(3, wordNoSpaces.Length * 0.7))
      {
        // If one contains the other and lengths are reasonably close, use the dictionary word
        double containmentSimilarity = Math.Min(wordNoSpaces.Length, dictWordNoSpaces.Length) / 
                                      (double)Math.Max(wordNoSpaces.Length, dictWordNoSpaces.Length);
        
        if (containmentSimilarity > bestSimilarity)
        {
          bestSimilarity = containmentSimilarity;
          bestMatch = dictWord;
        }
      }

      // Try regular fuzzy matching
      double similarity = CalculateSimilarity(wordNoSpaces, dictWordNoSpaces);
      
      // Use phonetic similarity for cases where transcribed word is much longer (phonetic expansion)
      // e.g., "schatzien" (9 chars) vs "ShadCN" (6 chars) - phonetic transcription issue
      // Also check when lengths differ significantly (either direction)
      bool isPhoneticExpansion = wordNoSpaces.Length > dictWordNoSpaces.Length * 1.3;
      bool isPhoneticContraction = dictWordNoSpaces.Length > wordNoSpaces.Length * 1.3;
      bool hasSignificantLengthDiff = Math.Abs(wordNoSpaces.Length - dictWordNoSpaces.Length) >= 3;
      
      if (isPhoneticExpansion || isPhoneticContraction || hasSignificantLengthDiff)
      {
        // For phonetic variations, use lower threshold and check if key sounds match
        double phoneticSimilarity = CalculatePhoneticSimilarity(wordNoSpaces, dictWordNoSpaces);
        
        if (phoneticSimilarity >= PhoneticSimilarityThreshold && phoneticSimilarity > bestSimilarity)
        {
          bestSimilarity = phoneticSimilarity;
          bestMatch = dictWord;
        }
      }
      
      if (similarity >= threshold && similarity > bestSimilarity)
      {
        bestSimilarity = similarity;
        bestMatch = dictWord;
      }
    }

    return bestMatch;
  }

  private double CalculatePhoneticSimilarity(string s1, string s2)
  {
    if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
      return 0.0;

    // Normalize to lowercase
    s1 = s1.ToLowerInvariant();
    s2 = s2.ToLowerInvariant();

    // Extract key phonetic sounds - look for common letter patterns
    // This is a simplified phonetic matching approach
    
    // Check if they share significant consonant clusters or vowel patterns
    string s1Consonants = ExtractConsonants(s1);
    string s2Consonants = ExtractConsonants(s2);
    
    // Check consonant similarity (most important for phonetic matching)
    double consonantSimilarity = CalculateSimilarity(s1Consonants, s2Consonants);
    
    // Check if first few characters match (common in phonetic transcriptions)
    // For "schatzien" vs "shadcn", "sch" vs "sh" - check first 2-3 chars
    int matchingStart = 0;
    int checkLength = Math.Min(Math.Min(3, s1.Length), s2.Length);
    for (int i = 0; i < checkLength; i++)
    {
      if (i < s1.Length && i < s2.Length && s1[i] == s2[i])
        matchingStart++;
      else
        break;
    }
    
    // Also check if they start with similar sounds (e.g., "sch" vs "sh")
    bool similarStartSound = false;
    if (s1.Length >= 2 && s2.Length >= 2)
    {
      string s1Start = s1.Substring(0, Math.Min(2, s1.Length));
      string s2Start = s2.Substring(0, Math.Min(2, s2.Length));
      
      // Check for common phonetic variations
      if ((s1Start.StartsWith("sh") && s2Start.StartsWith("sch")) ||
          (s1Start.StartsWith("sch") && s2Start.StartsWith("sh")) ||
          (s1Start.StartsWith("ch") && s2Start.StartsWith("sh")) ||
          (s1Start.StartsWith("sh") && s2Start.StartsWith("ch")))
      {
        similarStartSound = true;
        matchingStart = Math.Max(matchingStart, 1); // Boost start match
      }
    }
    
    double startMatchRatio = checkLength > 0 ? (double)matchingStart / checkLength : 0;
    
    // Check if they share similar ending sounds
    int matchingEnd = 0;
    int endCheckLength = Math.Min(Math.Min(2, s1.Length), s2.Length);
    for (int i = 1; i <= endCheckLength; i++)
    {
      if (i <= s1.Length && i <= s2.Length && 
          s1[s1.Length - i] == s2[s2.Length - i])
        matchingEnd++;
      else
        break;
    }
    
    double endMatchRatio = endCheckLength > 0 ? (double)matchingEnd / endCheckLength : 0;
    
    // Check for key consonant clusters in the middle (e.g., "sh" + "d" in both)
    int commonConsonantClusters = CountCommonConsonantClusters(s1Consonants, s2Consonants);
    double clusterScore = Math.Min(1.0, commonConsonantClusters / 2.0); // Normalize to 0-1
    
    // Weighted combination: consonants are most important, then start matches, then clusters
    double phoneticScore = (consonantSimilarity * 0.5) + 
                          (startMatchRatio * 0.25) + 
                          (endMatchRatio * 0.1) + 
                          (clusterScore * 0.15);
    
    // Boost score if start sounds are similar
    if (similarStartSound)
      phoneticScore = Math.Min(1.0, phoneticScore + 0.15);
    
    return phoneticScore;
  }

  private int CountCommonConsonantClusters(string s1, string s2)
  {
    int count = 0;
    
    // Check for common 2-character consonant clusters
    for (int i = 0; i < s1.Length - 1; i++)
    {
      string cluster = s1.Substring(i, 2);
      if (s2.Contains(cluster))
        count++;
    }
    
    return count;
  }

  private string ExtractConsonants(string word)
  {
    if (string.IsNullOrEmpty(word))
      return "";

    var consonants = new System.Text.StringBuilder();
    word = word.ToLowerInvariant();
    
    // Normalize common phonetic variations first
    word = word.Replace("sch", "sh"); // "sch" -> "sh" sound
    word = word.Replace("ck", "k");
    word = word.Replace("ph", "f");
    word = word.Replace("qu", "kw");
    
    int i = 0;
    while (i < word.Length)
    {
      char c = word[i];
      
      if (char.IsLetter(c) && !IsVowel(c))
      {
        consonants.Append(c);
      }
      
      i++;
    }
    
    return consonants.ToString();
  }

  private bool IsVowel(char c)
  {
    c = char.ToLowerInvariant(c);
    return c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u' || c == 'y';
  }

  private double CalculateSimilarity(string s1, string s2)
  {
    if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
      return 1.0;

    if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
      return 0.0;

    // Normalize to lowercase for comparison
    s1 = s1.ToLowerInvariant();
    s2 = s2.ToLowerInvariant();

    // If exact match (case-insensitive), return 1.0
    if (s1 == s2)
      return 1.0;

    // Calculate Levenshtein distance
    int distance = LevenshteinDistance(s1, s2);
    int maxLength = Math.Max(s1.Length, s2.Length);

    if (maxLength == 0)
      return 1.0;

    // Similarity = 1 - (distance / maxLength)
    return 1.0 - ((double)distance / maxLength);
  }

  private int LevenshteinDistance(string s1, string s2)
  {
    if (string.IsNullOrEmpty(s1))
      return string.IsNullOrEmpty(s2) ? 0 : s2.Length;

    if (string.IsNullOrEmpty(s2))
      return s1.Length;

    int n = s1.Length;
    int m = s2.Length;
    int[,] d = new int[n + 1, m + 1];

    // Initialize first row and column
    for (int i = 0; i <= n; i++)
      d[i, 0] = i;

    for (int j = 0; j <= m; j++)
      d[0, j] = j;

    // Fill the matrix
    for (int i = 1; i <= n; i++)
    {
      for (int j = 1; j <= m; j++)
      {
        int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
        d[i, j] = Math.Min(
          Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
          d[i - 1, j - 1] + cost
        );
      }
    }

    return d[n, m];
  }

  private string ReplaceWordPreservingFormat(string originalWord, string wordToReplace, string replacement)
  {
    // Preserve leading/trailing punctuation
    string leadingPunctuation = "";
    string trailingPunctuation = "";
    
    int startIndex = 0;
    int endIndex = originalWord.Length;

    // Extract leading punctuation
    while (startIndex < originalWord.Length && !char.IsLetterOrDigit(originalWord[startIndex]))
    {
      leadingPunctuation += originalWord[startIndex];
      startIndex++;
    }

    // Extract trailing punctuation
    while (endIndex > startIndex && !char.IsLetterOrDigit(originalWord[endIndex - 1]))
    {
      trailingPunctuation = originalWord[endIndex - 1] + trailingPunctuation;
      endIndex--;
    }

    // Preserve capitalization
    string formattedReplacement = replacement;
    if (originalWord.Length > 0 && startIndex < originalWord.Length && endIndex > startIndex)
    {
      // If original word starts with uppercase, capitalize replacement
      if (char.IsLetter(originalWord[startIndex]) && char.IsUpper(originalWord[startIndex]))
      {
        if (replacement.Length > 0)
        {
          formattedReplacement = char.ToUpper(replacement[0]) + 
            (replacement.Length > 1 ? replacement.Substring(1) : "");
        }
      }
      // If original word is all uppercase, make replacement uppercase
      else if (startIndex < endIndex)
      {
        string wordPart = originalWord.Substring(startIndex, endIndex - startIndex);
        if (wordPart.All(c => !char.IsLetter(c) || char.IsUpper(c)))
        {
          formattedReplacement = replacement.ToUpperInvariant();
        }
      }
    }

    return leadingPunctuation + formattedReplacement + trailingPunctuation;
  }

  private List<string> GetDictionaryForUser(string username)
  {
    lock (cacheLock)
    {
      if (dictionaryCache.TryGetValue(username, out var cached))
        return cached;

      var entries = databaseService.GetDictionaryEntries(username);
      var words = entries.Select(e => e.word).ToList();
      
      dictionaryCache[username] = words;
      return words;
    }
  }

  private List<(string shortcut, string replacement)> GetSnippetsForUser(string username)
  {
    lock (cacheLock)
    {
      if (snippetsCache.TryGetValue(username, out var cached))
        return cached;

      var entries = databaseService.GetSnippets(username);
      var snippets = entries.Select(e => (e.shortcut, e.replacement)).ToList();
      
      snippetsCache[username] = snippets;
      return snippets;
    }
  }

  public void InvalidateCache(string username)
  {
    lock (cacheLock)
    {
      dictionaryCache.Remove(username);
      snippetsCache.Remove(username);
    }
  }

  public void InvalidateDictionaryCache(string username)
  {
    lock (cacheLock)
    {
      dictionaryCache.Remove(username);
    }
  }

  public void InvalidateSnippetsCache(string username)
  {
    lock (cacheLock)
    {
      snippetsCache.Remove(username);
    }
  }
}

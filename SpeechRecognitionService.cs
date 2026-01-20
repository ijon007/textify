using Vosk;
using NAudio.Wave;
using System.Text.Json;

namespace WinFormTest;

public class SpeechRecognitionService : IDisposable
{
  private Model? voskModel;
  private VoskRecognizer? recognizer;
  private WaveInEvent? waveIn;
  private bool isListening = false;
  private readonly object lockObject = new object();

  public event EventHandler<string>? SpeechRecognized;
  public event EventHandler<string>? SpeechPartialResult; // For real-time display only
  public event EventHandler<string>? SpeechRejected;

  public bool IsListening => isListening;

  public SpeechRecognitionService()
  {
    // Model will be loaded asynchronously via InitializeAsync
  }

  public async Task InitializeAsync()
  {
    await Task.Run(() =>
    {
      try
      {
        InitializeVoskModel();
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to initialize speech recognition: {ex.Message}", ex);
      }
    });
  }

  public bool IsModelLoaded => voskModel != null && recognizer != null;

  private void InitializeVoskModel()
  {
    // Model path: Application.StartupPath/models/vosk-model-en-us-0.22/
    string modelPath = Path.Combine(Application.StartupPath, "models", "vosk-model-en-us-0.22");
    
    if (!Directory.Exists(modelPath))
    {
      throw new Exception($"Vosk model not found at: {modelPath}\n" +
                         "Please download the English small model from https://alphacephei.com/vosk/models\n" +
                         "Extract it to: " + modelPath);
    }

    voskModel = new Model(modelPath);
    recognizer = new VoskRecognizer(voskModel, 16000.0f);
    recognizer.SetMaxAlternatives(0);
    recognizer.SetWords(true);
  }

  public void StartListening()
  {
    if (voskModel == null || recognizer == null || isListening)
      return;

    try
    {
      lock (lockObject)
      {
        // Initialize NAudio for microphone capture
        waveIn = new WaveInEvent();
        waveIn.WaveFormat = new WaveFormat(16000, 1); // 16kHz, Mono, 16-bit
        waveIn.DataAvailable += WaveIn_DataAvailable;
        waveIn.RecordingStopped += WaveIn_RecordingStopped;

        // Reset recognizer for new session
        recognizer.Reset();

        waveIn.StartRecording();
        isListening = true;
      }
    }
    catch (Exception ex)
    {
      throw new Exception($"Failed to start listening: {ex.Message}", ex);
    }
  }

  public void StopListening()
  {
    if (waveIn == null || !isListening)
      return;

    try
    {
      lock (lockObject)
      {
        waveIn.StopRecording();
        isListening = false;

        // Get final result
        if (recognizer != null)
        {
          string finalResult = recognizer.FinalResult();
          ProcessFinalResult(finalResult);
        }
      }
    }
    catch
    {
      // Ignore errors when stopping
    }
  }

  private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
  {
    if (recognizer == null || e.BytesRecorded == 0)
      return;

    try
    {
      lock (lockObject)
      {
        // Process audio chunk through Vosk
        if (recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
        {
          // Finalized result (sentence completed)
          string result = recognizer.Result();
          ProcessFinalResult(result);
        }
        else
        {
          // Partial result (words as they are spoken)
          string partialResult = recognizer.PartialResult();
          ProcessPartialResult(partialResult);
        }
      }
    }
    catch (Exception ex)
    {
      // Log error but don't stop recognition
      System.Diagnostics.Debug.WriteLine($"Error processing audio: {ex.Message}");
    }
  }

  private void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
  {
    // Cleanup handled in StopListening
  }

  private void ProcessPartialResult(string jsonResult)
  {
    if (string.IsNullOrWhiteSpace(jsonResult))
      return;

    try
    {
      using (JsonDocument doc = JsonDocument.Parse(jsonResult))
      {
        if (doc.RootElement.TryGetProperty("partial", out JsonElement partialElement))
        {
          string partialText = partialElement.GetString() ?? string.Empty;
          if (!string.IsNullOrWhiteSpace(partialText))
          {
            // Emit partial result for real-time display only (not for accumulation)
            SpeechPartialResult?.Invoke(this, partialText);
          }
        }
      }
    }
    catch
    {
      // Ignore JSON parsing errors
    }
  }

  private void ProcessFinalResult(string jsonResult)
  {
    if (string.IsNullOrWhiteSpace(jsonResult))
      return;

    try
    {
      using (JsonDocument doc = JsonDocument.Parse(jsonResult))
      {
        if (doc.RootElement.TryGetProperty("text", out JsonElement textElement))
        {
          string recognizedText = textElement.GetString() ?? string.Empty;
          if (!string.IsNullOrWhiteSpace(recognizedText))
          {
            // Emit final result (DashboardForm will handle accumulation)
            SpeechRecognized?.Invoke(this, recognizedText);
          }
        }
        else
        {
          // No text recognized
          SpeechRejected?.Invoke(this, "Recognition rejected");
        }
      }
    }
    catch
    {
      // Ignore JSON parsing errors
      SpeechRejected?.Invoke(this, "Recognition rejected");
    }
  }

  public void Dispose()
  {
    StopListening();
    
    lock (lockObject)
    {
      waveIn?.Dispose();
      recognizer?.Dispose();
      voskModel?.Dispose();
    }
  }
}

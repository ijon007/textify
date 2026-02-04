using Whisper.net;
using Whisper.net.Ggml;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;

namespace WinFormTest;

public class SpeechRecognitionService : IDisposable
{
  private WhisperFactory? whisperFactory;
  private object? processor;
  private WaveInEvent? waveIn;
  private MemoryStream? audioBuffer;
  private bool isListening = false;
  private readonly object lockObject = new object();
  private int? selectedDeviceNumber = null;
  private CancellationTokenSource? cancellationTokenSource;
  private Task? processingTask;
  private DateTime lastProcessTime = DateTime.MinValue;
  private const int ProcessIntervalMs = 2000; // Process every 2 seconds
  private const int BufferSizeBytes = 32000; // ~1 second at 16kHz mono 16-bit

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
    try
    {
      await InitializeWhisperModel();
    }
    catch (Exception ex)
    {
      throw new Exception($"Failed to initialize speech recognition: {ex.Message}", ex);
    }
  }

  public bool IsModelLoaded => whisperFactory != null && processor != null;

  public static List<(int deviceNumber, string deviceName)> GetAvailableMicrophones()
  {
    var devices = new List<(int, string)>();
    try
    {
      for (int i = 0; i < WaveInEvent.DeviceCount; i++)
      {
        var capabilities = WaveInEvent.GetCapabilities(i);
        devices.Add((i, capabilities.ProductName));
      }
    }
    catch
    {
      // Ignore errors
    }
    return devices;
  }

  public void SetMicrophoneDevice(int? deviceNumber)
  {
    selectedDeviceNumber = deviceNumber;
  }

  private async Task InitializeWhisperModel()
  {
    var startupPath = Application.StartupPath ?? AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
    string modelPath = Path.Combine(startupPath, "models", "ggml-base.bin");
    string modelsDirectory = Path.Combine(startupPath, "models");

    // Ensure models directory exists
    if (!Directory.Exists(modelsDirectory))
    {
      Directory.CreateDirectory(modelsDirectory);
    }

    // Download model if it doesn't exist
    if (!File.Exists(modelPath))
    {
      try
      {
        using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Base);
        using var fileWriter = File.OpenWrite(modelPath);
        await modelStream.CopyToAsync(fileWriter);
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to download Whisper model: {ex.Message}\n" +
                          "Please ensure you have an internet connection for the first run.\n" +
                          "Alternatively, download ggml-base.bin from https://huggingface.co/ggerganov/whisper.cpp\n" +
                          "and place it in: " + modelPath, ex);
      }
    }

    if (!File.Exists(modelPath))
    {
      throw new Exception($"Whisper model not found at: {modelPath}\n" +
                        "Please download ggml-base.bin from https://huggingface.co/ggerganov/whisper.cpp\n" +
                        "and place it in: " + modelPath);
    }

    whisperFactory = WhisperFactory.FromPath(modelPath);
    var builtProcessor = whisperFactory.CreateBuilder()
      .WithLanguage("auto")
      .Build();
    processor = builtProcessor;
  }

  public void StartListening()
  {
    if (whisperFactory == null || processor == null || isListening)
      return;

    try
    {
      lock (lockObject)
      {
        // Initialize audio buffer
        audioBuffer = new MemoryStream();

        // Initialize NAudio for microphone capture
        waveIn = new WaveInEvent();
        
        // Set device number if specified
        if (selectedDeviceNumber.HasValue && selectedDeviceNumber.Value >= 0 && selectedDeviceNumber.Value < WaveInEvent.DeviceCount)
        {
          waveIn.DeviceNumber = selectedDeviceNumber.Value;
        }
        
        waveIn.WaveFormat = new WaveFormat(16000, 1); // 16kHz, Mono, 16-bit
        waveIn.DataAvailable += WaveIn_DataAvailable;
        waveIn.RecordingStopped += WaveIn_RecordingStopped;

        // Initialize cancellation token for processing task
        cancellationTokenSource = new CancellationTokenSource();
        lastProcessTime = DateTime.Now;

        // Start background processing task
        processingTask = Task.Run(async () => await ProcessAudioBufferAsync(cancellationTokenSource.Token));

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

        // Cancel processing task
        cancellationTokenSource?.Cancel();

        // Process final buffer
        if (audioBuffer != null && audioBuffer.Length > 0 && processor != null)
        {
          try
          {
            var finalBuffer = new MemoryStream();
            audioBuffer.Position = 0;
            audioBuffer.CopyTo(finalBuffer);
            finalBuffer.Position = 0;
            _ = Task.Run(async () => await ProcessAudioBufferAsync(finalBuffer, true));
          }
          catch
          {
            // Ignore errors
          }
        }

        // Wait for processing task to complete (with timeout)
        if (processingTask != null)
        {
          try
          {
            processingTask.Wait(TimeSpan.FromSeconds(2));
          }
          catch
          {
            // Ignore timeout or cancellation exceptions
          }
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
    if (e.BytesRecorded == 0 || audioBuffer == null)
      return;

    try
    {
      lock (lockObject)
      {
        // Write audio chunk to buffer
        audioBuffer.Write(e.Buffer, 0, e.BytesRecorded);
      }
    }
    catch
    {
      // Ignore errors
    }
  }

  private void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
  {
    // Cleanup handled in StopListening
  }

  private async Task ProcessAudioBufferAsync(CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested && isListening)
    {
      try
      {
        await Task.Delay(500, cancellationToken); // Check every 500ms

        lock (lockObject)
        {
          if (audioBuffer == null || processor == null)
            continue;

          var elapsed = DateTime.Now - lastProcessTime;
          
          // Process if enough time has passed or buffer is large enough
          if (elapsed.TotalMilliseconds >= ProcessIntervalMs || audioBuffer.Length >= BufferSizeBytes)
          {
            if (audioBuffer.Length > 0)
            {
              // Create a copy of the buffer for processing
              var bufferCopy = new MemoryStream();
              audioBuffer.Position = 0;
              audioBuffer.CopyTo(bufferCopy);
              bufferCopy.Position = 0;

              // Reset the main buffer
              audioBuffer.SetLength(0);
              audioBuffer.Position = 0;

              // Process the copy asynchronously
              _ = Task.Run(async () => await ProcessAudioBufferAsync(bufferCopy, false));
            }

            lastProcessTime = DateTime.Now;
          }
        }
      }
      catch (OperationCanceledException)
      {
        break;
      }
      catch
      {
        // Ignore errors
      }
    }
  }

  private async Task ProcessAudioBufferAsync(MemoryStream audioStream, bool isFinal)
  {
    if (processor == null || audioStream.Length == 0)
    {
      audioStream.Dispose();
      return;
    }

    // Whisper needs at least 1 second of audio (16000 samples/sec * 2 bytes/sample = 32000 bytes)
    // Require at least 1 second for better accuracy
    if (audioStream.Length < 32000)
    {
      audioStream.Dispose();
      return;
    }

    try
    {
      audioStream.Position = 0;

      // Convert to WAV format (Whisper.net expects WAV format)
      var wavStream = ConvertToWav(audioStream);
      audioStream.Dispose(); // Dispose PCM stream after conversion

      // Ensure stream is at the beginning
      wavStream.Position = 0;

      // Process audio through Whisper.net
      var segments = new List<string>();

      try
      {
        // Save to temp file - more reliable than MemoryStream
        string tempFile = Path.Combine(Path.GetTempPath(), $"whisper_{Guid.NewGuid()}.wav");
        try
        {
          using (var fileStream = File.Create(tempFile))
          {
            wavStream.Position = 0;
            wavStream.CopyTo(fileStream);
            fileStream.Flush();
          }
          
          // Process using file stream
          using (var fileStream = File.OpenRead(tempFile))
          {
            // Call ProcessAsync using dynamic
            dynamic dynamicProcessor = processor;
            var processAsyncResult = dynamicProcessor.ProcessAsync(fileStream);
            
            // Get the async enumerator
            System.Collections.Generic.IAsyncEnumerable<object>? asyncEnumerableInterface = processAsyncResult as System.Collections.Generic.IAsyncEnumerable<object>;
            if (asyncEnumerableInterface == null)
            {
              // Try to cast dynamically
              try
              {
                if (processAsyncResult != null)
                {
                  asyncEnumerableInterface = (System.Collections.Generic.IAsyncEnumerable<object>)processAsyncResult;
                }
              }
              catch
              {
                return;
              }
            }
            
            if (asyncEnumerableInterface == null)
            {
              return;
            }
            
            var enumerator = asyncEnumerableInterface.GetAsyncEnumerator();
            try
            {
              while (await enumerator.MoveNextAsync())
              {
                var current = enumerator.Current;
                if (current != null)
                {
                  // Use reflection to get Text property
                  var textProperty = current.GetType().GetProperty("Text");
                  if (textProperty != null)
                  {
                    var text = textProperty.GetValue(current)?.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                      segments.Add(text);
                    }
                  }
                  else
                  {
                    // Try dynamic access as fallback
                    try
                    {
                      dynamic result = current;
                      string? text = result.Text?.ToString();
                      if (!string.IsNullOrWhiteSpace(text))
                      {
                        segments.Add(text);
                      }
                    }
                    catch { }
                  }
                }
              }
            }
            finally
            {
              await enumerator.DisposeAsync();
            }
          }
        }
        finally
        {
          // Clean up temp file
          try
          {
            if (File.Exists(tempFile))
              File.Delete(tempFile);
          }
          catch { }
        }
      }
      catch
      {
        // Ignore errors
      }
      finally
      {
        wavStream.Dispose();
      }

      if (segments.Count > 0)
      {
        string combinedText = string.Join(" ", segments);
        
        if (isFinal)
        {
          // Emit final result
          if (!string.IsNullOrWhiteSpace(combinedText))
          {
            SpeechRecognized?.Invoke(this, combinedText);
          }
          else
          {
            SpeechRejected?.Invoke(this, "Recognition rejected");
          }
        }
        else
        {
          // Emit partial result for real-time display
          SpeechPartialResult?.Invoke(this, combinedText);
        }
      }
      else
      {
        if (isFinal)
        {
          SpeechRejected?.Invoke(this, "Recognition rejected");
        }
      }
    }
    catch
    {
      if (isFinal)
      {
        SpeechRejected?.Invoke(this, "Recognition rejected");
      }
    }
    finally
    {
      // Ensure stream is disposed
      if (audioStream != null && audioStream.CanRead)
      {
        audioStream.Dispose();
      }
    }
  }

  private MemoryStream ConvertToWav(MemoryStream pcmStream)
  {
    // Create WAV format manually - Whisper.net expects proper WAV headers
    var wavStream = new MemoryStream();
    
    // WAV header for 16kHz, Mono, 16-bit PCM
    int sampleRate = 16000;
    int channels = 1;
    int bitsPerSample = 16;
    int dataSize = (int)pcmStream.Length;
    int fileSize = 36 + dataSize;

    // RIFF header (little-endian)
    wavStream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, 4);
    wavStream.Write(BitConverter.GetBytes(fileSize), 0, 4);
    wavStream.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, 4);

    // fmt chunk
    wavStream.Write(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, 4);
    wavStream.Write(BitConverter.GetBytes(16), 0, 4); // fmt chunk size
    wavStream.Write(BitConverter.GetBytes((short)1), 0, 2); // audio format (PCM)
    wavStream.Write(BitConverter.GetBytes((short)channels), 0, 2);
    wavStream.Write(BitConverter.GetBytes(sampleRate), 0, 4);
    
    int byteRate = sampleRate * channels * bitsPerSample / 8;
    wavStream.Write(BitConverter.GetBytes(byteRate), 0, 4);
    
    short blockAlign = (short)(channels * bitsPerSample / 8);
    wavStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);
    wavStream.Write(BitConverter.GetBytes((short)bitsPerSample), 0, 2);

    // data chunk
    wavStream.Write(System.Text.Encoding.ASCII.GetBytes("data"), 0, 4);
    wavStream.Write(BitConverter.GetBytes(dataSize), 0, 4);

    // Copy PCM data
    pcmStream.Position = 0;
    pcmStream.CopyTo(wavStream);

    wavStream.Position = 0;
    return wavStream;
  }


  public void Dispose()
  {
    StopListening();
    
    lock (lockObject)
    {
      cancellationTokenSource?.Dispose();
      waveIn?.Dispose();
      audioBuffer?.Dispose();
      (processor as IDisposable)?.Dispose();
      whisperFactory?.Dispose();
    }
  }
}

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CloseDriver.Core.Services;

public class DataLogger : IDisposable
{
	private readonly string _filePath;
	private StreamWriter? _writer;
	private bool _isLogging;

	public bool IsLogging => _isLogging;

	public DataLogger(string filePath)
	{
		_filePath = filePath;
	}

	public void Start()
	{
		if (_isLogging)
			return;

		var directory = Path.GetDirectoryName(_filePath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		_writer = new StreamWriter(_filePath, append: true, Encoding.UTF8)
		{
			AutoFlush = true
		};
		_isLogging = true;
	}

	public void Stop()
	{
		_isLogging = false;
		if (_writer != null)
		{
			_writer.Flush();
			_writer.Dispose();
			_writer = null;
		}
	}

	public async Task LogDataAsync(byte[] data, bool isTransmit = false)
	{
		if (!_isLogging || _writer == null || data == null || data.Length == 0)
			return;

		var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
		var direction = isTransmit ? "TX" : "RX";
		var hexData = BitConverter.ToString(data).Replace("-", " ");

		var logLine = $"[{timestamp}] {direction}: {hexData}";
		await _writer.WriteLineAsync(logLine);
	}

	public void Dispose()
	{
		Stop();
	}
}

using System;
using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CloseDriver.Core.Services;

public class DataLogger : IDisposable
{
	private readonly StreamWriter _writer;
	private readonly Channel<string> _channel;
	private readonly Task _consumerTask;

	public DataLogger(string filePath)
	{
		var directory = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			Directory.CreateDirectory(directory);

		_writer = new StreamWriter(filePath, append: false, Encoding.UTF8)
		{
			AutoFlush = true
		};

		_channel = Channel.CreateBounded<string>(new BoundedChannelOptions(256)
		{
			FullMode = BoundedChannelFullMode.DropOldest,
			SingleReader = true,
			SingleWriter = false
		});

		_consumerTask = Task.Run(ConsumeAsync);
	}

	public Task LogDataAsync(byte[] data, bool isTransmit = false)
	{
		if (data == null || data.Length == 0)
			return Task.CompletedTask;

		var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
		var direction = isTransmit ? "TX" : "RX";
		var hexData = BitConverter.ToString(data).Replace("-", " ");

		_channel.Writer.TryWrite($"[{timestamp}] {direction}: {hexData}");
		return Task.CompletedTask;
	}

	private async Task ConsumeAsync()
	{
		await foreach (var line in _channel.Reader.ReadAllAsync())
		{
			await _writer.WriteLineAsync(line);
		}
	}

	public void Dispose()
	{
		_channel.Writer.TryComplete();
		_consumerTask.GetAwaiter().GetResult();
		_writer.Flush();
		_writer.Dispose();
	}
}

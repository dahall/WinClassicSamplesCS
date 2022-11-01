namespace Websocket;

internal class Transport
{
	private readonly MemoryStream stream = new(1024);

	public HRESULT ReadData(uint dataLength, out uint outputDataLength, IntPtr data)
	{
		if (data == IntPtr.Zero)
		{
			outputDataLength = 0;
			return HRESULT.E_FAIL;
		}
		lock (stream)
		{
			outputDataLength = (uint)stream.Read(data.AsSpan<byte>((int)dataLength));
		}
		return HRESULT.S_OK;
	}

	public HRESULT WriteData(IntPtr data, uint dataLength)
	{
		if (data != IntPtr.Zero)
		{
			// Add the entry to the list.
			lock (stream)
			{
				stream.Write(data.AsReadOnlySpan<byte>((int)dataLength));
			}
		}
		return HRESULT.S_OK;
	}
}
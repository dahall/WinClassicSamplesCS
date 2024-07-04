using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.AMSI;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Macros;

CStreamScanner scanner = new();
HRESULT hr = 0;
if (args.Length < 1)
{
	// Scan a single memory stream.
	Console.Write("Creating memory stream object\n");

	CAmsiMemoryStream stream = new();
	hr = scanner.ScanStream(stream);
}
else
{
	// Scan the files passed on the command line.
	for (int i = 0; i < args.Length; i++)
	{
		string fileName = args[i];

		Console.Write("Creating stream object with file name: {0}\n", fileName);
		CAmsiFileStream stream = new(fileName);
		hr = scanner.ScanStream(stream);
	}
}
Console.Write("Leaving with hr = 0x{0:x}\n", (int)hr);

return 0;

internal class CAmsiFileStream : CAmsiStreamBase, IAmsiStream
{
	private readonly SafeHFILE m_fileHandle;

	public CAmsiFileStream(string fileName)
	{
		HRESULT hr = HRESULT.S_OK;

		SetContentName(fileName);

		m_fileHandle = CreateFile(fileName,
			FileAccess.GENERIC_READ, // dwDesiredAccess
			0, // dwShareMode
			default, // lpSecurityAttributes
			CreationOption.OPEN_EXISTING, // dwCreationDisposition
			FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, // dwFlagsAndAttributes
			default); // hTemplateFile

		if (m_fileHandle.IsInvalid)
		{
			hr = Win32Error.GetLastError().ToHRESULT();
			Console.Write("Unable to open file {0}, hr = 0x{1:x}\n", fileName, (int)hr);
			hr.ThrowIfFailed();
		}

		if (!GetFileSizeEx(m_fileHandle, out var fileSize))
		{
			hr = Win32Error.GetLastError().ToHRESULT();
			Console.Write("GetFileSizeEx failed with 0x{0:X}\n", (int)hr);
			hr.ThrowIfFailed();
		}
		m_contentSize = (ulong)fileSize;

		hr.ThrowIfFailed();
	}

	// IAmsiStream
	HRESULT IAmsiStream.GetAttribute([In] AMSI_ATTRIBUTE attribute, uint bufferSize, [Out] IntPtr buffer, out uint actualSize) => BaseGetAttribute(attribute, bufferSize, buffer, out actualSize);

	HRESULT IAmsiStream.Read(ulong position, uint size, [Out] IntPtr buffer, out uint readSize)
	{
		Console.Write("Read() called with: position = {0}, size = {1}\n", position, size);

		NativeOverlapped o = new() { OffsetLow = unchecked((int)((long)position).LowPart()), OffsetHigh = ((long)position).HighPart() };

		if (!ReadFile(m_fileHandle, buffer, size, out readSize, ref o))
		{
			HRESULT hr = Win32Error.GetLastError().ToHRESULT();
			Console.Write("ReadFile failed with 0x{0:X}\n", (int)hr);
			return hr;
		}

		return HRESULT.S_OK;
	}

	[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool ReadFile(HFILE hFile, IntPtr lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, ref NativeOverlapped lpOverlapped);
}

internal class CAmsiMemoryStream : CAmsiStreamBase, IAmsiStream
{
	public static readonly byte[] SampleStream = StringHelper.GetBytes("Hello, world");

	public CAmsiMemoryStream()
	{
		m_contentSize = (ulong)SampleStream.Length;
		SetContentName("Sample content.txt");
	}

	// IAmsiStream
	HRESULT IAmsiStream.GetAttribute([In] AMSI_ATTRIBUTE attribute, uint bufferSize, IntPtr buffer, out uint actualSize)
	{
		HRESULT hr = BaseGetAttribute(attribute, bufferSize, buffer, out actualSize);
		if (hr == HRESULT.E_NOTIMPL)
		{
			switch (attribute)
			{
				case AMSI_ATTRIBUTE.AMSI_ATTRIBUTE_CONTENT_ADDRESS:
					hr = CopyAttribute(SampleStream, bufferSize, buffer, out actualSize);
					break;
			}
		}
		return hr;
	}

	HRESULT IAmsiStream.Read(ulong position, uint size, [Out] IntPtr buffer, out uint readSize)
	{
		Console.Write("Read() called with: position = {0}, size = {1}\n", position, size);

		readSize = 0;
		if (position >= m_contentSize)
		{
			Console.Write("Reading beyond end of stream\n");
			return HRESULT.HRESULT_FROM_WIN32(Win32Error.ERROR_HANDLE_EOF);
		}

		if (size > m_contentSize - position)
		{
			size = (uint)(m_contentSize - position);
		}

		try
		{
			readSize = (uint)buffer.Write(SampleStream.Skip((int)position), 0, size);
			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			readSize = 0;
			return ex.HResult;
		}
	}
}

internal class CAmsiStreamBase
{
	protected string? m_contentName = null;
	protected ulong m_contentSize = 0;
	private const string AppName = "Contoso Script Engine v3.4.9999.0";

	protected HRESULT BaseGetAttribute([In] AMSI_ATTRIBUTE attribute, uint bufferSize, [Out] IntPtr buffer, out uint actualSize)
	// Return Values: HRESULT.S_OK: SUCCESS HRESULT.E_NOTIMPL: attribute not supported HRESULT.E_NOT_SUFFICIENT_BUFFER: need a larger buffer,
	// required size ref in retSize HRESULT.E_INVALIDARG: bad arguments HRESULT.E_NOT_VALID_STATE: object not initialized
	{
		Console.Write("GetAttribute() called with: attribute = {0}, bufferSize = {1}\n", attribute, bufferSize);

		actualSize = 0;
		if (buffer != IntPtr.Zero && bufferSize > 0)
		{
			return HRESULT.E_INVALIDARG;
		}

		switch (attribute)
		{
			case AMSI_ATTRIBUTE.AMSI_ATTRIBUTE_CONTENT_SIZE:
				return CopyAttribute(m_contentSize, bufferSize, buffer, out actualSize);

			case AMSI_ATTRIBUTE.AMSI_ATTRIBUTE_CONTENT_NAME:
				return CopyAttribute(m_contentName, bufferSize, buffer, out actualSize);

			case AMSI_ATTRIBUTE.AMSI_ATTRIBUTE_APP_NAME:
				return CopyAttribute(AppName, bufferSize, buffer, out actualSize);

			case AMSI_ATTRIBUTE.AMSI_ATTRIBUTE_SESSION:
				HAMSISESSION session = default; // no session for file stream
				return CopyAttribute((IntPtr)session, bufferSize, buffer, out actualSize);
		}

		return HRESULT.E_NOTIMPL; // unsupport attribute
	}

	protected virtual HRESULT CopyAttribute([In] object? resultData, uint bufferSize, IntPtr buffer, out uint actualSize)
	{
		try
		{
			actualSize = (uint)buffer.Write(resultData, 0, bufferSize);
			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			actualSize = 0;
			return ex.HResult;
		}
	}

	protected HRESULT SetContentName(string name)
	{
		m_contentName = name;
		return HRESULT.S_OK;
	}
}

internal class CStreamScanner
{
	private readonly IAntimalware m_antimalware = new();

	public HRESULT ScanStream([In] IAmsiStream stream)
	{
		Console.Write("Calling antimalware.Scan() ...\n");
		HRESULT hr = m_antimalware.Scan(stream, out var r, out var provider);
		if (hr.Failed)
		{
			return hr;
		}

		Console.Write("Scan result is {0}. IsMalware: {1}\n", r, AmsiResultIsMalware(r));

		if (provider is not null)
		{
			hr = provider.DisplayName(out var name);
			if (hr.Succeeded)
			{
				Console.Write("Provider display name: {0}\n", name);
			}
			else
			{
				Console.Write("DisplayName failed with 0x{0:x}", (int)hr);
			}
		}

		return HRESULT.S_OK;
	}
}
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using static Vanara.PInvoke.MSCTF;

namespace tsfapp;

internal class CTSFPersistentPropertyLoader : ITfPersistentPropertyLoaderACP
{
	private readonly TF_PERSISTENT_PROPERTY_HEADER_ACP m_hdr;
	private readonly byte[] m_pb;

	public CTSFPersistentPropertyLoader(TF_PERSISTENT_PROPERTY_HEADER_ACP tF_PERSISTENT_PROPERTY_HEADER_ACP, IStream pStream)
	{
		m_hdr = tF_PERSISTENT_PROPERTY_HEADER_ACP;
		m_pb = new byte[m_hdr.cb];
		pStream.Read(m_pb, m_pb.Length, default);
	}

	HRESULT ITfPersistentPropertyLoaderACP.LoadProperty(in TF_PERSISTENT_PROPERTY_HEADER_ACP pHdr, out IStream? ppStream)
	{
		ppStream = default;

		//create a stream to return
		HRESULT hr = Ole32.CreateStreamOnHGlobal(default, true, out IStream pStream);
		if (hr.Succeeded)
		{
			try
			{
				//write the property data into the stream
				pStream.Write(m_pb, m_pb.Length, default);
				pStream.Seek(0, (int)Ole32.STREAM_SEEK.STREAM_SEEK_SET, default);
				ppStream = pStream;
			}
			catch (Exception e)
			{
				hr = e.HResult;
			}
		}

		return hr;
	}
}
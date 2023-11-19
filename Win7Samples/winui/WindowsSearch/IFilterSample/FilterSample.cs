using System.Xml;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.SearchApi;

namespace IFilterSample;

[ComVisible(true), Guid("30A86815-6F17-45FF-8A4B-C89FC2F7D2B8")]
public class CFilterSample : CFilterBase
{
	private EMITSTATE m_iEmitState = EMITSTATE.EMITSTATE_FLAGSTATUS;
	private XmlReader? m_pReader = null;

	public CFilterSample()
	{
	}

	// some props we want to emit don't come from the doc. We use this as our state
	private enum EMITSTATE { EMITSTATE_FLAGSTATUS = 0, EMITSTATE_ISREAD };

	public override void Dispose()
	{
		m_pReader?.Dispose();
		base.Dispose();
	}

	public override HRESULT GetNextChunkValue(ref CChunkValue chunkValue)
	{
		chunkValue.Clear();

		// read through the stream
		XmlNodeType nodeType;
		while (m_pReader is not null && m_pReader.Read())
		{
			nodeType = m_pReader.NodeType;
			if (XmlNodeType.Element == nodeType)
			{
				string? pszValue;
				var pszName = m_pReader.Name;

				// if it is the title
				if (pszName == "mytitle")
				{
					pszValue = GetElementText();
					if (pszValue != null)
					{
						// return this value chunk
						chunkValue.SetTextValue(PROPERTYKEY.System.Title, pszValue);
						return HRESULT.S_OK;
					}
				}
				// if it is the my keywords
				else if (pszName == "mykeywords")
				{
					pszValue = GetElementText();
					if (pszValue != null)
					{
						// return this value chunk
						chunkValue.SetTextValue(PROPERTYKEY.System.Keywords, pszValue);
						return HRESULT.S_OK;
					}
				}
				// if it is the my author
				else if (pszName == "Author")
				{
					pszValue = GetElementText();
					if (pszValue != null)
					{
						// return this value chunk
						chunkValue.SetTextValue(PROPERTYKEY.System.ItemAuthors, pszValue);
						return HRESULT.S_OK;
					}
				}
				// if it is the my body
				else if (pszName == "lastmodified")
				{
					pszValue = GetElementText();
					if (pszValue != null)
					try
					{
						var filetime = DateTime.Parse(pszValue).ToFileTimeStruct();
						chunkValue.SetValue(PROPERTYKEY.System.DateModified, filetime);
						return HRESULT.S_OK;
					}
					catch { }
				}
				// if it is the my body
				else if (pszName == "body")
				{
					pszValue = GetElementText();
					if (pszValue != null)
					{
						// return this value chunk
						chunkValue.SetTextValue(PROPERTYKEY.System.Search.Contents, pszValue, CHUNKSTATE.CHUNK_TEXT);
						return HRESULT.S_OK;
					}
				}
				// If we found an element of interest then the value is stored in chunkValue and we return. Otherwise continue until
				// another element is found.
			}
		}

		// Not all data from the XML document has been read but additional props can be added For this we use the m_iEmitState to
		// iterate through them, each call will go to the next one
		switch (m_iEmitState)
		{
			case EMITSTATE.EMITSTATE_FLAGSTATUS:
				// we are using this just to illustrate a numeric property
				chunkValue.SetValue(PROPERTYKEY.System.FlagStatus, 1);
				m_iEmitState++;
				return HRESULT.S_OK;

			case EMITSTATE.EMITSTATE_ISREAD:
				// we are using this just to illustrate a bool property
				chunkValue.SetValue(PROPERTYKEY.System.IsRead, true);
				m_iEmitState++;
				return HRESULT.S_OK;
		}

		// if we get to here we are done with this document
		return FILTER_E_END_OF_CHUNKS;
	}

	public override HRESULT OnInit()
	{
		m_pReader = XmlReader.Create(new Vanara.InteropServices.ComStream(m_pStream!));
		return HRESULT.S_OK;
	}

	// helper method for XmlLite
	private string? GetElementText()
	{
		while (m_pReader is not null && m_pReader.Read())
		{
			if (m_pReader.NodeType == XmlNodeType.Text)
			{
				return m_pReader.Value;
			}
		}
		return null;
	}
}
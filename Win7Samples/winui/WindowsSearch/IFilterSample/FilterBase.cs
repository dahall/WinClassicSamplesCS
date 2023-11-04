using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PropSys;
using static Vanara.PInvoke.SearchApi;

namespace IFilterSample
{
	public class CChunkValue
	{
		private STAT_CHUNK m_chunk;
		private bool m_fIsValid = false;
		private PROPVARIANT m_propVariant = new();
		private string m_pszValue = null;

		public CChunkValue()
		{
			Clear();
		}

		~CChunkValue()
		{
			Clear();
		}

		// clear the ChunkValue
		public void Clear()
		{
			m_fIsValid = false;
			m_chunk = default;
			m_propVariant.Clear();
			m_pszValue = null;
		}

		// copy the chunk
		public void CopyChunk(out STAT_CHUNK pStatChunk) => pStatChunk = m_chunk;

		// get the type of chunk
		public CHUNKSTATE GetChunkType() => m_chunk.flags;

		// get the string value
		public string GetString() => m_pszValue;

		// get the value as an allocated PROPVARIANT
		public PROPVARIANT GetValue() { m_propVariant.Clone(out var pv); return pv; }

		// Is this propvalue valid
		public bool IsValid() => m_fIsValid;

		// set the property by key to a unicode string
		public void SetTextValue(in PROPERTYKEY pkey, string pszValue, CHUNKSTATE chunkType = CHUNKSTATE.CHUNK_VALUE, LCID locale = default,
			uint cwcLenSource = 0, uint cwcStartSource = 0, CHUNK_BREAKTYPE chunkBreakType = CHUNK_BREAKTYPE.CHUNK_NO_BREAK)
		{
			if (pszValue == null) throw new ArgumentNullException(nameof(pszValue));

			SetChunk(pkey, chunkType, locale, cwcLenSource, cwcStartSource, chunkBreakType);
			m_fIsValid = true;
			if (chunkType == CHUNKSTATE.CHUNK_VALUE)
			{
				m_propVariant = new PROPVARIANT(pszValue, VarEnum.VT_LPWSTR);
			}
			else
			{
				m_pszValue = pszValue;
			}
		}

		public void SetValue(in PROPERTYKEY pkey, object val, CHUNKSTATE chunkType = CHUNKSTATE.CHUNK_VALUE, LCID locale = default,
			uint cwcLenSource = 0, uint cwcStartSource = 0, CHUNK_BREAKTYPE chunkBreakType = CHUNK_BREAKTYPE.CHUNK_NO_BREAK)
		{
			if (val is string s)
				SetTextValue(pkey, s, chunkType, locale, cwcLenSource, cwcStartSource, chunkBreakType);
			else
			{
				SetChunk(pkey, chunkType, locale, cwcLenSource, cwcStartSource, chunkBreakType);
				m_propVariant = new PROPVARIANT(val);
				m_fIsValid = true;
			}
		}

		// set the locale for this chunk
		protected void SetChunk(in PROPERTYKEY pkey, CHUNKSTATE chunkType = CHUNKSTATE.CHUNK_VALUE, LCID locale = default, uint cwcLenSource = 0, uint cwcStartSource = 0, CHUNK_BREAKTYPE chunkBreakType = CHUNK_BREAKTYPE.CHUNK_NO_BREAK)
		{
			Clear();

			// initialize the chunk
			m_chunk.attribute.psProperty = new PROPSPEC(pkey.pid);
			m_chunk.attribute.guidPropSet = pkey.fmtid;
			m_chunk.flags = chunkType;
			m_chunk.locale = (uint)locale;
			m_chunk.cwcLenSource = cwcLenSource;
			m_chunk.cwcStartSource = cwcStartSource;
			m_chunk.breakType = chunkBreakType;
		}
	}

	// base class that implements IFilter and initialization interfaces for a filter To use:
	// - Create a COM Object derived from CFilterBase
	// - Then add IFilter, IInitializeWithStream to your COM map
	// - Implement the methods OnInit and GetNextChunkValue
	public abstract class CFilterBase : IFilter, IInitializeWithStream, IDisposable
	{
		protected const uint FILTER_E_END_OF_CHUNKS = 0x80041700;
		protected const uint FILTER_E_NO_MORE_TEXT = 0x80041701;
		protected const uint FILTER_E_NO_MORE_VALUES = 0x80041702;
		protected const uint FILTER_E_NO_TEXT = 0x80041705;
		protected const uint FILTER_S_LAST_TEXT = 0x00041709;

		protected IStream m_pStream;         // stream of this document
		private CChunkValue m_currentChunk;  // the current chunk value
		private uint m_dwChunkId;            // Current chunk id
		private uint m_iText;                // index into ChunkValue

		public CFilterBase()
		{
		}

		public virtual void Dispose()
		{
			if (m_pStream != null)
				Marshal.ReleaseComObject(m_pStream);
		}

		// When GetNextChunkValue() is called you should fill in the ChunkValue by calling SetXXXValue() with the property.
		// example:  chunkValue.SetTextValue(PKYE_ItemName,L"blah de blah"); return FILTER_E_END_OF_CHUNKS when there are no more chunks
		public abstract HRESULT GetNextChunkValue(ref CChunkValue chunkValue);

		// OnInit() is called after the IStream is valid
		public abstract HRESULT OnInit();

		HRESULT IFilter.BindRegion(FILTERREGION origPos, in Guid riid, out object ppunk)
		{
			ppunk = null;
			return HRESULT.E_NOTIMPL;
		}

		HRESULT IFilter.GetChunk(out STAT_CHUNK pStat)
		{
			// Get the chunk from the derived class. A return of S_FALSE indicates the chunk should be skipped and we should try to get the
			// next chunk.

			int cIterations = 0;
			HRESULT hr = HRESULT.S_FALSE;
			pStat = default;

			while (HRESULT.S_FALSE == hr && (~cIterations & 0x100) != 0)  // Limit to 256 iterations for safety
			{
				pStat.idChunk = m_dwChunkId;
				hr = GetNextChunkValue(ref m_currentChunk);
				++cIterations;
			}

			if (hr == HRESULT.S_OK)
			{
				if (m_currentChunk.IsValid())
				{
					// copy out the STAT_CHUNK
					m_currentChunk.CopyChunk(out pStat);

					// and set the id to be the sequential chunk
					pStat.idChunk = ++m_dwChunkId;
				}
				else
				{
					hr = HRESULT.E_INVALIDARG;
				}
			}

			return hr;
		}

		HRESULT IFilter.GetText(ref uint pcwcBuffer, StringBuilder awcBuffer)
		{
			HRESULT hr = HRESULT.S_OK;

			if (pcwcBuffer == 0)
			{
				return HRESULT.E_INVALIDARG;
			}

			if (!m_currentChunk.IsValid())
			{
				return FILTER_E_NO_MORE_TEXT;
			}

			if (m_currentChunk.GetChunkType() != CHUNKSTATE.CHUNK_TEXT)
			{
				return FILTER_E_NO_TEXT;
			}

			uint cchTotal = (uint)m_currentChunk.GetString().Length;
			uint cchLeft = cchTotal - m_iText;
			uint cchToCopy = Math.Min(pcwcBuffer - 1, cchLeft);

			if (cchToCopy > 0)
			{
				string psz = m_currentChunk.GetString() + m_iText;

				// copy the chars
				awcBuffer.Append(psz);

				// set how much data is copied
				pcwcBuffer = (uint)awcBuffer.Length;

				// remember we copied it
				m_iText += cchToCopy;
				cchLeft -= cchToCopy;

				if (cchLeft == 0)
				{
					hr = FILTER_S_LAST_TEXT;
				}
			}
			else
			{
				hr = FILTER_E_NO_MORE_TEXT;
			}

			return hr;
		}

		HRESULT IFilter.GetValue(ref PROPVARIANT ppPropValue)
		{
			HRESULT hr = HRESULT.S_OK;

			// if this is not a value chunk they shouldn't be calling this
			if (m_currentChunk.GetChunkType() != CHUNKSTATE.CHUNK_VALUE)
			{
				return FILTER_E_NO_MORE_VALUES;
			}

			if (ppPropValue == null)
			{
				return HRESULT.E_INVALIDARG;
			}

			if (m_currentChunk.IsValid())
			{
				// return the value of this chunk as a PROPVARIANT ( they own freeing it properly )
				ppPropValue = m_currentChunk.GetValue();
				m_currentChunk.Clear();
			}
			else
			{
				// we have already return the value for this chunk, go away
				hr = FILTER_E_NO_MORE_VALUES;
			}

			return hr;
		}

		HRESULT IFilter.Init(IFILTER_INIT grfFlags, uint cAttributes, FULLPROPSPEC[] aAttributes, out IFILTER_FLAGS pFlags)
		{
			pFlags = default;
			// Common initialization
			m_dwChunkId = 0;
			m_iText = 0;
			m_currentChunk.Clear();
			return HRESULT.S_OK;
		}

		void IInitializeWithStream.Initialize(IStream pstream, STGM grfMode)
		{
			if (m_pStream != null)
			{
				Marshal.ReleaseComObject(m_pStream);
			}
			m_pStream = pstream;
			OnInit();  // derived class inits now
		}

		// Service functions for derived classes
		protected uint GetChunkId() => m_dwChunkId;
	}
}
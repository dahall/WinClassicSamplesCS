using System.Collections.Generic;
using System.IO;
using Vanara.Extensions;
using static Vanara.PInvoke.ProjectedFSLib;

namespace ProjectedFileSystem
{
	// This holds the information the RegFS provider will return for a single directory entry.
	//
	// Note that RegFS does not supply any timestamps.  This is because the only timestamp the registry
	// maintains is the last write time for a key.  It does not maintain creation, last-access, or change
	// times for keys, and it does not maintain any timestamps at all for values.  When RegFS calls
	// PrjFillDirEntryBuffer(), ProjFS sees that the timestamp values are 0 and uses the current time
	// instead.
	internal struct DirEntry
	{
		public string FileName;
		public bool IsDirectory;
		public long FileSize;
	}

	// RegFS uses a DirInfo object to hold directory entries.  When RegFS receives enumeration callbacks
	// it populates the DirInfo with a vector of DirEntry structs, one for each key and value in the
	// registry key being enumerated.
	//
	// Refer to RegfsProvider::StartDirEnum, RegfsProvider::GetDirEnum, and RegfsProvider::EndDirEnum
	// to see how this class is used.
	internal class DirInfo
	{
		// The index of the item in _entries that CurrentBasicInfo() and CurrentFileName() will return.
		private int _currIndex;

		// The list of entries in the directory this DirInfo represents.
		private List<DirEntry> _entries = new List<DirEntry>();

		// Stores the name of the directory this DirInfo represents.
		private string _filePathName;

		// Constructs a new empty DirInfo, initializing it with the name of the directory it represents.
		public DirInfo(string FilePathName) { _filePathName = FilePathName; }

		// Returns a PRJ_FILE_BASIC_INFO populated with the information for the current item.
		public PRJ_FILE_BASIC_INFO CurrentBasicInfo => new PRJ_FILE_BASIC_INFO { IsDirectory = _entries[_currIndex].IsDirectory, FileSize = _entries[_currIndex].FileSize };

		// Returns the file name for the current item.
		public string CurrentFileName => _entries[_currIndex].FileName;

		// Returns true if CurrentBasicInfo() and CurrentFileName() will return valid values.
		public bool CurrentIsValid => _currIndex < _entries.Count;

		// Returns true if the DirInfo object has been populated with entries.
		public bool EntriesFilled { get; private set; }

		// Adds a DirEntry to the list using the given name. The entry gets marked as a directory.
		public void FillDirEntry(string DirName) => _entries.Add(new DirEntry { IsDirectory = true, FileName = DirName });

		// Adds a DirEntry to the list, using the given name and size. The entry gets marked as a file.
		public void FillFileEntry(string FileName) => _entries.Add(new DirEntry { FileName = FileName, FileSize = 128 });

		// Moves the internal index to the next DirEntry item. Returns false if there are no more items.
		public bool MoveNext() => ++_currIndex < _entries.Count;

		// Deletes all the DirEntry items in the DirInfo object.
		public void Reset()
		{
			_currIndex = 0;
			EntriesFilled = false;
			_entries.Clear();
		}

		// Sorts the entries in the DirInfo object and marks the object as being fully populated.
		public void SortEntriesAndMarkFilled()
		{
			EntriesFilled = true;
			_entries.Sort((a, b) => string.CompareOrdinal(a.FileName, b.FileName));
		}
	}
}
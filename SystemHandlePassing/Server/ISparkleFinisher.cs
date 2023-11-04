using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace SparkleFinisherLib;

[ComVisible(true), Guid("1284C788-6978-43D5-9A02-414901A2EC75"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ISparkleFinisher
{
	[PreserveSig]
	HRESULT AddSparkleFinishToFile([In] HFILE decorateThisFile, [In] HEVENT whenThisEventFires, out HEVENT willNotifyWhenDone);
}

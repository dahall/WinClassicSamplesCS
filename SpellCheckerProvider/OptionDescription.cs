using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke.InteropServices;
using static Vanara.PInvoke.SpellCheck;

class COptionDescription(string optionId) : IOptionDescription
{
	public string Id => optionId;

	public string Heading { get { OptionsStore.GetOptionHeading(Id, out var s).ThrowIfFailed(); return s; } }

	public string Description { get { OptionsStore.GetOptionDescription(Id, out var s).ThrowIfFailed(); return s; } }

	public IEnumString Labels { get { OptionsStore.GetOptionLabels(Id, out var s).ThrowIfFailed(); return new ComEnumString(s); } }
}
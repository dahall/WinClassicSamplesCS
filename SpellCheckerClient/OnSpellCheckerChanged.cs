using Vanara.PInvoke;
using static Vanara.PInvoke.SpellCheck;

namespace SpellCheckerClient;

internal class OnSpellCheckerChanged : ISpellCheckerChangedEventHandler
{
	private uint eventCookie;

	public static void StartListeningToChangeEvents([In] ISpellChecker spellChecker, out OnSpellCheckerChanged eventListener)
	{
		OnSpellCheckerChanged onChanged = new();
		spellChecker.add_SpellCheckerChanged(onChanged, out onChanged.eventCookie);
		eventListener = onChanged;
	}

	public static void StopListeningToChangeEvents([In] ISpellChecker spellChecker, in OnSpellCheckerChanged eventHandler)
	{
		spellChecker.remove_SpellCheckerChanged(eventHandler.eventCookie);
	}

	HRESULT ISpellCheckerChangedEventHandler.Invoke([In] ISpellChecker? _)
	{
		Console.Write("Spell checker changed.\n");
		return HRESULT.S_OK;
	}
}
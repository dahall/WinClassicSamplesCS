namespace imapi2sample;

static class ConsoleUtil
{
	public static void DeleteCurrentLine()
	{
		Console.Write(new string('\b', 80));
		return;
	}

	public static void OverwriteCurrentLine()
	{
		Console.Write(new string(' ', 80));
		return;
	}

	public static void UpdatePercentageDisplay(int Numerator, int Denominator)
	{
		if (Numerator > Denominator)
			return;

		// NOTE: Overflow possibility exists for large numerators.
		var percent = (Numerator * 100) / Denominator;

		// each block is 2%
		// ----=----1----=----2----=----3----=----4----=----5----=----6----=----7----=----8
		// ±.....................

		for (var i = 1; i < 100; i += 2)
		{
			if (i < percent)
			{
				Console.Write((char)178);
			}
			else if (i == percent)
			{
				Console.Write((char)177);
			}
			else
			{
				Console.Write((char)176);
			}
		}
		Console.Write(" {0}%", percent);
	}
}
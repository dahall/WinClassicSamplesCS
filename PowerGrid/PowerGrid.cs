using Windows.Devices.Power;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization.DateTimeFormatting;
using Windows.Globalization.NumberFormatting;

class Program
{
	[STAThread]
	static void Main()
	{
		// Do calculations immediately.
		PerformForecastCalculations();

		// If the forecast changes, then do the calculations again.
		var token = PowerGridForecast.ForecastUpdated += (s,e) => PerformForecastCalculations();

		// Wait until the user presses a key to exit.
		Console.Write("Waiting for the forecast to change...\n");
		Console.Write("Press Enter to exit the program.\n");
		Console.ReadKey(true);

		// Clean up.
		PowerGridForecast.ForecastUpdated(token); // unsubscribe from event
	}

	static string FormatDateTime(DateTime dateTime) => dateTime.ToShortDateString();

	static string FormatSeverity(double severity) => $"{severity:0.00}";

	static void ShowForecast()
	{
		PowerGridForecast gridForecast = PowerGridForecast.GetForecast();

		// If the API cannot obtain a forecast, the forecast is empty
		IVectorView<PowerGridData> forecast = gridForecast.Forecast();
		if (forecast.Size() > 0)
		{
			// Print some forecast general information.
			DateTime blockStartTime = gridForecast.StartTime();
			TimeSpan blockDuration = gridForecast.BlockDuration();

			Console.Write($"Forecast start time:{FormatDateTime(blockStartTime)}\n");
			Console.Write($"Forecast block duration (minutes): {blockDuration}\n");
			Console.Write("\n");

			// Print each entry in the forecast.
			foreach (PowerGridData data in forecast)
			{
				Console.Write($"Date/Time: {FormatDateTime(blockStartTime)}, Severity: {FormatSeverity(data.Severity())} , Is low user impact: {(data.IsLowUserExperienceImpact() ? "Yes" : "No")}\n");
				blockStartTime += blockDuration;
			}
		}

		else
		{
			Console.Write("No forecast available. Try again later.\n");
		}
		Console.Write("\n");
	}


	// Calculate the index of the forecast entry that contains the requested time.
	// If the time is before the start of the forecast, then returns 0.
	// If the time is past the end of the forecast, then returns the number of forecasts.
	static int GetForecastIndexContainingTime(PowerGridForecast gridForecast, DateTime time)
	{
		TimeSpan blockDuration = gridForecast.BlockDuration();

		// Avoid division by zero.
		if (blockDuration.count() == 0)
		{
			return 0;
		}

		var startBlock = (int)((time - gridForecast.StartTime()) / blockDuration);
		return std.clamp(startBlock, 0, (int)(gridForecast.Forecast().Size()));
	}

	static void FindBest(TimeSpan lookAhead, bool restrictToLowUXImpact)
	{
		PowerGridForecast gridForecast = PowerGridForecast.GetForecast();

		// Find the first and last blocks that include the time range we are
		// interested in.
		DateTime startTime = clock.now();
		DateTime endTime = startTime + lookAhead;

		int startBlock = GetForecastIndexContainingTime(gridForecast, startTime);
		int endBlock = GetForecastIndexContainingTime(gridForecast, endTime + gridForecast.BlockDuration());

		double lowestSeverity = (std.numeric_limits<double>.max)();
		DateTime timeWithLowestSeverity = (DateTime.max)();

		for (int index = startBlock; index < endBlock; ++index)
		{
			PowerGridData data = gridForecast.Forecast().GetAt(index);

			// If we are restricting to low impact, then use only low impact time periods.
			if (restrictToLowUXImpact && !data.IsLowUserExperienceImpact())
			{
				continue;
			}

			// If the severity is not an improvement, then don't use this one.
			double severity = data.Severity();
			if (severity >= lowestSeverity)
			{
				continue;
			}

			lowestSeverity = severity;
			timeWithLowestSeverity = gridForecast.StartTime() + gridForecast.BlockDuration();
		}

		// Print the results.
		if (lowestSeverity <= 1.0)
		{
			Console.Write($"{FormatDateTime(timeWithLowestSeverity)} to {FormatDateTime(timeWithLowestSeverity + gridForecast.BlockDuration())} (severity = {FormatSeverity(lowestSeverity)})\n");
		}
		else
		{
			Console.Write("Unable to find a good time to do work\n");
		}
		Console.Write("\n");
	}

	static void PerformForecastCalculations()
	{
		// Show the entire forecast.
		ShowForecast();

		// Arbitrarily look ahead 10 hours with low user impact.
		Console.Write("Best time to do work in the next 10 hours with low user experience impact:\n");
		FindBest(TimeSpan.FromHours(10), true);

		// Arbitrarily look ahead 10 hours with no regard for low user impact.
		Console.Write("Best time to do work in the next 10 hours without regard for user experience impact:\n");
		FindBest(TimeSpan.FromHours(10), false);
	}
}
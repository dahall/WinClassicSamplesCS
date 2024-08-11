using CustomResourceManager;
using Vanara.PInvoke;
using static Vanara.PInvoke.AclUI;

CSecInfo cSecInfo = new();
bool bResult;
if (cSecInfo.m_bFailedToConstruct)
{
	Console.Write("Couldn't construct our class. Exiting.\n");
	return 1;
}

do
{
	// Print the following: ------------------------------------
	// 1. Contoso forums
	// 2. Sports
	// 3. Favorite team
	// 4. Upcoming events
	// 5. Movies
	// 6. 2012 releases
	// 7. Classics
	// 8. Hobbies
	// 9. Learning to cook
	// 10. Snowboarding
	// 11. Help ------------------------------------
	Console.Write("------------------------------------\n");
	for (int i = 0; i < 10; i++)
		Console.Write($"{i + 1}. {cSecInfo.GetResource((ResourceIndices)i).Name}\n");
	Console.Write("11. Help\n");
	Console.Write("------------------------------------\n");
	Console.Write("Input a number to view/edit the security descriptor " +
		"for one of the above: ");
	int choice = int.Parse(Console.ReadLine() ?? "11");

	if (choice is >= 1 and <= 10)
	{
		cSecInfo.SetCurrentObject((ResourceIndices)choice - 1);
		Console.Write("You chose {0}\n\n",
			cSecInfo.GetResource((ResourceIndices)choice - 1).Name);
	}
	else if (choice == 11)
	{
		Console.Write("\n" +
			"This resource manager example models a set of forums" +
			". On the top level, there is a single FORUMS object " +
			"({0}). It has three children, which are SECTIONs ({1}," +
			" {2}, and {3}), and they each have two children, which" +
			" are TOPICs. Inheritable ACEs follow this inheritance" +
			" hierarchy. Choose a number (1-{4}) to edit any of " +
			"their security descriptors.\n\n",
			cSecInfo.GetResource(ResourceIndices.CONTOSO_FORUMS).Name,
			cSecInfo.GetResource(ResourceIndices.SPORTS).Name,
			cSecInfo.GetResource(ResourceIndices.MOVIES).Name,
			cSecInfo.GetResource(ResourceIndices.HOBBIES).Name,
			10);
		continue;
	}
	else
	{
		break;
	}

	// Suppress warning about the param being 0
	bResult = EditSecurity(default, cSecInfo);
	if (!bResult)
	{
		Console.Write("EditSecurity error {0}\n", Win32Error.GetLastError());
		break;
	}
} while (true);

return 0;
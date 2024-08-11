namespace CustomResourceManager;

internal enum ResourceIndices
{
	NONEXISTENT_OBJECT = -1,
	CONTOSO_FORUMS,
	SPORTS,
	FAVORITE_TEAM,
	UPCOMING_EVENTS,
	MOVIES,
	NEW_RELEASES,
	CLASSICS,
	HOBBIES,
	LEARNING_TO_COOK,
	SNOWBOARDING,
};

internal enum ResourceType
{
	FORUM,
	SECTION,
	TOPIC
};

// This class represents a FORUM, SECTION, or TOPIC.
internal class Resource(string name, [In] ResourceType type, string sd, ResourceIndices parentIndex)
{
	public List<ResourceIndices> ChildIndices { get; } = [];

	public string Name { get; } = name;

	public ResourceIndices ParentIndex { get; } = parentIndex;

	// All of these functions are basic accessors/mutators
	public string SD { get; set; } = sd;

	public ResourceType Type { get; } = type;

	// Adds to childIndices
	public void AddChild(ResourceIndices index) => ChildIndices.Add(index);

	// Returns true if this is a FORUM or SECTION
	public bool IsContainer => Type is ResourceType.FORUM or ResourceType.SECTION;
}
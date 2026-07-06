namespace Shiny.Contacts;

public class ContactRelationship
{
    public ContactRelationship() { }

    public ContactRelationship(string name, RelationshipType type = RelationshipType.Other, string? label = null)
    {
        Name = name;
        Type = type;
        Label = label;
    }

    public string Name { get; set; } = string.Empty;
    public RelationshipType Type { get; set; } = RelationshipType.Other;
    public string? Label { get; set; }

    public override string ToString() => Name;
}

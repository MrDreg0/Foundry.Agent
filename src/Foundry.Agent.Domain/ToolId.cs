namespace Foundry.Agent.Domain;

public class ToolId : IEquatable<ToolId>
{
  public required string Domain { get; set; }
  public required string Name { get; set; }
  public required int Major { get; set; }

  public string Value => $"{Domain}.{Name}@{Major}";
  
  public static ToolId Parse(string value)
  {
    var parts = value.Split('@');
    if (parts.Length != 2 || !int.TryParse(parts[1], out var major))
    {
      throw new ArgumentException("Invalid ToolId format, expected domain.name@major", nameof(value));
    }
    
    var nameParts = parts[0].Split('.');
    if (nameParts.Length != 2)
    {
      throw new ArgumentException("Invalid ToolId format, expected domain.name@major", nameof(value));
    }

    return new ToolId
    {
      Domain = nameParts[0],
      Name = nameParts[1],
      Major = major
    };
  }

  public override string ToString() => Value;

  public bool Equals(ToolId? other) => other is not null && Value == other.Value;
  public override bool Equals(object? obj) => Equals(obj as ToolId);
  public override int GetHashCode() => Value.GetHashCode();
}

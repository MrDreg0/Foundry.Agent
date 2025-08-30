namespace Foundry.Agent.Domain;

public readonly record struct ActionName(string Value)
{
  public static ActionName Parse(string value)
  {
    return string.IsNullOrWhiteSpace(value) 
      ? throw new ArgumentException("Value cannot be empty.", nameof(value)) 
      : new ActionName(value.ToLowerInvariant());
  }
  
  public override string ToString() => Value;
}

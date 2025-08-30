namespace Foundry.Agent.Application.Agent;

public interface IAgent
{
  Task<AgentResult> Run(string userPrompt, string model, CancellationToken cancellationToken);
}

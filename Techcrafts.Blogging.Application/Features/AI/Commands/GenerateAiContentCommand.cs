using MediatR;

namespace Techcrafts.Blogging.Application.Features.AI.Commands;

public record GenerateAiContentCommand(string Prompt) : IRequest<string>;

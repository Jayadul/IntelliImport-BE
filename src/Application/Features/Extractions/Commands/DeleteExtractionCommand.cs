using IntelliImport.Domain.Interfaces;
using IntelliImport.Domain.ValueObjects;
using MediatR;

namespace IntelliImport.Application.Features.Extractions.Commands;

using CommandResult = IntelliImport.Domain.ValueObjects.Result;

public sealed record DeleteExtractionCommand(Guid Id) : IRequest<CommandResult>;

public sealed class DeleteExtractionCommandHandler(IExtractionRepository repository)
    : IRequestHandler<DeleteExtractionCommand, CommandResult>
{
    public async Task<CommandResult> Handle(DeleteExtractionCommand request, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(request.Id, ct);
        return deleted
            ? CommandResult.Ok
            : CommandResult.Failure($"Extraction {request.Id} not found.", "NOT_FOUND");
    }
}

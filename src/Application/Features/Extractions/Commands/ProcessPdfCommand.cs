using IntelliImport.Application.Models;
using IntelliImport.Domain.ValueObjects;
using MediatR;

namespace IntelliImport.Application.Features.Extractions.Commands;

public sealed record ProcessPdfCommand(
    string FileName,
    byte[] FileBytes
) : IRequest<Result<ExtractionDetailDto>>;

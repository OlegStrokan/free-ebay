using Application.Common;
using Application.DTOs;

namespace Application.Commands.UpdateQuoteDraft;

public record UpdateQuoteDraftCommand(
    Guid B2BOrderId,
    List<QuoteItemChangeDto> Changes,
    string? Comment,
    string? CommentAuthor) : ICommand<Result>;

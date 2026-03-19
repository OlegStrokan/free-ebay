using Application.Common;
using Application.DTOs;
using Application.Mappers;
using Domain.Interfaces;
using Domain.ValueObjects;
using MediatR;

namespace Application.Queries.GetPaymentByOrderId;

internal sealed class GetPaymentByOrderIdQueryHandler(IPaymentRepository paymentRepository)
    : IRequestHandler<GetPaymentByOrderIdQuery, Result<PaymentDetailsDto>>
{
    public async Task<Result<PaymentDetailsDto>> Handle(
        GetPaymentByOrderIdQuery request,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = IdempotencyKey.From(request.IdempotencyKey);
        var payment = await paymentRepository.GetByOrderIdAndIdempotencyKeyAsync(
            request.OrderId,
            idempotencyKey,
            cancellationToken);

        if (payment is null)
        {
            return Result<PaymentDetailsDto>.Failure(
                $"Payment for order '{request.OrderId}' and idempotency key '{request.IdempotencyKey}' was not found.");
        }

        return Result<PaymentDetailsDto>.Success(PaymentDtoMapper.ToPaymentDetailsDto(payment));
    }
}
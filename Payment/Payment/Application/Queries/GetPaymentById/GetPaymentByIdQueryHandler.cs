using Application.Common;
using Application.DTOs;
using Application.Mappers;
using Domain.Interfaces;
using Domain.ValueObjects;
using MediatR;

namespace Application.Queries.GetPaymentById;

internal sealed class GetPaymentByIdQueryHandler(IPaymentRepository paymentRepository)
    : IRequestHandler<GetPaymentByIdQuery, Result<PaymentDetailsDto>>
{
    public async Task<Result<PaymentDetailsDto>> Handle(
        GetPaymentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var paymentId = PaymentId.From(request.PaymentId);
        var payment = await paymentRepository.GetByIdAsync(paymentId, cancellationToken);

        if (payment is null)
        {
            return Result<PaymentDetailsDto>.Failure($"Payment '{request.PaymentId}' was not found");
        }

        return Result<PaymentDetailsDto>.Success(PaymentDtoMapper.ToPaymentDetailsDto(payment));
    }
}
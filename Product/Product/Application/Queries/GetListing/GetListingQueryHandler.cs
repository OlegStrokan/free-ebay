using Application.DTOs;
using Application.Interfaces;
using Domain.Exceptions;
using MediatR;

namespace Application.Queries.GetListing;

internal sealed class GetListingQueryHandler : IRequestHandler<GetListingQuery, ProductDetailDto>
{
    private readonly IListingReadRepository _readRepository;

    public GetListingQueryHandler(IListingReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<ProductDetailDto> Handle(GetListingQuery request, CancellationToken cancellationToken)
    {
        var listing = await _readRepository.GetByIdAsync(request.ListingId, cancellationToken);

        if (listing is null)
            throw new ProductNotFoundException(request.ListingId);

        return listing;
    }
}

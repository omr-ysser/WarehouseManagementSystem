using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Riders.Queries;

public sealed class GetRidersHandler(IRiderService service)
    : IRequestHandler<GetRidersQuery, PageResponse<RiderResponse>>
{
    public Task<PageResponse<RiderResponse>> Handle(
        GetRidersQuery request,
        CancellationToken cancellationToken)
    {
        return service.GetRidersAsync(
            request.Search,
            request.Available,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}

public sealed class GetRiderByIdHandler(IRiderService service)
    : IRequestHandler<GetRiderByIdQuery, RiderResponse>
{
    public Task<RiderResponse> Handle(
        GetRiderByIdQuery request,
        CancellationToken cancellationToken)
    {
        return service.GetRiderAsync(
            request.Id,
            cancellationToken);
    }
}

public sealed class GetRiderTournamentsHandler(IRiderService service)
    : IRequestHandler<
        GetRiderTournamentsQuery,
        PageResponse<ParticipationResponse>>
{
    public Task<PageResponse<ParticipationResponse>> Handle(
        GetRiderTournamentsQuery request,
        CancellationToken cancellationToken)
    {
        return service.GetParticipationsAsync(
            request.RiderId,
            request.Search,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}

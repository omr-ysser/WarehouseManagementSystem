using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.DTOs;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Riders.Queries;

public sealed record GetRidersQuery(
    string? Search = null,
    bool? Available = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<PageResponse<RiderResponse>>;

public sealed record GetRiderByIdQuery(int Id)
    : IRequest<RiderResponse>;

public sealed record GetRiderTournamentsQuery(
    int RiderId,
    string? Search = null,
    ParticipationStatus? Status = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<PageResponse<ParticipationResponse>>;

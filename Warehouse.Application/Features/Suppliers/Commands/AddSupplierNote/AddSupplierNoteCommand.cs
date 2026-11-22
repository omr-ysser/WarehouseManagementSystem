using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Commands.AddSupplierNote;

public sealed record AddSupplierNoteCommand(
    int SupplierId,
    AddSupplierNoteRequest Data,
    string ActorAccountId)
    : IRequest<SupplierNoteResponse>;

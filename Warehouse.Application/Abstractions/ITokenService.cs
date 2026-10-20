using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Application.DTOs;

namespace Warehouse.Application.Abstractions;

public interface ITokenService
{
    LoginResponse CreateToken(AuthenticatedUser user);
}

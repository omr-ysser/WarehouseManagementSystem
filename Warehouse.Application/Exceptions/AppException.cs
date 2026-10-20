using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.Exceptions;

public sealed class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

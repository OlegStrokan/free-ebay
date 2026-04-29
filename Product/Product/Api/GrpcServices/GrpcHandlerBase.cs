using Domain.Exceptions;
using Grpc.Core;

namespace Api.GrpcServices;

public abstract class GrpcHandlerBase(ILogger logger)
{
    protected void HandleException(Exception ex, string methodName)
    {
        if (ex is ProductNotFoundException notFound)
        {
            logger.LogWarning(ex, "Entity {Id} not found in {Method}", notFound.ProductId, methodName);
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        if (ex is FormatException)
        {
            logger.LogWarning(ex, "Invalid GUID format in {Method}", methodName);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ID format."));
        }
        logger.LogError(ex, "Error during {Method}", methodName);
        throw new RpcException(new Status(StatusCode.Internal, $"Internal error in {methodName}"));
    }
}

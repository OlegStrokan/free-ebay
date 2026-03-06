using Api.Mappers;
using Application.UseCases.GetUserById;
using Grpc.Core;
using Protos.User;
using GetUserByIdResponseProto = Protos.User.GetUserByIdResponse;


namespace Api.GrpcServices;

public class GetUserByIdGrpcService(IGetUserByIdUseCase useCase) : UserServiceProto.UserServiceProtoBase 
{
    public override async Task<GetUserByIdResponseProto> GetUserById(GetUserByidRequest request,
        ServerCallContext context)
    {
        var result = await useCase.ExecuteAsync(request.Id);
        if (result == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound,
                $"User with ID  {request.Id} not found"));
        }

        return result.ToProto();
    }
}
using Api.Mappers;
using Application.UseCases.UpdateUser;
using Grpc.Core;
using Protos.User;
using UpdateUserResponseProto = Protos.User.UpdateUserResponse;
namespace Api.GrpcServices;

public class UpdateUserGrpcService(IUpdateUserUseCase useCase) : UserServiceProto.UserServiceProtoBase
{
    public override async Task<UpdateUserResponseProto> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        try
        {
            var result = await useCase.ExecuteAsync(new UpdateUserCommand(
                request.Id, request.Email, request.FullName, request.Phone));

            return result.ToProto();
            
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }

    }

}
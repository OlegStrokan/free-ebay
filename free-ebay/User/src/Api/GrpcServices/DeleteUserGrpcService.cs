using Application.UseCases.DeleteUser;
using Grpc.Core;
using Protos.User;

namespace Api.GrpcServices;

public class DeleteUserGrpcService(IDeleteUserUseCase useCase) : UserServiceProto.UserServiceProtoBase 
{
    public override async Task<DeleteUserResponse> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        await useCase.ExecuteAsync(request.Id);
        return new DeleteUserResponse{};
    }
}
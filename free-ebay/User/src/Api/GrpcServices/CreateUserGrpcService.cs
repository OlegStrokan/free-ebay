using Application.UseCases.CreateUser;
using Grpc.Core;
using Api.Mappers;
using Protos.User;
using CreateUserResponseProto = Protos.User.CreateUserResponse;

namespace Api.GrpcServices;

public class CreateUserGrpcService(ICreateUserUseCase useCase) : UserServiceProto.UserServiceProtoBase
{

    public override async Task<CreateUserResponseProto> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        var user = await useCase.ExecuteAsync(new CreateUserCommand(request.Email, request.Password, request.FullName, request.Phone));
        return user.ToProto();
    }

}
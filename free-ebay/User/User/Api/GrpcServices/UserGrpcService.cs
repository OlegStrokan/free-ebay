using Api.Mappers;
using Application.UseCases.BlockUser;
using Application.UseCases.CreateUser;
using Application.UseCases.DeleteUser;
using Application.UseCases.GetUserById;
using Application.UseCases.UpdatePassword;
using Application.UseCases.UpdateUser;
using Grpc.Core;
using Protos.User;
using BlockUserResponseProto = Protos.User.BlockUserResponse;
using CreateUserResponseProto = Protos.User.CreateUserResponse;
using GetUserByIdResponseProto = Protos.User.GetUserByIdResponse;
using UpdateUserResponseProto = Protos.User.UpdateUserResponse;

namespace Api.GrpcServices;

public class UserGrpcService(
    ICreateUserUseCase createUserUseCase,
    IUpdateUserUseCase updateUserUseCase,
    IGetUserByIdUseCase getUserByIdUseCase,
    IDeleteUserUseCase deleteUserUseCase,
    IBlockUserUseCase blockUserUseCase,
    IUpdatePasswordUseCase updatePasswordUseCase) : UserServiceProto.UserServiceProtoBase
{
    public override async Task<CreateUserResponseProto> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        try
        {
            var user = await createUserUseCase.ExecuteAsync(new CreateUserCommand(
                request.Email,
                request.Password,
                request.FullName,
                request.Phone,
                string.IsNullOrWhiteSpace(request.CountryCode) ? "DE" : request.CountryCode,
                request.CustomerTier.ToEntity()));

            return user.ToProto();
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
    }

    public override async Task<UpdateUserResponseProto> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        try
        {
            var result = await updateUserUseCase.ExecuteAsync(new UpdateUserCommand(
                request.Id,
                request.Email,
                request.FullName,
                request.Phone,
                string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode,
                request.CustomerTier.ToEntity()));

            return result.ToProto();
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
    }

    public override async Task<GetUserByIdResponseProto> GetUserById(GetUserByIdRequest request, ServerCallContext context)
    {
        var result = await getUserByIdUseCase.ExecuteAsync(request.Id);
        if (result == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound,
                $"User with ID  {request.Id} not found"));
        }

        return result.ToProto();
    }

    public override async Task<DeleteUserResponse> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        await deleteUserUseCase.ExecuteAsync(request.Id);
        return new DeleteUserResponse();
    }

    public override async Task<BlockUserResponseProto> BlockUser(BlockUserRequest request, ServerCallContext context)
    {
        try
        {
            var result = await blockUserUseCase.ExecuteAsync(new BlockUserCommand(request.Id));
            return result.ToProto();
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<UpdatePasswordResponse> UpdatePassword(UpdatePasswordRequest request, ServerCallContext context)
    {
        try
        {
            await updatePasswordUseCase.ExecuteAsync(new UpdatePasswordCommand(
                request.Id,
                request.CurrentPassword,
                request.NewPassword));

            return new UpdatePasswordResponse();
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }
}

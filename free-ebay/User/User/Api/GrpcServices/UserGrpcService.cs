using Api.Mappers;
using Application.UseCases.BlockUser;
using Application.UseCases.CreateUser;
using Application.UseCases.DeleteUser;
using Application.UseCases.GetUserByEmail;
using Application.UseCases.GetUserById;
using Application.UseCases.UpdatePassword;
using Application.UseCases.UpdateUserPassword;
using Application.UseCases.UpdateUser;
using Application.UseCases.VerifyCredentials;
using Application.UseCases.VerifyUserEmail;
using Grpc.Core;
using Protos.User;
using BlockUserResponseProto = Protos.User.BlockUserResponse;
using CreateUserResponseProto = Protos.User.CreateUserResponse;
using GetUserByEmailResponseProto = Protos.User.GetUserByEmailResponse;
using GetUserByIdResponseProto = Protos.User.GetUserByIdResponse;
using UpdateUserPasswordResponseProto = Protos.User.UpdateUserPasswordResponse;
using UpdateUserResponseProto = Protos.User.UpdateUserResponse;
using VerifyCredentialsResponseProto = Protos.User.VerifyCredentialsResponse;
using VerifyUserEmailResponseProto = Protos.User.VerifyUserEmailResponse;

namespace Api.GrpcServices;

public class UserGrpcService(
    ICreateUserUseCase createUserUseCase,
    IUpdateUserUseCase updateUserUseCase,
    IGetUserByIdUseCase getUserByIdUseCase,
    IGetUserByEmailUseCase getUserByEmailUseCase,
    IVerifyCredentialsUseCase verifyCredentialsUseCase,
    IDeleteUserUseCase deleteUserUseCase,
    IBlockUserUseCase blockUserUseCase,
    IUpdatePasswordUseCase updatePasswordUseCase,
    IVerifyUserEmailUseCase verifyUserEmailUseCase,
    IUpdateUserPasswordUseCase updateUserPasswordUseCase) : UserServiceProto.UserServiceProtoBase
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
                request.CountryCode,
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

    public override async Task<GetUserByEmailResponseProto> GetUserByEmail(
        GetUserByEmailRequest request,
        ServerCallContext context)
    {
        try
        {
            var user = await getUserByEmailUseCase.ExecuteAsync(request.Email);
            return user.ToProto();
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<VerifyCredentialsResponseProto> VerifyCredentials(
        VerifyCredentialsRequest request,
        ServerCallContext context)
    {
        try
        {
            var user = await verifyCredentialsUseCase.ExecuteAsync(request.Email, request.Password);
            return user.ToProto();
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<VerifyUserEmailResponseProto> VerifyUserEmail(
        VerifyUserEmailRequest request,
        ServerCallContext context)
    {
        try
        {
            var success = await verifyUserEmailUseCase.ExecuteAsync(request.UserId);
            return new VerifyUserEmailResponseProto { Success = success };
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<UpdateUserPasswordResponseProto> UpdateUserPassword(
        UpdateUserPasswordRequest request,
        ServerCallContext context)
    {
        try
        {
            var result = await updateUserPasswordUseCase.ExecuteAsync(
                new UpdateUserPasswordCommand(request.UserId, request.NewPasswordHash));

            return new UpdateUserPasswordResponseProto
            {
                Success = result.Success,
                Message = result.Message,
            };
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }
}

using Api.GrpcServices;
using Application.UseCases.BlockUser;
using Application.UseCases.CreateUser;
using Application.UseCases.DeleteUser;
using Application.UseCases.GetUserById;
using Application.UseCases.UpdatePassword;
using Application.UseCases.UpdateUser;
using NSubstitute;

namespace Api.Tests.TestHelpers;

internal static class UserGrpcServiceTestFactory
{
    public static UserGrpcService Create(
        ICreateUserUseCase? createUserUseCase = null,
        IUpdateUserUseCase? updateUserUseCase = null,
        IGetUserByIdUseCase? getUserByIdUseCase = null,
        IDeleteUserUseCase? deleteUserUseCase = null,
        IBlockUserUseCase? blockUserUseCase = null,
        IUpdatePasswordUseCase? updatePasswordUseCase = null)
    {
        return new UserGrpcService(
            createUserUseCase ?? Substitute.For<ICreateUserUseCase>(),
            updateUserUseCase ?? Substitute.For<IUpdateUserUseCase>(),
            getUserByIdUseCase ?? Substitute.For<IGetUserByIdUseCase>(),
            deleteUserUseCase ?? Substitute.For<IDeleteUserUseCase>(),
            blockUserUseCase ?? Substitute.For<IBlockUserUseCase>(),
            updatePasswordUseCase ?? Substitute.For<IUpdatePasswordUseCase>());
    }
}

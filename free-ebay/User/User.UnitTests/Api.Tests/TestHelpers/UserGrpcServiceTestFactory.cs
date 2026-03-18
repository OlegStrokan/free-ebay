using Api.GrpcServices;
using Application.UseCases.BlockUser;
using Application.UseCases.CreateUser;
using Application.UseCases.DeleteUser;
using Application.UseCases.GetUserByEmail;
using Application.UseCases.GetUserById;
using Application.UseCases.UpdatePassword;
using Application.UseCases.UpdateUserPassword;
using Application.UseCases.UpdateUser;
using Application.UseCases.VerifyUserEmail;
using NSubstitute;

namespace Api.Tests.TestHelpers;

internal static class UserGrpcServiceTestFactory
{
    public static UserGrpcService Create(
        ICreateUserUseCase? createUserUseCase = null,
        IUpdateUserUseCase? updateUserUseCase = null,
        IGetUserByIdUseCase? getUserByIdUseCase = null,
        IGetUserByEmailUseCase? getUserByEmailUseCase = null,
        IDeleteUserUseCase? deleteUserUseCase = null,
        IBlockUserUseCase? blockUserUseCase = null,
        IUpdatePasswordUseCase? updatePasswordUseCase = null,
        IVerifyUserEmailUseCase? verifyUserEmailUseCase = null,
        IUpdateUserPasswordUseCase? updateUserPasswordUseCase = null)
    {
        return new UserGrpcService(
            createUserUseCase ?? Substitute.For<ICreateUserUseCase>(),
            updateUserUseCase ?? Substitute.For<IUpdateUserUseCase>(),
            getUserByIdUseCase ?? Substitute.For<IGetUserByIdUseCase>(),
            getUserByEmailUseCase ?? Substitute.For<IGetUserByEmailUseCase>(),
            deleteUserUseCase ?? Substitute.For<IDeleteUserUseCase>(),
            blockUserUseCase ?? Substitute.For<IBlockUserUseCase>(),
            updatePasswordUseCase ?? Substitute.For<IUpdatePasswordUseCase>(),
            verifyUserEmailUseCase ?? Substitute.For<IVerifyUserEmailUseCase>(),
            updateUserPasswordUseCase ?? Substitute.For<IUpdateUserPasswordUseCase>());
    }
}

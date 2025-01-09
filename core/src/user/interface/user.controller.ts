import {
  Body,
  Controller,
  Delete,
  Get,
  Inject,
  Patch,
  Post,
  Query,
  UseInterceptors,
} from '@nestjs/common';
import { IGetUserByIdUseCase } from '../epplication/use-cases/get-user-by-id/get-user-by-id.interface';
import { MetricsInterceptor } from 'src/shared/interceptors/metrics.interceptor';
import { IGetUsersUseCase } from '../epplication/use-cases/get-users/get-users.interface';
import { User, UserData } from '../core/entity/user';
import { IUserMapper } from '../infrastructure/mappers/user.mapper.interface';
import { UserDb } from '../infrastructure/entity/user.entity';
import { IUpdateUserUseCase } from '../epplication/use-cases/update-user/update-user.interface';
import { ICreateUserUseCase } from '../epplication/use-cases/create-user/create-user.interface';
import { CreateUserDto } from './dtos/create-user.dto';
import { UpdateUserDto } from './dtos/update-user.dto';
import { IDeleteUserUseCase } from '../epplication/use-cases/delete-user/delete-user.interface';
import {
  CREATE_USER_USE_CASE,
  DELETE_USER_USE_CASE,
  GET_USER_BY_ID_USE_CASE,
  GET_USERS_USE_CASE,
  UPDATE_USER_USE_CASE,
} from '../epplication/injection-tokens/use-case.token';
import { USER_MAPPER } from '../epplication/injection-tokens/mapper.token';

@Controller('user')
export class UserController {
  constructor(
    @Inject(GET_USER_BY_ID_USE_CASE)
    private readonly getUserByIdUseCase: IGetUserByIdUseCase,
    @Inject(GET_USERS_USE_CASE)
    private readonly getUsersUseCase: IGetUsersUseCase,
    @Inject(CREATE_USER_USE_CASE)
    private readonly createUserUseCase: ICreateUserUseCase,
    @Inject(UPDATE_USER_USE_CASE)
    private readonly updateUserUseCase: IUpdateUserUseCase,
    @Inject(DELETE_USER_USE_CASE)
    private readonly deleteUserUseCase: IDeleteUserUseCase,
    @Inject(USER_MAPPER)
    private readonly userMapper: IUserMapper<UserData, User, UserDb>,
  ) {}

  @UseInterceptors(MetricsInterceptor)
  @Get('')
  async getUsers(): Promise<UserData[]> {
    const users = await this.getUsersUseCase.execute();
    return users.length > 0
      ? users.map((user) => this.userMapper.toClient(user))
      : [];
  }
  @Get(':id')
  async getUserById(@Query('id') id: string) {
    const user = await this.getUserByIdUseCase.execute(id);
    return this.userMapper.toClient(user);
  }

  @Post()
  async createUser(@Body() dto: CreateUserDto) {
    return await this.createUserUseCase.execute(dto);
  }

  @Patch(':id')
  async updateUser(@Query('id') id: string, @Body() dto: UpdateUserDto) {
    const user = await this.updateUserUseCase.execute({ id, dto });
    return this.userMapper.toClient(user);
  }

  @Delete(':id')
  async deleteUser(@Query('id') id: string) {
    await this.deleteUserUseCase.execute(id);
  }
}

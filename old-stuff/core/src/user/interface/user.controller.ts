import {
  Body,
  Controller,
  Delete,
  Get,
  Patch,
  Post,
  Query,
  UseInterceptors,
} from '@nestjs/common';
import { IGetUserByIdUseCase } from '../epplication/use-cases/get-user-by-id/get-user-by-id.interface';
import { MetricsInterceptor } from 'src/shared/interceptors/metrics.interceptor';
import { IGetUsersUseCase } from '../epplication/use-cases/get-users/get-users.interface';
import { UserData } from '../core/entity/user';
import { IUserMapper } from '../infrastructure/mappers/user.mapper.interface';
import { IUpdateUserUseCase } from '../epplication/use-cases/update-user/update-user.interface';
import { ICreateUserUseCase } from '../epplication/use-cases/create-user/create-user.interface';
import { CreateUserDto } from './dtos/create-user.dto';
import { UpdateUserDto } from './dtos/update-user.dto';
import { IDeleteUserUseCase } from '../epplication/use-cases/delete-user/delete-user.interface';

@Controller('user')
export class UserController {
  constructor(
    private readonly getUserByIdUseCase: IGetUserByIdUseCase,
    private readonly getUsersUseCase: IGetUsersUseCase,
    private readonly createUserUseCase: ICreateUserUseCase,
    private readonly updateUserUseCase: IUpdateUserUseCase,
    private readonly deleteUserUseCase: IDeleteUserUseCase,
    private readonly userMapper: IUserMapper,
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

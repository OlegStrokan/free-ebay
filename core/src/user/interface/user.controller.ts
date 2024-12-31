import {
  Controller,
  Get,
  Inject,
  Query,
  UseInterceptors,
} from '@nestjs/common';
import { IGetUserByIdUseCase } from '../epplication/use-cases/get-user-by-id/get-user-by-id.interface';
import { GetUserByIdUseCase } from '../epplication/use-cases/get-user-by-id/get-user-by-id.use-case';
import { MetricsInterceptor } from 'src/shared/interceptors/metrics.interceptor';
import { GetUsersUseCase } from '../epplication/use-cases/get-users/get-users.use-case';
import { IGetUsersUseCase } from '../epplication/use-cases/get-users/get-users.interface';
import { User, UserData } from '../core/entity/user';
import { UserMapper } from '../infrastructure/mappers/user.mapper';
import { USER_MAPPER } from '../core/repository/user.repository';
import { IUserMapper } from '../infrastructure/mappers/user.mapper.interface';
import { UserDb } from '../infrastructure/entity/user.entity';

@Controller('user')
export class UserController {
  constructor(
    @Inject(GetUserByIdUseCase)
    private readonly getUserByIdUseCase: IGetUserByIdUseCase,
    @Inject(GetUsersUseCase)
    private readonly getUsersUseCase: IGetUsersUseCase,
    @Inject(USER_MAPPER)
    private readonly userMapper: IUserMapper<UserData, User, UserDb>,
  ) {}

  @UseInterceptors(MetricsInterceptor)
  @Get('')
  async getUsers(): Promise<UserData[]> {
    const users = await this.getUsersUseCase.execute();
    return users ? users.map((user) => this.userMapper.toClient(user)) : [];
  }
  @Get(':id')
  async getUserById(@Query('id') id: string) {
    const user = this.getUserByIdUseCase.execute(id);
    return;
  }
}

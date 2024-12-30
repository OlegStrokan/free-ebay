import { Inject, Injectable, UnauthorizedException } from '@nestjs/common';
import * as bcrypt from 'bcrypt';
import { LoginDto } from 'src/auth/interface/dtos/login.dto';
import { TokenService } from '../../service/token.service';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { UserRepository } from 'src/user/infrastructure/repository/user.repository';
import { User } from 'src/user/core/entity/user';

@Injectable()
export class LoginUseCase {
  constructor(
    @Inject(UserRepository)
    private readonly userRepository: IUserRepository,
    private readonly tokenService: TokenService,
  ) {}

  async execute(dto: LoginDto) {
    const user = await this.validateUser(dto);
    const accessToken = this.tokenService.createAccessToken(user.data);
    const refreshToken = this.tokenService.createAccessToken(user.data);

    return { user, accessToken, refreshToken };
  }

  private async validateUser(dto: LoginDto): Promise<User> {
    const alreadyExistingUser = await this.userRepository.findByEmail(
      dto.email,
    );
    if (!alreadyExistingUser) throw new UnauthorizedException('Fuck you');

    const isPasswordValid = await bcrypt.compare(
      dto.password,
      alreadyExistingUser.data.password,
    );
    if (!isPasswordValid) {
      throw new UnauthorizedException('Fuck you again');
    }
    return alreadyExistingUser;
  }
}

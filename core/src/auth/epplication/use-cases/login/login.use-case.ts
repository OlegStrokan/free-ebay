import { Inject, Injectable } from '@nestjs/common';
import * as bcrypt from 'bcrypt';
import { LoginRequestDto } from 'src/auth/interface/dtos/login-request.dto';
import { TokenService } from '../../service/token.service';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { User } from 'src/user/core/entity/user';
import { USER_REPOSITORY } from 'src/user/epplication/injection-tokens/repository.token';
import { ILoginUseCase } from './login.interface';
import { InvalidLoginCredentialsException } from 'src/auth/core/exceptions/invalid-login-credentials.exception';

@Injectable()
export class LoginUseCase implements ILoginUseCase {
  constructor(
    @Inject(USER_REPOSITORY)
    private readonly userRepository: IUserRepository,
    private readonly tokenService: TokenService,
  ) {}

  async execute(dto: LoginRequestDto) {
    const user = await this.validateUser(dto);
    const accessToken = this.tokenService.createAccessToken(user.data);
    const refreshToken = this.tokenService.createAccessToken(user.data);

    return { user, accessToken, refreshToken };
  }

  private async validateUser(dto: LoginRequestDto): Promise<User> {
    const alreadyExistingUser = await this.userRepository.findByEmail(
      dto.email,
    );
    if (!alreadyExistingUser) throw new InvalidLoginCredentialsException();

    const isPasswordValid = await bcrypt.compare(
      dto.password,
      alreadyExistingUser.data.password,
    );
    if (!isPasswordValid) {
      throw new InvalidLoginCredentialsException();
    }
    return alreadyExistingUser;
  }
}

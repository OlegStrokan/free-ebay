import { Injectable } from '@nestjs/common';
import * as bcrypt from 'bcrypt';
import { LoginRequestDto } from 'src/auth/interface/dtos/login-request.dto';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { User } from 'src/user/core/entity/user';
import { InvalidLoginCredentialsException } from 'src/auth/core/exceptions/invalid-login-credentials.exception';
import { ILoginUseCase } from './login.interface';
import { ITokenService } from '../../service/token.service.interface';

@Injectable()
export class LoginUseCase implements ILoginUseCase {
  constructor(
    private readonly userRepository: IUserRepository,
    private readonly tokenService: ITokenService,
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

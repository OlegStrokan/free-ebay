import * as bcrypt from 'bcrypt';
import * as jwt from 'jsonwebtoken';
import { ILoginUseCase } from './login.interface';
import { Injectable } from '@nestjs/common';
import { TokenRepository } from 'src/auth/infrastructure/repository/token.repository';
import { UserRepository } from 'src/auth/infrastructure/repository/user.repository';
import { LoginDto } from 'src/auth/interface/dtos/login.dto';

@Injectable()
export class LoginUseCase implements ILoginUseCase {
  constructor(
    private readonly userRepository: UserRepository,
    private readonly tokenRepository: TokenRepository,
  ) {}

  async execute(dto: LoginDto): Promise<any> {
    const user = await this.userRepository.findByUsername(dto.username);
    if (!user) {
      throw new Error('User not found');
    }

    const isPasswordValid = await bcrypt.compare(dto.password, user.password);
    if (!isPasswordValid) {
      throw new Error('Invalid password');
    }

    const accessToken = jwt.sign({ userId: user.id }, 'access_secret', {
      expiresIn: '1h',
    });
    const refreshToken = jwt.sign({ userId: user.id }, 'refresh_secret', {
      expiresIn: '7d',
    });

    await this.tokenRepository.createToken(user.id, accessToken, refreshToken);

    return { accessToken, refreshToken };
  }
}

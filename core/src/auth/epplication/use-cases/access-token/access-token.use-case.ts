import { Injectable } from '@nestjs/common';
import * as jwt from 'jsonwebtoken';
import { ITokenRepository } from 'src/auth/core/repository/token.repository';
import { IAccessTokenUseCase } from './access-token.interface';
import { TokenDto } from 'src/auth/interface/dtos/token.dto';

@Injectable()
export class AccessTokenUseCase implements IAccessTokenUseCase {
  constructor(private readonly tokenRepository: ITokenRepository) {}

  async execute(dto: TokenDto): Promise<any> {
    const token = await this.tokenRepository.findByRefreshToken(
      dto.refreshToken,
    );
    if (!token) {
      throw new Error('Invalid refresh token');
    }

    try {
      const decoded = jwt.verify(dto.refreshToken, 'refresh_secret') as any;
      const newAccessToken = jwt.sign(
        { userId: decoded.userId },
        'access_secret',
        { expiresIn: '1h' },
      );
      await this.tokenRepository.updateAccessToken(
        dto.refreshToken,
        newAccessToken,
      );

      return { accessToken: newAccessToken };
    } catch (e) {
      throw new Error('Failed to verify refresh token');
    }
  }
}

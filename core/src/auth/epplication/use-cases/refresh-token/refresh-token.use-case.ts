import { Injectable } from '@nestjs/common';
import * as jwt from 'jsonwebtoken';
import { TokenDto } from 'src/auth/interface/dtos/token.dto';
import { ITokenRepository } from 'src/auth/core/repository/token.repository';
import { IRefreshTokenUseCase } from './refresh-token.interface';

@Injectable()
export class RefreshTokenUseCase implements IRefreshTokenUseCase {
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
      throw new Error('Failed to refresh token');
    }
  }
}

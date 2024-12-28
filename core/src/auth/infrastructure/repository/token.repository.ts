import { Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { TokenDb } from '../entity/token.entity';

@Injectable()
export class TokenRepository {
  constructor(
    @InjectRepository(TokenDb)
    private readonly tokenRepository: Repository<TokenDb>,
  ) {}

  async createToken(
    userId: string,
    accessToken: string,
    refreshToken: string,
  ): Promise<TokenDb> {
    const token = this.tokenRepository.create({
      userId,
      accessToken,
      refreshToken,
    });
    return await this.tokenRepository.save(token);
  }

  async findByRefreshToken(refreshToken: string): Promise<TokenDb | undefined> {
    return this.tokenRepository.findOne({ where: { refreshToken } });
  }

  async updateAccessToken(
    refreshToken: string,
    accessToken: string,
  ): Promise<void> {
    await this.tokenRepository.update({ refreshToken }, { accessToken });
  }
}

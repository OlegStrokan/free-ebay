import {
  CanActivate,
  ExecutionContext,
  Inject,
  Injectable,
  UnauthorizedException,
} from '@nestjs/common';
import { ITokenService } from '../epplication/service/token.service.interface';
import { TokenService } from '../epplication/service/token.service';

@Injectable()
export class AuthGuard implements CanActivate {
  constructor(
    @Inject(TokenService)
    private readonly tokenService: ITokenService,
  ) {}

  async canActivate(context: ExecutionContext): Promise<boolean> {
    const request = context.switchToHttp().getRequest();
    const authHeader = request.headers['authorization'];

    if (!authHeader) {
      throw new UnauthorizedException('Authorization header is missing');
    }

    const token = authHeader.split(' ')[1];
    if (!token) {
      throw new UnauthorizedException('Token is missing');
    }

    try {
      const decodedToken = this.tokenService.verifyAccessToken(token);
      request.user = decodedToken;
      return true;
    } catch (error) {
      throw new UnauthorizedException('Invalid token');
    }
  }
}

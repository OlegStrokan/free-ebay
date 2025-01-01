import { Controller, Inject, Post, Body } from '@nestjs/common';
import { ILoginUseCase } from '../epplication/use-cases/login/login.interface';
import { LoginUseCase } from '../epplication/use-cases/login/login.use-case';
import { IRegisterUseCase } from '../epplication/use-cases/register/register.interface';
import { RegisterUseCase } from '../epplication/use-cases/register/register.use-case';
import { LoginDto } from './dtos/login.dto';
import { CreateUserDto } from 'src/user/interface/dtos/create-user.dto';

@Controller('auth')
export class AuthController {
  constructor(
    @Inject(RegisterUseCase)
    private readonly registerUseCase: IRegisterUseCase,

    @Inject(LoginUseCase)
    private readonly loginUseCase: ILoginUseCase,
  ) {}

  @Post('register')
  async register(@Body() registerDto: CreateUserDto) {
    return await this.registerUseCase.execute(registerDto);
  }

  @Post('login')
  async login(@Body() loginDto: LoginDto) {
    return await this.loginUseCase.execute(loginDto);
  }
}

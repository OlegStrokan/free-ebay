import { Controller, Post, Body } from '@nestjs/common';
import { ApiTags, ApiOperation, ApiBody, ApiResponse } from '@nestjs/swagger'; // Import Swagger decorators
import { ILoginUseCase } from '../epplication/use-cases/login/login.interface';
import { IRegisterUseCase } from '../epplication/use-cases/register/register.interface';
import { LoginRequestDto } from './dtos/login-request.dto';
import { CreateUserDto } from 'src/user/interface/dtos/create-user.dto';
import { LoginResponseDto } from './dtos/login-response.dto';

@ApiTags('Authentication')
@Controller('auth')
export class AuthController {
  constructor(
    private readonly registerUseCase: IRegisterUseCase,
    private readonly loginUseCase: ILoginUseCase,
  ) {}

  @Post('register')
  @ApiOperation({ summary: 'Register a new user' })
  @ApiBody({ type: CreateUserDto, description: 'User registration data' })
  @ApiResponse({
    status: 201,
    description: 'User successfully registered',
    type: String,
  })
  @ApiResponse({
    status: 409,
    description: 'User with this email already exits',
  })
  async register(@Body() registerDto: CreateUserDto) {
    return await this.registerUseCase.execute(registerDto);
  }

  @Post('login')
  @ApiOperation({ summary: 'Login a user' })
  @ApiBody({ type: LoginRequestDto, description: 'User login data' })
  @ApiResponse({
    status: 200,
    description: 'User successfully logged',
    type: LoginResponseDto,
  })
  @ApiResponse({
    status: 401,
    description: 'Invalid email or password',
  })
  async login(@Body() loginDto: LoginRequestDto) {
    return await this.loginUseCase.execute(loginDto);
  }
}

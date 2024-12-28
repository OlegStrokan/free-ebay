@Controller('auth')
export class AuthController {
  constructor(
    @Inject(RegisterUseCase)
    private readonly registerUseCase: IRegisterUseCase,

    @Inject(LoginUseCase)
    private readonly loginUseCase: ILoginUseCase,

    @Inject(AccessTokenUseCase)
    private readonly accessTokenUseCase: IAccessTokenUseCase,

    @Inject(RefreshTokenUseCase)
    private readonly refreshTokenUseCase: IRefreshTokenUseCase,
  ) {}

  @Post('register')
  async register(@Body() registerDto: RegisterDto) {
    return await this.registerUseCase.execute(registerDto);
  }

  @Post('login')
  async login(@Body() loginDto: LoginDto) {
    return await this.loginUseCase.execute(loginDto);
  }

  @Post('access_token')
  async accessToken(@Body() tokenDto: TokenDto) {
    return await this.accessTokenUseCase.execute(tokenDto);
  }

  @Post('refresh_token')
  async refreshToken(@Body() tokenDto: TokenDto) {
    return await this.refreshTokenUseCase.execute(tokenDto);
  }
}

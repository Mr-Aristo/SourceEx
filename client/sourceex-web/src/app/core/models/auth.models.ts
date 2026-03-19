export interface RegisterUserRequest {
  userName: string;
  email: string;
  password: string;
  displayName: string;
  departmentId: string;
}

export interface LoginRequest {
  userNameOrEmail: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface LogoutRequest {
  refreshToken: string;
}

export interface AuthTokenResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: IdentityUserResponse;
  tokenType: string;
}

export interface IdentityUserResponse {
  userId: string;
  userName: string;
  email: string;
  displayName: string;
  departmentId: string;
  roles: string[];
}

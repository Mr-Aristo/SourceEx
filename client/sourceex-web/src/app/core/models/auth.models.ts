export interface GenerateTokenRequest {
  userId: string;
  departmentId: string;
  roles: string[];
}

export interface AccessTokenResponse {
  accessToken: string;
  expiresAtUtc: string;
  tokenType: string;
}

export interface CurrentUserResponse {
  userId: string;
  departmentId: string;
  roles: string[];
}


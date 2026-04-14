export interface RegisterDto {
  name: string;
  email: string;
  password: string;
  phone?: string;
}

export interface LoginDto {
  email: string;
  password: string;
}

export interface VerifyLoginOtpDto {
  challengeId: string;
  otp: string;
}

export interface ResendLoginOtpDto {
  challengeId: string;
}

export interface TokenDto {
  token: string;
}

export interface GoogleLoginDto {
  idToken: string;
}

export interface GoogleConfigResponse {
  clientId?: string;
}

export interface ForgotPasswordDto {
  email: string;
}

export interface ResetPasswordDto {
  token: string;
  newPassword: string;
}

export interface UpdateUserDto {
  name?: string;
  phone?: string;
}

export interface UpdateUserRoleDto {
  roleName: string;
}

export interface CreateRoleDto {
  roleName: string;
}

export interface AssignUserRoleDto {
  email: string;
  roleName: string;
}

export interface AuthResponse {
  accessToken?: string;
  refreshToken?: string;
  requiresOtp?: boolean;
  challengeId?: string;
  message?: string;
  cooldownSeconds?: number;
}

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  phone?: string;
  role: string;
}

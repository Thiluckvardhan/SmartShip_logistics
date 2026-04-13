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

export interface TokenDto {
  token: string;
}

export interface GoogleLoginDto {
  idToken: string;
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
  accessToken: string;
  refreshToken: string;
}

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  phone?: string;
  role: string;
}

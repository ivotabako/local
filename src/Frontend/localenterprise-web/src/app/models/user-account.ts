export const userRoles = ['Admin', 'Reader', 'Writer'] as const;

export type UserRole = (typeof userRoles)[number];

export interface UserAccount {
  id: string;
  username: string;
  roles: UserRole[];
  createdAt: string;
  createdBy: string | null;
  requiresPasswordChange: boolean;
  lastPasswordChangedAt: string | null;
  isLocked: boolean;
  twoFactorEnabled: boolean;
  recoveryCodesRemaining: number;
}

export interface CreateUserAccountRequest {
  username: string;
  password: string;
  roles: UserRole[];
}

export interface UpdateUserAccountRequest {
  roles: UserRole[];
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ResetUserPasswordRequest {
  newPassword: string;
}

export interface TwoFactorEnrollment {
  sharedSecret: string;
  provisioningUri: string;
  twoFactorEnabled: boolean;
}

export interface TwoFactorVerificationResult {
  user: UserAccount;
  recoveryCodes: string[];
}
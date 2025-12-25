export interface PasswordReset {
  ID: number,
  Key: string,
  Expiry: Date,
  UserId: number,
}

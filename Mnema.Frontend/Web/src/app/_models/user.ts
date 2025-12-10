
export enum Role {
  ManagePages          = "ManagePages",
  ManageUsers          = "ManageUsers",
  ManageServerConfigs  = "ManageSettings",
  ManagePreferences    = "ManagePreferences",
  Subscriptions        = "Subscriptions",
}

export const AllRoles = [
  Role.ManagePages, Role.ManageUsers, Role.ManageServerConfigs, Role.ManagePreferences, Role.Subscriptions
]

export interface UserDto {
  id: number;
  name: string;
  email: string;
  roles: Role[];
  pages: number[];
  canDelete: boolean;
}

export interface User {
  id: number;
  name: string;
  email: string;
  oidcToken?: string;
  token: string;
  apiKey: string;
  roles: Role[];
}

export function hasRole(user: User, role: Role) {
  return user.roles.includes(role);
}


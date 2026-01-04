
export enum Role {
  ManagePages                 = "ManagePages",
  ManageServerConfigs         = "ManageSettings",
  Subscriptions               = "Subscriptions",
  ManageExternalConnections   = "ManageExternalConnections",
}

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


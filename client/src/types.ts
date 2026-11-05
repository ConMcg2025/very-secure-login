export type Role = "User" | "SuperUser" | "Admin";

export interface Me {
  id: number;
  username: string;
  role: Role;
  manageableRoles: Role[];
}

export interface UserRow {
  id: number;
  username: string;
  role: Role;
  createdAt: string;
}

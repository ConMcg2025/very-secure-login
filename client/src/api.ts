import type { Me, Role, UserRow } from "./types";

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
  }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const res = await fetch(path, {
    method,
    credentials: "same-origin",
    headers: body === undefined ? undefined : { "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (!res.ok) {
    let message = `Request failed (${res.status})`;
    try {
      const problem = await res.json();
      message = problem.detail ?? problem.title ?? message;
      // ASP.NET model-validation errors: { errors: { Field: ["msg"] } }
      if (problem.errors) message = Object.values<string[]>(problem.errors).flat().join(" ");
    } catch {
      if (res.status === 429) message = "Too many attempts. Please wait a minute and try again.";
    }
    throw new ApiError(res.status, message);
  }

  return res.status === 204 ? (undefined as T) : ((await res.json()) as T);
}

export const api = {
  me: () => request<Me>("GET", "/api/auth/me"),
  login: (username: string, password: string) => request<Me>("POST", "/api/auth/login", { username, password }),
  logout: () => request<void>("POST", "/api/auth/logout"),

  listUsers: () => request<UserRow[]>("GET", "/api/users"),
  createUser: (data: { username: string; password: string; role: Role }) =>
    request<UserRow>("POST", "/api/users", data),
  updateUser: (id: number, data: { username?: string; password?: string; role?: Role }) =>
    request<UserRow>("PUT", `/api/users/${id}`, data),
  deleteUser: (id: number) => request<void>("DELETE", `/api/users/${id}`),
};

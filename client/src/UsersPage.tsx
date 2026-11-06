import { useCallback, useEffect, useState } from "react";
import { api, ApiError } from "./api";
import type { Me, UserRow } from "./types";
import { UserDialog } from "./UserDialog";

interface Props {
  me: Me;
  onLogout: () => void;
  onSessionExpired: () => void;
}

export function UsersPage({ me, onLogout, onSessionExpired }: Props) {
  const [users, setUsers] = useState<UserRow[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  
  const [dialog, setDialog] = useState<UserRow | null | undefined>(undefined);

  const handleError = useCallback(
    (err: unknown) => {
      if (err instanceof ApiError && err.status === 401) return onSessionExpired();
      setError(err instanceof Error ? err.message : "Something went wrong");
    },
    [onSessionExpired],
  );

  const load = useCallback(async () => {
    try {
      setUsers(await api.listUsers());
      setError(null);
    } catch (err) {
      handleError(err);
    }
  }, [handleError]);

  useEffect(() => {
    load();
  }, [load]);

  async function remove(user: UserRow) {
    if (!confirm(`Delete ${user.username}? This cannot be undone.`)) return;
    try {
      await api.deleteUser(user.id);
      await load();
    } catch (err) {
      handleError(err);
    }
  }

  const canManage = (u: UserRow) => me.manageableRoles.includes(u.role);
  const canCreate = me.manageableRoles.length > 0;

  return (
    <main className="app-shell">
      <header className="app-header">
        <div className="brand-lockup compact">
          <span className="brand-name">Kvist <span>&amp;</span> Bjælke</span>
        </div>
        <div className="account-area">
          <div className="welcome-copy">
            <span className="account-label">Welcome back</span>
            <strong>{me.username}</strong>
          </div>
          <span className={`badge ${me.role}`}>{me.role}</span>
          <button className="signout-button" onClick={onLogout}>Sign out</button>
        </div>
      </header>

      <section className="users-content">
        <header className="topbar">
          <div>
            <span className="eyebrow">WORKSPACE / ACCESS</span>
            <h1>User management</h1>
            <p className="muted page-intro">Manage accounts and access levels for your organisation.</p>
          </div>
        <div className="actions">
          {canCreate && (
            <button className="primary" onClick={() => setDialog(null)}>
              Add user
            </button>
          )}
        </div>
      </header>

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}

      {users === null ? (
        <p className="muted">Loading…</p>
      ) : (
        <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Account</th>
              <th>Role</th>
              <th>Created</th>
              {canCreate && <th aria-label="Actions" />}
            </tr>
          </thead>
          <tbody>
            {users.map(u => (
              <tr key={u.id}>
                <td>
                  <span className="user-name">{u.username}</span>
                  {u.id === me.id && <span className="you-label">YOU</span>}
                </td>
                <td>
                  <span className={`badge ${u.role}`}>{u.role}</span>
                </td>
                <td>{new Date(u.createdAt).toLocaleString()}</td>
                {canCreate && (
                  <td className="row-actions">
                    {canManage(u) && (
                      <>
                        <button onClick={() => setDialog(u)}>Edit</button>
                        <button className="danger" onClick={() => remove(u)} disabled={u.id === me.id}>
                          Delete
                        </button>
                      </>
                    )}
                  </td>
                )}
              </tr>
            ))}
            {users.length === 0 && (
              <tr>
                <td colSpan={canCreate ? 4 : 3} className="muted">
                  No users.
                </td>
              </tr>
            )}
          </tbody>
        </table>
        </div>
      )}

      {!canCreate && <p className="muted">Your role has read-only access.</p>}

      {dialog !== undefined && (
        <UserDialog
          me={me}
          user={dialog}
          onClose={() => setDialog(undefined)}
          onSaved={() => {
            setDialog(undefined);
            load();
          }}
        />
      )}
      </section>
    </main>
  );
}

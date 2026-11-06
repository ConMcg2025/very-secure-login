import { useEffect, useRef, useState, type FormEvent } from "react";
import { api } from "./api";
import type { Me, Role, UserRow } from "./types";

interface Props {
  me: Me;
  user: UserRow | null;
  onClose: () => void;
  onSaved: () => void;
}

export function UserDialog({ me, user, onClose, onSaved }: Props) {
  const ref = useRef<HTMLDialogElement>(null);
  const isEdit = user !== null;
  const isSelf = user?.id === me.id;

  const [username, setUsername] = useState(user?.username ?? "");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<Role>(user?.role ?? me.manageableRoles[0] ?? "User");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    ref.current?.showModal();
  }, []);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      if (isEdit) {
        await api.updateUser(user.id, {
          username,
          password: password || undefined,
          role: isSelf ? undefined : role,
        });
      } else {
        await api.createUser({ username, password, role });
      }
      onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong");
    } finally {
      setBusy(false);
    }
  }

  return (
    <dialog ref={ref} onClose={onClose} onCancel={onClose}>
      <form onSubmit={submit}>
        <div className="dialog-heading">
          <span className="eyebrow">{isEdit ? "ACCOUNT SETTINGS" : "NEW ACCOUNT"}</span>
          <h2>{isEdit ? `Edit ${user.username}` : "Add a user"}</h2>
          <p className="muted">{isEdit ? "Update account details and permissions." : "Create an account for a colleague."}</p>
        </div>

        <label>
          Username
          <input
            value={username}
            onChange={e => setUsername(e.target.value)}
            required
            minLength={3}
            maxLength={32}
            pattern="[A-Za-z0-9_.\-]+"
            title="Letters, digits, '.', '_' and '-'"
            autoComplete="off"
            autoFocus
          />
        </label>

        <label>
          {isEdit ? "New password (leave empty to keep)" : "Password"}
          <input
            type="password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            required={!isEdit}
            minLength={3}
            maxLength={128}
            autoComplete="new-password"
          />
        </label>

        <label>
          Role
          <select value={role} onChange={e => setRole(e.target.value as Role)} disabled={isSelf}>
            {(isSelf && user ? [user.role] : me.manageableRoles).map(r => (
              <option key={r} value={r}>
                {r}
              </option>
            ))}
          </select>
        </label>

        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}

        <div className="actions dialog-actions">
          <button type="button" onClick={() => ref.current?.close()}>
            Cancel
          </button>
          <button type="submit" className="primary" disabled={busy}>
            {busy ? "Saving…" : "Save"}
          </button>
        </div>
      </form>
    </dialog>
  );
}

import { useState, type FormEvent } from "react";
import { api } from "./api";
import type { Me } from "./types";
import "./LoginForm.css";

const UserIcon = () => (
  <svg viewBox="0 0 24 24" aria-hidden="true" className="field-icon">
    <circle cx="12" cy="8" r="4" fill="currentColor" />
    <path d="M4 21c0-4.4 3.6-7 8-7s8 2.6 8 7z" fill="currentColor" />
  </svg>
);

const LockIcon = () => (
  <svg viewBox="0 0 24 24" aria-hidden="true" className="field-icon">
    <rect x="5" y="10" width="14" height="11" rx="2" fill="currentColor" />
    <path d="M8 10V7a4 4 0 0 1 8 0v3" fill="none" stroke="currentColor" strokeWidth="2" />
  </svg>
);

export function LoginForm({ onLoggedIn }: { onLoggedIn: (me: Me) => void }) {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      onLoggedIn(await api.login(username, password));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed");
      setPassword("");
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="wood-login">
      <div className="wood-badge" aria-hidden="true">
        <svg viewBox="0 0 24 24">
          <circle cx="12" cy="8.5" r="3.6" fill="none" stroke="currentColor" strokeWidth="1.2" />
          <path d="M5 20c0-3.8 3.1-6 7-6s7 2.2 7 6z" fill="none" stroke="currentColor" strokeWidth="1.2" />
        </svg>
      </div>

     
      <div className="wood-sign-shadow">
        <div className="wood-sign">
          <h1 className="wood-title">Kvist &amp; Bjælke</h1>

          <form onSubmit={submit} className="wood-form">
            <label className="wood-field">
              <span className="sr-only">Username</span>
              <UserIcon />
              <input
                value={username}
                onChange={e => setUsername(e.target.value)}
                placeholder="USERNAME"
                autoComplete="username"
                autoFocus
                required
                maxLength={64}
              />
            </label>

            <label className="wood-field">
              <span className="sr-only">Password</span>
              <LockIcon />
              <input
                type="password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="PASSWORD"
                autoComplete="current-password"
                required
                maxLength={128}
              />
            </label>

            {error && (
              <p className="wood-error" role="alert">
                {error}
              </p>
            )}

            <button type="submit" className="wood-button" disabled={busy}>
              {busy ? "SIGNING IN…" : "LOGIN"}
            </button>
          </form>
        </div>
      </div>
    </main>
  );
}

import "./index.css";

import { useCallback, useEffect, useState } from "react";
import { api } from "./api";
import { LoginForm } from "./LoginForm";
import type { Me } from "./types";
import { UsersPage } from "./UsersPage";

export function App() {
  // undefined = still checking for an existing session, null = signed out
  const [me, setMe] = useState<Me | null | undefined>(undefined);

  useEffect(() => {
    api
      .me()
      .then(setMe)
      .catch(() => setMe(null));
  }, []);

  const logout = useCallback(async () => {
    try {
      await api.logout();
    } finally {
      setMe(null);
    }
  }, []);

  if (me === undefined) return <p className="muted">Loading…</p>;
  if (me === null) return <LoginForm onLoggedIn={setMe} />;
  return <UsersPage me={me} onLogout={logout} onSessionExpired={() => setMe(null)} />;
}

export default App;

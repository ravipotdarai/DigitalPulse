import { Button } from "@fluentui/react-components";
import { FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ApiError, api } from "../lib/api";
import { useSession } from "../state/session";
import { Field, Note } from "../design/Field";
import { PageState } from "../components/PageState";

export function RegisterPage() {
  const navigate = useNavigate();
  const applyAuth = useSession((s) => s.applyAuth);
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      applyAuth(await api.register({ displayName, email, password }));
      navigate("/onboarding", { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not register.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthSplit title="Open a pulse." stage="Your user comes first. Tenant, business, and plan follow.">
      <form onSubmit={onSubmit}>
        <Field label="Your name" value={displayName} onChange={setDisplayName} required />
        <Field label="Work email" value={email} onChange={setEmail} type="email" required />
        <Field label="Password" value={password} onChange={setPassword} type="password" required />
        <Note error={error} ok={false} />
        <Button appearance="primary" type="submit" disabled={busy}>{busy ? "Creating…" : "Create account"}</Button>
        <p style={{ color: "var(--muted)", fontSize: "0.88rem" }}>Already onboarded? <Link to="/login">Log in</Link></p>
      </form>
    </AuthSplit>
  );
}

export function LoginPage() {
  const navigate = useNavigate();
  const applyAuth = useSession((s) => s.applyAuth);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      applyAuth(await api.login({ email, password }));
      navigate("/onboarding", { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not sign in.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthSplit title="Welcome back." stage="This screen talks only to /v1/auth. Development tokens stay on the server.">
      <form onSubmit={onSubmit}>
        <Field label="Email" value={email} onChange={setEmail} type="email" required />
        <Field label="Password" value={password} onChange={setPassword} type="password" required />
        <Note error={error} ok={false} />
        <Button appearance="primary" type="submit" disabled={busy}>{busy ? "Checking…" : "Enter workspace"}</Button>
        <p style={{ color: "var(--muted)", fontSize: "0.88rem" }}>
          <Link to="/forgot">Forgot password</Link>
          {" · "}
          <Link to="/register">Create account</Link>
        </p>
      </form>
    </AuthSplit>
  );
}

export function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);

  return (
    <AuthSplit title="Reset stays locked." stage="Password reset is not issued in this development slice.">
      <form
        onSubmit={(event) => {
          event.preventDefault();
          if (email.trim()) setSent(true);
        }}
      >
        <Field label="Work email" value={email} onChange={setEmail} type="email" required />
        {sent ? (
          <PageState mode="info" title="Reset is not available" detail="Use the account you registered. Recovery will talk to /v1/auth when that endpoint exists." />
        ) : (
          <Button appearance="primary" type="submit">Check email</Button>
        )}
        <p style={{ color: "var(--muted)", fontSize: "0.88rem" }}><Link to="/login">Back to login</Link></p>
      </form>
    </AuthSplit>
  );
}

function AuthSplit({ title, stage, children }: { title: string; stage: string; children: React.ReactNode }) {
  return (
    <main className="auth-split">
      <section className="auth-stage">
        <p className="hero-kicker">Presence OS</p>
        <h2 className="display">{title}</h2>
        <p style={{ color: "var(--muted)", maxWidth: "22rem" }}>{stage}</p>
      </section>
      <section className="auth-panel">
        <h1 className="display" style={{ fontSize: "2rem", marginBottom: "1.2rem" }}>{title}</h1>
        {children}
      </section>
    </main>
  );
}

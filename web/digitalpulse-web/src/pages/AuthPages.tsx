import { Button } from "../design/Button";
import { FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ApiError, api } from "../lib/api";
import { useSession } from "../state/session";
import { Field, Note } from "../design/Field";
import { PageState } from "../components/PageState";
import { MOTION, useMotionTiming } from "../design/motion";
import { motion } from "framer-motion";

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
        <p className="auth-foot">Already onboarded? <Link to="/login">Log in</Link></p>
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
        <p className="auth-foot">
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
        <p className="auth-foot"><Link to="/login">Back to login</Link></p>
      </form>
    </AuthSplit>
  );
}

function AuthSplit({ title, stage, children }: { title: string; stage: string; children: React.ReactNode }) {
  const { reduce, enter, ease } = useMotionTiming();
  return (
    <main className="auth-split">
      <motion.section
        className="auth-stage"
        initial={reduce ? false : { opacity: 0, x: -MOTION.distance.lg }}
        animate={{ opacity: 1, x: 0 }}
        transition={{ duration: enter, ease }}
      >
        <p className="chapter-num">Access</p>
        <p className="hero-kicker">Presence OS</p>
        <h2 className="display">{title}</h2>
        <p className="ink-muted stage-copy">{stage}</p>
      </motion.section>
      <motion.section
        className="auth-panel"
        initial={reduce ? false : { opacity: 0, y: MOTION.distance.md }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: enter, delay: reduce ? 0 : 0.08, ease }}
      >
        <h1 className="display display-page">{title}</h1>
        <p className="ink-muted auth-stage-mobile">{stage}</p>
        {children}
      </motion.section>
    </main>
  );
}

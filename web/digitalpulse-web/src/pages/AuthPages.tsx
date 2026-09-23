import { Button, Input, Label } from "@fluentui/react-components";
import { FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ApiError, api } from "../lib/api";
import { useSession } from "../state/session";
import { PageState } from "../components/PageState";

export function RegisterPage() {
  const navigate = useNavigate();
  const applyAuth = useSession((s) => s.applyAuth);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setError(null);
    try {
      const auth = await api.register({
        displayName: String(form.get("displayName") ?? ""),
        email: String(form.get("email") ?? ""),
        password: String(form.get("password") ?? "")
      });
      applyAuth(auth);
      navigate("/onboarding", { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not register.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthCard title="Open a pulse." subtitle="Your user comes first. Tenant, business, and plan follow.">
      <form className="grid gap-4" onSubmit={onSubmit}>
        <Field name="displayName" label="Your name" />
        <Field name="email" label="Work email" type="email" />
        <Field name="password" label="Password" type="password" />
        {error ? <PageState mode="error" title="Registration failed" detail={error} /> : null}
        <Button appearance="primary" type="submit" disabled={busy}>{busy ? "Creating…" : "Create account"}</Button>
        <p className="text-sm" style={{ color: "var(--muted)" }}>Already onboarded? <Link to="/login">Log in</Link></p>
      </form>
    </AuthCard>
  );
}

export function LoginPage() {
  const navigate = useNavigate();
  const applyAuth = useSession((s) => s.applyAuth);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setError(null);
    try {
      const auth = await api.login({
        email: String(form.get("email") ?? ""),
        password: String(form.get("password") ?? "")
      });
      applyAuth(auth);
      navigate("/onboarding", { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not sign in.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthCard title="Welcome back." subtitle="Development authentication sits behind the API. This screen only talks to /v1/auth.">
      <form className="grid gap-4" onSubmit={onSubmit}>
        <Field name="email" label="Email" type="email" />
        <Field name="password" label="Password" type="password" />
        {error ? <PageState mode="error" title="Sign-in failed" detail={error} /> : null}
        <Button appearance="primary" type="submit" disabled={busy}>{busy ? "Checking…" : "Enter workspace"}</Button>
      </form>
    </AuthCard>
  );
}

function AuthCard({ title, subtitle, children }: { title: string; subtitle: string; children: React.ReactNode }) {
  return (
    <main className="mx-auto grid min-h-[80dvh] max-w-md content-center px-5 py-10">
      <div className="rounded-3xl border p-6 md:p-8" style={{ background: "var(--card)", borderColor: "var(--stroke)" }}>
        <h1 className="display text-4xl">{title}</h1>
        <p className="mb-6 mt-2 text-sm" style={{ color: "var(--muted)" }}>{subtitle}</p>
        {children}
      </div>
    </main>
  );
}

function Field({ name, label, type = "text" }: { name: string; label: string; type?: "text" | "email" | "password" }) {
  return (
    <label className="grid gap-2">
      <Label>{label}</Label>
      <Input
        name={name}
        type={type}
        required
        minLength={type === "password" ? 8 : undefined}
        autoComplete={type === "password" ? "current-password" : type === "email" ? "email" : "name"}
      />
    </label>
  );
}

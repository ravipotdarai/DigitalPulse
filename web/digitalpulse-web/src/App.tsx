import { FluentProvider } from "@fluentui/react-components";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AnimatePresence, motion } from "framer-motion";
import { useEffect, useMemo, useState } from "react";
import { BrowserRouter, Navigate, Route, Routes, useLocation } from "react-router-dom";
import { AppShell } from "./components/AppShell";
import { getStoredToken } from "./lib/api";
import { LandingPage } from "./pages/LandingPage";
import { LoginPage, RegisterPage } from "./pages/AuthPages";
import { DashboardPage } from "./pages/DashboardPage";
import { OnboardingRedirect } from "./pages/OnboardingRedirect";
import { WorkspacePage } from "./pages/WorkspacePage";
import { BusinessPage } from "./pages/onboarding/BusinessPage";
import { CreateTenantPage } from "./pages/onboarding/CreateTenantPage";
import { LocationPage } from "./pages/onboarding/LocationPage";
import { PlanPage } from "./pages/onboarding/PlanPage";
import { TenantReviewPage } from "./pages/onboarding/TenantReviewPage";
import { useSession } from "./state/session";
import { darkTheme, lightTheme } from "./theme/pulseTheme";

const queryClient = new QueryClient();

function RequireAuth({ children }: { children: React.ReactNode }) {
  if (!getStoredToken()) return <Navigate to="/login" replace />;
  return children;
}

function GuestOnly({ children }: { children: React.ReactNode }) {
  if (getStoredToken()) return <Navigate to="/onboarding" replace />;
  return children;
}

function AnimatedRoutes() {
  const location = useLocation();
  return (
    <AnimatePresence mode="wait">
      <motion.div key={location.pathname} initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -8 }} transition={{ duration: 0.28 }}>
        <Routes location={location}>
          <Route path="/" element={<GuestOnly><LandingPage /></GuestOnly>} />
          <Route path="/register" element={<GuestOnly><RegisterPage /></GuestOnly>} />
          <Route path="/login" element={<GuestOnly><LoginPage /></GuestOnly>} />
          <Route path="/onboarding" element={<RequireAuth><OnboardingRedirect /></RequireAuth>} />
          <Route path="/onboarding/tenant" element={<RequireAuth><CreateTenantPage /></RequireAuth>} />
          <Route path="/onboarding/tenant-review" element={<RequireAuth><TenantReviewPage /></RequireAuth>} />
          <Route path="/onboarding/business" element={<RequireAuth><BusinessPage /></RequireAuth>} />
          <Route path="/onboarding/location" element={<RequireAuth><LocationPage /></RequireAuth>} />
          <Route path="/onboarding/plan" element={<RequireAuth><PlanPage /></RequireAuth>} />
          <Route path="/app" element={<RequireAuth><DashboardPage /></RequireAuth>} />
          <Route path="/app/info" element={<RequireAuth><WorkspacePage /></RequireAuth>} />
        </Routes>
      </motion.div>
    </AnimatePresence>
  );
}

export function App() {
  const [dark, setDark] = useState(() => window.matchMedia("(prefers-color-scheme: dark)").matches);
  const hydrate = useSession((s) => s.hydrate);
  const theme = useMemo(() => (dark ? darkTheme : lightTheme), [dark]);

  useEffect(() => {
    document.documentElement.classList.toggle("dark", dark);
  }, [dark]);

  useEffect(() => {
    void hydrate();
  }, [hydrate]);

  return (
    <FluentProvider theme={theme} style={{ minHeight: "100dvh", background: "transparent" }}>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <div className="grain" />
          <AppShell dark={dark} onToggleTheme={() => setDark((v) => !v)}>
            <AnimatedRoutes />
          </AppShell>
        </BrowserRouter>
      </QueryClientProvider>
    </FluentProvider>
  );
}

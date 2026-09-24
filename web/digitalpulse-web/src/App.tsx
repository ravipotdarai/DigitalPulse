import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AnimatePresence, motion, useReducedMotion } from "framer-motion";
import { useEffect } from "react";
import { BrowserRouter, Navigate, Route, Routes, useLocation } from "react-router-dom";
import { AppShell } from "./components/AppShell";
import { PublicChrome } from "./components/PublicChrome";
import { getStoredToken } from "./lib/api";
import { LandingPage } from "./pages/LandingPage";
import { ForgotPasswordPage, LoginPage, RegisterPage } from "./pages/AuthPages";
import { DashboardPage } from "./pages/DashboardPage";
import { OnboardingRedirect } from "./pages/OnboardingRedirect";
import { WorkspacePage } from "./pages/WorkspacePage";
import { BusinessIdentityPage } from "./pages/BusinessIdentityPage";
import { IdentityRedirect } from "./pages/IdentityRedirect";
import { ConnectionsPage } from "./pages/ConnectionsPage";
import { FindingsPage } from "./pages/FindingsPage";
import { WebsitePage } from "./pages/WebsitePage";
import { SocialPage } from "./pages/SocialPage";
import { DirectoriesPage } from "./pages/DirectoriesPage";
import { ProjectsPage } from "./pages/ProjectsPage";
import { BusinessPage } from "./pages/onboarding/BusinessPage";
import { CreateTenantPage } from "./pages/onboarding/CreateTenantPage";
import { LocationPage } from "./pages/onboarding/LocationPage";
import { PlanPage } from "./pages/onboarding/PlanPage";
import { TenantReviewPage } from "./pages/onboarding/TenantReviewPage";
import { useSession } from "./state/session";
import { ThemeProvider, useTheme } from "./theme";

const queryClient = new QueryClient();

function RequireAuth({ children }: { children: React.ReactNode }) {
  if (!getStoredToken()) return <Navigate to="/login" replace />;
  return children;
}

function GuestOnly({ children }: { children: React.ReactNode }) {
  if (getStoredToken()) return <Navigate to="/onboarding" replace />;
  return children;
}

function Frame({ children }: { children: React.ReactNode }) {
  const { pathname } = useLocation();
  if (pathname.startsWith("/app")) {
    return <AppShell>{children}</AppShell>;
  }
  return <PublicChrome>{children}</PublicChrome>;
}

function AnimatedRoutes() {
  const location = useLocation();
  const reduce = useReducedMotion();
  const { tokens } = useTheme();
  const duration = reduce ? 0 : Number.parseFloat(tokens.motion.duration) / 1000;

  return (
    <AnimatePresence mode="wait">
      <motion.div
        key={location.pathname}
        initial={reduce ? false : { opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={reduce ? undefined : { opacity: 0 }}
        transition={{ duration }}
      >
        <Routes location={location}>
          <Route path="/" element={<GuestOnly><LandingPage /></GuestOnly>} />
          <Route path="/register" element={<GuestOnly><RegisterPage /></GuestOnly>} />
          <Route path="/login" element={<GuestOnly><LoginPage /></GuestOnly>} />
          <Route path="/forgot" element={<GuestOnly><ForgotPasswordPage /></GuestOnly>} />
          <Route path="/onboarding" element={<RequireAuth><OnboardingRedirect /></RequireAuth>} />
          <Route path="/onboarding/tenant" element={<RequireAuth><CreateTenantPage /></RequireAuth>} />
          <Route path="/onboarding/tenant-review" element={<RequireAuth><TenantReviewPage /></RequireAuth>} />
          <Route path="/onboarding/business" element={<RequireAuth><BusinessPage /></RequireAuth>} />
          <Route path="/onboarding/location" element={<RequireAuth><LocationPage /></RequireAuth>} />
          <Route path="/onboarding/plan" element={<RequireAuth><PlanPage /></RequireAuth>} />
          <Route path="/app" element={<RequireAuth><DashboardPage /></RequireAuth>} />
          <Route path="/app/info" element={<RequireAuth><WorkspacePage /></RequireAuth>} />
          <Route path="/app/identity" element={<RequireAuth><IdentityRedirect /></RequireAuth>} />
          <Route path="/app/connections" element={<RequireAuth><ConnectionsPage /></RequireAuth>} />
          <Route path="/app/findings" element={<RequireAuth><FindingsPage /></RequireAuth>} />
          <Route path="/app/website" element={<RequireAuth><WebsitePage /></RequireAuth>} />
          <Route path="/app/social" element={<RequireAuth><SocialPage /></RequireAuth>} />
          <Route path="/app/directories" element={<RequireAuth><DirectoriesPage /></RequireAuth>} />
          <Route path="/app/projects" element={<RequireAuth><ProjectsPage /></RequireAuth>} />
          <Route path="/app/businesses/:businessId" element={<RequireAuth><BusinessIdentityPage /></RequireAuth>} />
        </Routes>
      </motion.div>
    </AnimatePresence>
  );
}

function ThemedShell() {
  const userId = useSession((state) => state.profile?.userId);

  return (
    <ThemeProvider userId={userId}>
      <div className="grain" aria-hidden="true" />
      <Frame>
        <AnimatedRoutes />
      </Frame>
    </ThemeProvider>
  );
}

export function App() {
  const hydrate = useSession((state) => state.hydrate);

  useEffect(() => {
    void hydrate();
  }, [hydrate]);

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <ThemedShell />
      </BrowserRouter>
    </QueryClientProvider>
  );
}

import { useQuery } from "@tanstack/react-query";
import { Navigate } from "react-router-dom";
import { PageState } from "../components/PageState";
import { api, getStoredToken } from "../lib/api";

const stepRoute: Record<string, string> = {
  tenant: "/onboarding/tenant",
  business: "/onboarding/business",
  location: "/onboarding/location",
  plan: "/onboarding/plan",
  dashboard: "/app"
};

export function OnboardingRedirect() {
  const token = getStoredToken();
  const query = useQuery({
    queryKey: ["onboarding"],
    queryFn: api.onboarding,
    enabled: Boolean(token)
  });

  if (!token) return <Navigate to="/login" replace />;
  if (query.isLoading) return <PageState mode="loading" title="Reading your workspace" />;
  if (query.isError) return <PageState mode="error" title="Could not load onboarding" detail="Try signing in again." />;
  return <Navigate to={stepRoute[query.data?.nextStep ?? "tenant"]} replace />;
}

import { useQuery } from "@tanstack/react-query";
import { Navigate } from "react-router-dom";
import { api } from "../lib/api";
import { PageState } from "../components/PageState";

export function IdentityRedirect() {
  const query = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  if (query.isLoading) return <PageState mode="loading" title="Opening identity" />;
  if (query.isError) return <PageState mode="error" title="Identity unavailable" />;
  const first = query.data?.[0];
  if (!first) return <PageState mode="empty" title="No business yet" detail="Finish onboarding to create the first business record." />;
  return <Navigate to={`/app/businesses/${first.id}`} replace />;
}

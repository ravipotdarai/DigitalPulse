import { Button } from "@fluentui/react-components";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { api } from "../../lib/api";
import { PageState } from "../../components/PageState";
import { Wizard } from "./CreateTenantPage";

export function TenantReviewPage() {
  const navigate = useNavigate();
  const query = useQuery({ queryKey: ["tenant"], queryFn: api.currentTenant });

  if (query.isLoading) return <PageState mode="loading" title="Loading tenant" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Tenant not found" />;

  return (
    <Wizard title="Tenant ready" step="2 / 5">
      <dl className="grid gap-3 text-sm">
        <div><dt style={{ color: "var(--muted)" }}>Name</dt><dd className="text-lg">{query.data.name}</dd></div>
        <div><dt style={{ color: "var(--muted)" }}>Type</dt><dd>{query.data.type}</dd></div>
      </dl>
      <Button className="mt-6" appearance="primary" onClick={() => navigate("/onboarding/business")}>Add business</Button>
    </Wizard>
  );
}

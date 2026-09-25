export function tenantTypeLabel(type: string | null | undefined): string {
  if (!type) return "";
  return type === "Agency" ? "Company" : type;
}

export function isCompanyTenant(type: string | null | undefined): boolean {
  return type === "Agency";
}

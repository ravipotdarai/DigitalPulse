export type AuthResponse = {
  accessToken: string;
  userId: string;
  email: string;
  displayName: string;
  tenantId: string | null;
  tenantName: string | null;
  tenantType: string | null;
};

export type MeResponse = {
  userId: string;
  email: string;
  displayName: string;
  tenantId: string | null;
  tenantName: string | null;
  tenantType: string | null;
};

export type TenantResponse = { id: string; name: string; type: string };
export type BusinessResponse = { id: string; tenantId: string; name: string; website: string | null };
export type LocationResponse = {
  id: string;
  businessId: string;
  tenantId: string;
  name: string;
  addressLine: string | null;
  city: string | null;
  region: string | null;
  postalCode: string | null;
  countryCode: string;
};
export type PlanResponse = {
  id: string;
  code: string;
  name: string;
  monthlyPriceInr: number;
  maxBusinesses: number;
  agencyOnly: boolean;
};
export type OnboardingStatus = {
  hasTenant: boolean;
  hasBusiness: boolean;
  hasLocation: boolean;
  hasSubscription: boolean;
  nextStep: string;
};
export type DashboardResponse = {
  tenantId: string;
  tenantName: string;
  tenantType: string;
  planName: string;
  maxBusinesses: number;
  businessCount: number;
  locationCount: number;
  businesses: { id: string; name: string; website: string | null; locationCount: number }[];
};

export class ApiError extends Error {
  constructor(public status: number, public title: string) {
    super(title);
  }
}

const TOKEN_KEY = "dp.session";

export function getStoredToken() {
  return sessionStorage.getItem(TOKEN_KEY);
}

export function storeToken(token: string | null) {
  if (token) sessionStorage.setItem(TOKEN_KEY, token);
  else sessionStorage.removeItem(TOKEN_KEY);
}

const PUBLIC_PATHS = new Set(["/v1/auth/login", "/v1/auth/register"]);

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getStoredToken();
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");
  if (init.body) headers.set("Content-Type", "application/json");
  if (token && !PUBLIC_PATHS.has(path)) headers.set("Authorization", `Bearer ${token}`);

  let response: Response;
  try {
    response = await fetch(path, { ...init, headers });
  } catch {
    throw new ApiError(0, "Cannot reach the API. Start DigitalPulse.Api on http://localhost:5088, then retry.");
  }

  if (response.status === 204) return undefined as T;

  const contentType = response.headers.get("content-type") ?? "";
  const payload = contentType.includes("json") ? await response.json().catch(() => ({})) : {};
  if (!response.ok) {
    const title =
      (typeof payload === "object" && payload && "title" in payload && typeof payload.title === "string" && payload.title) ||
      (typeof payload === "object" && payload && "detail" in payload && typeof payload.detail === "string" && payload.detail) ||
      (response.status === 502 || response.status === 503 || response.status === 504 || response.status >= 500
        ? "Cannot reach the API. Confirm DigitalPulse.Api is running on http://localhost:5088."
        : "Request failed");
    throw new ApiError(response.status, title);
  }
  return payload as T;
}

export const api = {
  register: (body: { email: string; displayName: string; password: string }) =>
    request<AuthResponse>("/v1/auth/register", { method: "POST", body: JSON.stringify(body) }),
  login: (body: { email: string; password: string }) =>
    request<AuthResponse>("/v1/auth/login", { method: "POST", body: JSON.stringify(body) }),
  me: () => request<MeResponse>("/v1/auth/me"),
  logout: () => request<void>("/v1/auth/logout", { method: "POST" }),
  createTenant: (body: { name: string; type: string }) =>
    request<AuthResponse>("/v1/tenants", { method: "POST", body: JSON.stringify(body) }),
  currentTenant: () => request<TenantResponse>("/v1/tenants/current"),
  createBusiness: (body: { name: string; website?: string }) =>
    request<BusinessResponse>("/v1/businesses", { method: "POST", body: JSON.stringify(body) }),
  listBusinesses: () => request<BusinessResponse[]>("/v1/businesses"),
  createLocation: (businessId: string, body: Record<string, string>) =>
    request<LocationResponse>(`/v1/businesses/${businessId}/locations`, { method: "POST", body: JSON.stringify(body) }),
  plans: () => request<PlanResponse[]>("/v1/plans"),
  selectPlan: (planCode: string) =>
    request("/v1/subscriptions", { method: "POST", body: JSON.stringify({ planCode }) }),
  onboarding: () => request<OnboardingStatus>("/v1/onboarding"),
  dashboard: () => request<DashboardResponse>("/v1/dashboard"),
  updateProfile: (displayName: string) =>
    request<MeResponse>("/v1/auth/me", { method: "PUT", body: JSON.stringify({ displayName }) }),
  updateTenant: (name: string) =>
    request<TenantResponse>("/v1/tenants/current", { method: "PUT", body: JSON.stringify({ name }) }),
  updateBusiness: (businessId: string, body: { name: string; website?: string }) =>
    request<BusinessResponse>(`/v1/businesses/${businessId}`, { method: "PUT", body: JSON.stringify(body) }),
  listLocations: (businessId: string) =>
    request<LocationResponse[]>(`/v1/businesses/${businessId}/locations`),
  updateLocation: (businessId: string, locationId: string, body: Record<string, string>) =>
    request<LocationResponse>(`/v1/businesses/${businessId}/locations/${locationId}`, {
      method: "PUT",
      body: JSON.stringify(body)
    })
};

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
export type BusinessResponse = {
  id: string;
  tenantId: string;
  name: string;
  website: string | null;
  foundedYear?: number | null;
  brandVoice?: string | null;
  industryCode?: string | null;
};
export type CatalogItem = { code: string; name: string };
export type ContactPoint = { id: string; businessId: string; kind: string; value: string; label: string | null };
export type NamedItem = { id: string; name: string; description?: string | null };
export type BusinessBrand = { id: string; brandId: string; name: string };
export type BusinessFact = { id: string; factTypeCode: string; value: string; status: string; canPublish: boolean };
export type CustomerContact = { id: string; kind: string; value: string };
export type Customer = { id: string; displayName: string; notes: string | null; contacts: CustomerContact[] };
export type BusinessIdentity = {
  business: BusinessResponse;
  industries: CatalogItem[];
  factTypes: CatalogItem[];
  contacts: ContactPoint[];
  categories: NamedItem[];
  services: NamedItem[];
  brands: BusinessBrand[];
  facts: BusinessFact[];
  customers: Customer[];
};
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
export type DashboardFinding = {
  id: string;
  businessId: string;
  severity: string;
  category: string;
  title: string;
  status: string;
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
  lastScanAtUtc: string | null;
  openFindingCount: number;
  highFindingCount: number;
  topFindings: DashboardFinding[];
  lastWebsiteAtUtc: string | null;
  websiteObservationCount: number;
  searchProvider: string;
  socialDraftCount: number;
  socialBlockedCount: number;
  directoryOpenCount: number;
  directoryVerifiedCount: number;
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
    }),
  identity: (businessId: string) => request<BusinessIdentity>(`/v1/businesses/${businessId}/identity`),
  updateBusinessProfile: (
    businessId: string,
    body: { name: string; website?: string; foundedYear?: number | null; brandVoice?: string; industryCode?: string }
  ) => request<BusinessResponse>(`/v1/businesses/${businessId}/profile`, { method: "PUT", body: JSON.stringify(body) }),
  addContact: (businessId: string, body: { kind: string; value: string; label?: string }) =>
    request<ContactPoint>(`/v1/businesses/${businessId}/contacts`, { method: "POST", body: JSON.stringify(body) }),
  updateContact: (businessId: string, contactId: string, body: { kind: string; value: string; label?: string }) =>
    request<ContactPoint>(`/v1/businesses/${businessId}/contacts/${contactId}`, { method: "PUT", body: JSON.stringify(body) }),
  deleteContact: (businessId: string, contactId: string) =>
    request<void>(`/v1/businesses/${businessId}/contacts/${contactId}`, { method: "DELETE" }),
  addCategory: (businessId: string, name: string) =>
    request<NamedItem>(`/v1/businesses/${businessId}/categories`, { method: "POST", body: JSON.stringify({ name }) }),
  updateCategory: (businessId: string, categoryId: string, name: string) =>
    request<NamedItem>(`/v1/businesses/${businessId}/categories/${categoryId}`, { method: "PUT", body: JSON.stringify({ name }) }),
  deleteCategory: (businessId: string, categoryId: string) =>
    request<void>(`/v1/businesses/${businessId}/categories/${categoryId}`, { method: "DELETE" }),
  addService: (businessId: string, body: { name: string; description?: string }) =>
    request<NamedItem>(`/v1/businesses/${businessId}/services`, { method: "POST", body: JSON.stringify(body) }),
  updateService: (businessId: string, serviceId: string, body: { name: string; description?: string }) =>
    request<NamedItem>(`/v1/businesses/${businessId}/services/${serviceId}`, { method: "PUT", body: JSON.stringify(body) }),
  deleteService: (businessId: string, serviceId: string) =>
    request<void>(`/v1/businesses/${businessId}/services/${serviceId}`, { method: "DELETE" }),
  addBrand: (businessId: string, name: string) =>
    request<BusinessBrand>(`/v1/businesses/${businessId}/brands`, { method: "POST", body: JSON.stringify({ name }) }),
  deleteBrand: (businessId: string, linkId: string) =>
    request<void>(`/v1/businesses/${businessId}/brands/${linkId}`, { method: "DELETE" }),
  addFact: (businessId: string, body: { factTypeCode: string; value: string; status: string }) =>
    request<BusinessFact>(`/v1/businesses/${businessId}/facts`, { method: "POST", body: JSON.stringify(body) }),
  updateFact: (businessId: string, factId: string, body: { factTypeCode: string; value: string; status: string }) =>
    request<BusinessFact>(`/v1/businesses/${businessId}/facts/${factId}`, { method: "PUT", body: JSON.stringify(body) }),
  deleteFact: (businessId: string, factId: string) =>
    request<void>(`/v1/businesses/${businessId}/facts/${factId}`, { method: "DELETE" }),
  addCustomer: (businessId: string, body: { displayName: string; mobile: string; email?: string; notes?: string }) =>
    request<Customer>(`/v1/businesses/${businessId}/customers`, { method: "POST", body: JSON.stringify(body) }),
  updateCustomer: (businessId: string, customerId: string, body: { displayName: string; mobile: string; email?: string; notes?: string }) =>
    request<Customer>(`/v1/businesses/${businessId}/customers/${customerId}`, { method: "PUT", body: JSON.stringify(body) }),
  deleteCustomer: (businessId: string, customerId: string) =>
    request<void>(`/v1/businesses/${businessId}/customers/${customerId}`, { method: "DELETE" }),
  connections: (businessId: string) =>
    request<ConnectionCenter>(`/v1/businesses/${businessId}/connections`),
  startConnection: (businessId: string, platformCode: string) =>
    request<StartConnection>(`/v1/businesses/${businessId}/connections`, { method: "POST", body: JSON.stringify({ platformCode }) }),
  completeConnection: (businessId: string, connectionId: string, code = "development") =>
    request<PlatformConnection>(`/v1/businesses/${businessId}/connections/${connectionId}/complete`, { method: "POST", body: JSON.stringify({ code }) }),
  healthConnection: (businessId: string, connectionId: string) =>
    request<PlatformConnection>(`/v1/businesses/${businessId}/connections/${connectionId}/health`, { method: "POST" }),
  diagnoseConnection: (businessId: string, connectionId: string) =>
    request<ConnectionDiagnostic[]>(`/v1/businesses/${businessId}/connections/${connectionId}/diagnose`, { method: "POST" }),
  reauthorizeConnection: (businessId: string, connectionId: string) =>
    request<StartConnection>(`/v1/businesses/${businessId}/connections/${connectionId}/reauthorize`, { method: "POST" }),
  disconnectConnection: (businessId: string, connectionId: string) =>
    request<void>(`/v1/businesses/${businessId}/connections/${connectionId}`, { method: "DELETE" }),
  scans: (businessId: string) => request<ScanCenter>(`/v1/businesses/${businessId}/scans`),
  runScan: (businessId: string) =>
    request<ScanDetail>(`/v1/businesses/${businessId}/scans`, { method: "POST" }),
  scan: (businessId: string, scanId: string) =>
    request<ScanDetail>(`/v1/businesses/${businessId}/scans/${scanId}`),
  updateFinding: (businessId: string, findingId: string, status: string) =>
    request<Finding>(`/v1/businesses/${businessId}/findings/${findingId}`, {
      method: "PATCH",
      body: JSON.stringify({ status })
    }),
  website: (businessId: string) => request<WebsiteIntelligence>(`/v1/businesses/${businessId}/website`),
  analyzeWebsite: (businessId: string) =>
    request<WebsiteIntelligence>(`/v1/businesses/${businessId}/website/analyze`, { method: "POST" }),
  searchWebsite: (businessId: string, query: string) =>
    request<SiteSearch>(`/v1/businesses/${businessId}/website/search?q=${encodeURIComponent(query)}`),
  social: (businessId: string) => request<SocialWorkspace>(`/v1/businesses/${businessId}/social`),
  createSocial: (businessId: string, body: { platformCode: string; title: string; body: string }) =>
    request<SocialContent>(`/v1/businesses/${businessId}/social/content`, { method: "POST", body: JSON.stringify(body) }),
  updateSocial: (businessId: string, contentId: string, body: { title: string; body: string }) =>
    request<SocialContent>(`/v1/businesses/${businessId}/social/content/${contentId}`, { method: "PUT", body: JSON.stringify(body) }),
  approveSocial: (businessId: string, contentId: string) =>
    request<SocialContent>(`/v1/businesses/${businessId}/social/content/${contentId}/approve`, { method: "POST" }),
  publishSocial: (businessId: string, contentId: string) =>
    request<SocialContent>(`/v1/businesses/${businessId}/social/content/${contentId}/publish`, { method: "POST" }),
  refreshSocialMetrics: (businessId: string) =>
    request<SocialWorkspace>(`/v1/businesses/${businessId}/social/metrics/refresh`, { method: "POST" }),
  directories: (businessId: string) => request<DirectoryWorkspace>(`/v1/businesses/${businessId}/directories`),
  prepareDirectory: (businessId: string, platformCode: string) =>
    request<DirectoryTask>(`/v1/businesses/${businessId}/directories/prepare`, { method: "POST", body: JSON.stringify({ platformCode }) }),
  completeDirectoryStep: (businessId: string, taskId: string, stepId: string) =>
    request<DirectoryTask>(`/v1/businesses/${businessId}/directories/tasks/${taskId}/steps/${stepId}/complete`, { method: "POST" }),
  verifyDirectory: (businessId: string, taskId: string, note: string) =>
    request<DirectoryTask>(`/v1/businesses/${businessId}/directories/tasks/${taskId}/verify`, { method: "POST", body: JSON.stringify({ note }) }),
  monitorDirectory: (businessId: string, platformCode: string) =>
    request<DirectoryWorkspace>(`/v1/businesses/${businessId}/directories/${platformCode}/monitor`, { method: "POST" })
};

export type PlatformCapabilities = {
  canRead: boolean;
  canCreate: boolean;
  canUpdate: boolean;
  canDelete: boolean;
  canPublish: boolean;
  canGetMetrics: boolean;
  assistedOnly: boolean;
};
export type PlatformCatalogItem = {
  code: string;
  name: string;
  category: string;
  authMode: string;
  summary: string;
  capabilities: PlatformCapabilities;
};
export type PlatformConnection = {
  id: string;
  businessId: string;
  platformCode: string;
  platformName: string;
  category: string;
  status: string;
  authMode: string;
  externalAccount: string | null;
  grantKind: string | null;
  connectedAtUtc: string | null;
  lastHealthAtUtc: string | null;
  lastHealthStatus: string | null;
  lastError: string | null;
  capabilities: PlatformCapabilities;
};
export type ConnectionCenter = {
  catalog: PlatformCatalogItem[];
  connections: PlatformConnection[];
  maxConnections: number;
};
export type StartConnection = {
  connection: PlatformConnection;
  authorizationUrl: string | null;
  completeInPlace: boolean;
};
export type ConnectionDiagnostic = { check: string; status: string; detail: string };
export type Evidence = { id: string; kind: string; label: string; value: string; source: string };
export type Finding = {
  id: string;
  scanId: string;
  businessId: string;
  category: string;
  severity: string;
  title: string;
  description: string;
  expectedValue: string | null;
  observedValue: string | null;
  recommendation: string;
  suggestedAction: string;
  verificationMethod: string;
  automationState: string;
  status: string;
  evidence: Evidence[];
};
export type ScanSummary = {
  id: string;
  businessId: string;
  trigger: string;
  status: string;
  startedAtUtc: string;
  completedAtUtc: string | null;
  summary: string | null;
  error: string | null;
  findingCount: number;
  openCount: number;
  criticalCount: number;
};
export type ScanDetail = {
  id: string;
  businessId: string;
  trigger: string;
  status: string;
  startedAtUtc: string;
  completedAtUtc: string | null;
  summary: string | null;
  error: string | null;
  scansPerMonth: number;
  scansUsedThisMonth: number;
  findings: Finding[];
};
export type ScanCenter = {
  scans: ScanSummary[];
  latest: ScanDetail | null;
  scansPerMonth: number;
  scansUsedThisMonth: number;
};
export type WebsiteSnapshot = {
  id: string;
  businessId: string;
  url: string | null;
  status: string;
  statusCode: number | null;
  title: string | null;
  metaDescription: string | null;
  h1: string | null;
  canonicalUrl: string | null;
  robots: string | null;
  hasJsonLd: boolean;
  hasFaqSchema: boolean;
  hasOrganizationSchema: boolean;
  hasOgTitle: boolean;
  wordCount: number;
  containsBusinessName: boolean;
  containsPhone: boolean;
  error: string | null;
  fetchedAtUtc: string;
};
export type SearchObservation = {
  id: string;
  category: string;
  severity: string;
  title: string;
  detail: string;
  expectedValue: string | null;
  observedValue: string | null;
  recommendation: string;
};
export type WebsiteIntelligence = {
  snapshot: WebsiteSnapshot | null;
  observations: SearchObservation[];
  searchConsole: { status: string; grantKind: string | null; detail: string };
  searchProvider: string;
  vectorSearchConfigured: boolean;
};
export type SiteSearch = {
  provider: string;
  query: string;
  hits: { title: string; url: string; snippet: string; score: number }[];
};
export type SocialChannel = {
  platformCode: string;
  platformName: string;
  category: string;
  canPublish: boolean;
  canGetMetrics: boolean;
  assistedOnly: boolean;
  connectionStatus: string | null;
  grantKind: string | null;
  metricStatus: string | null;
  metricDetail: string | null;
};
export type SocialContent = {
  id: string;
  businessId: string;
  platformCode: string;
  kind: string;
  title: string;
  body: string;
  status: string;
  verificationStatus: string;
  verificationDetail: string | null;
  lastPublishError: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};
export type SocialWorkspace = {
  channels: SocialChannel[];
  items: SocialContent[];
  note: string;
};
export type DirectoryCapability = {
  platformCode: string;
  platformName: string;
  canRead: boolean;
  canWriteOfficially: boolean;
  assistedOnly: boolean;
  officialRead: string;
  officialWrite: string;
};
export type DirectoryProvider = {
  capabilities: DirectoryCapability;
  connectionStatus: string | null;
  grantKind: string | null;
  lastHealthStatus: string | null;
  readStatus: string;
  readDetail: string;
};
export type DirectoryStep = {
  id: string;
  ordinal: number;
  title: string;
  detail: string;
  completedAtUtc: string | null;
};
export type DirectoryTask = {
  id: string;
  platformCode: string;
  kind: string;
  status: string;
  preparedName: string;
  preparedPhone: string | null;
  preparedWebsite: string | null;
  preparedCategory: string | null;
  preparedServices: string | null;
  verificationNote: string | null;
  verifiedAtUtc: string | null;
  lastMonitoredAtUtc: string | null;
  monitorDetail: string | null;
  steps: DirectoryStep[];
};
export type DirectoryWorkspace = {
  providers: DirectoryProvider[];
  tasks: DirectoryTask[];
  note: string;
};

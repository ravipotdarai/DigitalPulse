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
  annualPriceInr: number;
  maxBusinesses: number;
  maxLocations: number;
  maxConnections: number;
  scansPerMonth: number;
  actionsPerMonth: number;
  aiGenerationsPerMonth: number;
  maxUsers: number;
  maxAgencyClients: number;
  storageGb: number;
  whiteLabel: boolean;
  whatsAppEnabled: boolean;
  whatsAppMessagesPerMonth: number;
  monitoringIntervalHours: number;
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
  projectCount: number;
  contentHoldCount: number;
  aiRunCount: number;
  aiHeldCount: number;
  actionOpenCount: number;
  actionHeldCount: number;
  whatsAppOptInCount: number;
  whatsAppHeldCount: number;
  lastMonitoringAtUtc: string | null;
  openAlertCount: number;
  reportCount: number;
  monitoringIntervalHours: number;
  monitoringHoldReason: string;
  subscriptionStatus: string;
  billingHoldReason: string;
  heldInvoiceCount: number;
  agencyClientCount: number;
  whiteLabelEnabled: boolean;
  agencyHoldReason: string;
  lastBackupAtUtc: string | null;
  readinessHoldCount: number;
  operationsHoldReason: string;
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
  if (token && !PUBLIC_PATHS.has(path) && !path.startsWith("/v1/hub/")) headers.set("Authorization", `Bearer ${token}`);

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
  connectionAccounts: (businessId: string, connectionId: string) =>
    request<ConnectionAccountOption[]>(`/v1/businesses/${businessId}/connections/${connectionId}/accounts`),
  selectConnectionAccount: (businessId: string, connectionId: string, externalAccount: string) =>
    request<PlatformConnection>(`/v1/businesses/${businessId}/connections/${connectionId}/account`, {
      method: "POST",
      body: JSON.stringify({ externalAccount })
    }),
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
  listReports: (businessId: string) => request<TestReport[]>(`/v1/businesses/${businessId}/reports`),
  completeFindingStep: (businessId: string, findingId: string, stepId: string) =>
    request<Finding>(`/v1/businesses/${businessId}/findings/${findingId}/steps/${stepId}/complete`, { method: "POST" }),
  verifyFinding: (businessId: string, findingId: string) =>
    request<Finding>(`/v1/businesses/${businessId}/findings/${findingId}/verify`, { method: "POST" }),
  downloadReportPdf: async (businessId: string, reportId: string) => {
    const token = getStoredToken();
    const headers = new Headers();
    if (token) headers.set("Authorization", `Bearer ${token}`);
    const response = await fetch(`/v1/businesses/${businessId}/reports/${reportId}/pdf`, { headers });
    if (!response.ok) {
      throw new ApiError(response.status, "Could not download the stored report PDF.");
    }
    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    const file = response.headers.get("content-disposition")?.match(/filename="?([^"]+)"?/)?.[1] ?? "digitalpulse-report.pdf";
    link.href = url;
    link.download = file;
    link.click();
    URL.revokeObjectURL(url);
  },
  social: (businessId: string) => request<SocialWorkspace>(`/v1/businesses/${businessId}/social`),
  createSocial: (businessId: string, body: { platformCode: string; title: string; body: string }) =>
    request<SocialContent>(`/v1/businesses/${businessId}/social/content`, { method: "POST", body: JSON.stringify(body) }),
  updateSocial: (businessId: string, contentId: string, body: { title: string; body: string }) =>
    request<SocialContent>(`/v1/businesses/${businessId}/social/content/${contentId}`, { method: "PUT", body: JSON.stringify(body) }),
  approveSocial: (businessId: string, contentId: string) =>
    request<SocialContent>(`/v1/businesses/${businessId}/social/content/${contentId}/approve`, { method: "POST" }),
  publishSocial: (businessId: string, contentId: string) =>
    request<SocialContent>(`/v1/businesses/${businessId}/social/content/${contentId}/publish`, { method: "POST" }),
  deleteSocial: (businessId: string, contentId: string) =>
    request<void>(`/v1/businesses/${businessId}/social/content/${contentId}`, { method: "DELETE" }),
  uploadSocialMedia: async (businessId: string, file: File) => {
    const token = getStoredToken();
    const body = new FormData();
    body.append("file", file);
    const headers = new Headers();
    if (token) headers.set("Authorization", `Bearer ${token}`);
    const response = await fetch(`/v1/businesses/${businessId}/social/media`, { method: "POST", headers, body });
    const payload = await response.json().catch(() => ({}));
    if (!response.ok) {
      throw new ApiError(response.status, typeof payload.title === "string" ? payload.title : "The image or video could not be stored.");
    }
    return payload as SocialMediaUpload;
  },
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
    request<DirectoryWorkspace>(`/v1/businesses/${businessId}/directories/${platformCode}/monitor`, { method: "POST" }),
  projects: (businessId: string) => request<ProjectWorkspace>(`/v1/businesses/${businessId}/projects`),
  project: (businessId: string, projectId: string) =>
    request<ProjectDetail>(`/v1/businesses/${businessId}/projects/${projectId}`),
  createProject: (businessId: string, body: CreateProjectBody) =>
    request<ProjectDetail>(`/v1/businesses/${businessId}/projects`, { method: "POST", body: JSON.stringify(body) }),
  updateProject: (businessId: string, projectId: string, body: UpdateProjectBody) =>
    request<ProjectDetail>(`/v1/businesses/${businessId}/projects/${projectId}`, { method: "PUT", body: JSON.stringify(body) }),
  linkProjectService: (businessId: string, projectId: string, id: string) =>
    request<ProjectDetail>(`/v1/businesses/${businessId}/projects/${projectId}/services`, { method: "POST", body: JSON.stringify({ id }) }),
  linkProjectBrand: (businessId: string, projectId: string, id: string) =>
    request<ProjectDetail>(`/v1/businesses/${businessId}/projects/${projectId}/brands`, { method: "POST", body: JSON.stringify({ id }) }),
  registerProjectMedia: (businessId: string, projectId: string, body: { label: string; kind: string; sourceUrl: string | null }) =>
    request<ProjectDetail>(`/v1/businesses/${businessId}/projects/${projectId}/media`, { method: "POST", body: JSON.stringify(body) }),
  generateProjectContent: (businessId: string, projectId: string) =>
    request<ProjectDetail>(`/v1/businesses/${businessId}/projects/${projectId}/factory`, { method: "POST" }),
  contentHub: (businessId: string) => request<ContentHubWorkspace>(`/v1/businesses/${businessId}/content`),
  hubContent: (businessId: string, contentId: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}`),
  createHubContent: (businessId: string, body: HubContentDraft) =>
    request<HubContent>(`/v1/businesses/${businessId}/content`, { method: "POST", body: JSON.stringify(body) }),
  createHubCaseStudy: (businessId: string, projectId: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/from-project`, { method: "POST", body: JSON.stringify({ projectId }) }),
  updateHubContent: (businessId: string, contentId: string, body: HubContentDraft & { changeSummary?: string | null }) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}`, { method: "PUT", body: JSON.stringify(body) }),
  deleteHubContent: (businessId: string, contentId: string) =>
    request<void>(`/v1/businesses/${businessId}/content/${contentId}`, { method: "DELETE" }),
  submitHubContent: (businessId: string, contentId: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}/submit`, { method: "POST" }),
  approveHubContent: (businessId: string, contentId: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}/approve`, { method: "POST" }),
  rejectHubContent: (businessId: string, contentId: string, note?: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}/reject`, { method: "POST", body: JSON.stringify({ note: note ?? null }) }),
  scheduleHubContent: (businessId: string, contentId: string, scheduledAtUtc: string, channel?: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}/schedule`, { method: "POST", body: JSON.stringify({ scheduledAtUtc, channel: channel ?? "HUB" }) }),
  cancelHubSchedule: (businessId: string, contentId: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}/schedule/cancel`, { method: "POST" }),
  publishHubContent: (businessId: string, contentId: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}/publish`, { method: "POST" }),
  restoreHubRevision: (businessId: string, contentId: string, revisionId: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}/revisions/${revisionId}/restore`, { method: "POST" }),
  attachHubMedia: (businessId: string, contentId: string, mediaAssetId: string, role: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}/media`, { method: "POST", body: JSON.stringify({ mediaAssetId, role }) }),
  registerHubMedia: (businessId: string, body: { label: string; kind: string; sourceUrl: string }) =>
    request<HubMediaAsset>(`/v1/businesses/${businessId}/content/media`, { method: "POST", body: JSON.stringify(body) }),
  uploadHubMedia: async (businessId: string, file: File) => {
    const token = getStoredToken();
    const body = new FormData();
    body.append("file", file);
    const headers = new Headers();
    if (token) headers.set("Authorization", `Bearer ${token}`);
    const response = await fetch(`/v1/businesses/${businessId}/content/media/upload`, { method: "POST", headers, body });
    const payload = await response.json().catch(() => ({}));
    if (!response.ok) {
      throw new ApiError(response.status, typeof payload.title === "string" ? payload.title : "The image could not be stored.");
    }
    return payload as HubMediaAsset;
  },
  archiveHubContent: (businessId: string, contentId: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}/archive`, { method: "POST" }),
  releaseHubCalendar: (businessId: string) =>
    request<ContentHubWorkspace>(`/v1/businesses/${businessId}/content/calendar/release`, { method: "POST" }),
  analyzeHubSeo: (businessId: string, contentId: string, focusKeyword?: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}/seo`, { method: "POST", body: JSON.stringify({ focusKeyword: focusKeyword ?? null }) }),
  createHubVariants: (businessId: string, contentId: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}/variants`, { method: "POST" }),
  distributeHubContent: (businessId: string, contentId: string, providerCode: string) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/${contentId}/distribute`, { method: "POST", body: JSON.stringify({ providerCode }) }),
  discoverContentOpportunities: (businessId: string) =>
    request<ContentHubWorkspace>(`/v1/businesses/${businessId}/content/opportunities/discover`, { method: "POST" }),
  createContentOpportunity: (businessId: string, topic: string, description?: string) =>
    request<ContentHubWorkspace>(`/v1/businesses/${businessId}/content/opportunities`, { method: "POST", body: JSON.stringify({ topic, description: description ?? null }) }),
  dismissContentOpportunity: (businessId: string, opportunityId: string) =>
    request<ContentHubWorkspace>(`/v1/businesses/${businessId}/content/opportunities/${opportunityId}/dismiss`, { method: "POST" }),
  generateHubContent: (businessId: string, prompt: string, opportunityId?: string | null) =>
    request<HubContent>(`/v1/businesses/${businessId}/content/generate`, { method: "POST", body: JSON.stringify({ prompt, opportunityId: opportunityId ?? null }) }),
  assistHubContent: (businessId: string, body: { action: string; instruction?: string | null; contentId?: string | null; section?: string | null }) =>
    request<HubAssist>(`/v1/businesses/${businessId}/content/assist`, { method: "POST", body: JSON.stringify(body) }),
  publicHubIndex: (businessId: string) => request<PublicHubIndex>(`/v1/hub/${businessId}`),
  publicHubArticle: (businessId: string, slug: string) =>
    request<PublicHubArticle>(`/v1/hub/${businessId}/${slug}`),
  requestProjectApproval: (businessId: string, projectId: string, contentId: string) =>
    request<ProjectDetail>(`/v1/businesses/${businessId}/projects/${projectId}/content/${contentId}/approvals`, { method: "POST" }),
  decideProjectApproval: (businessId: string, projectId: string, approvalId: string, approve: boolean, note: string) =>
    request<ProjectDetail>(`/v1/businesses/${businessId}/projects/${projectId}/approvals/${approvalId}/decide`, {
      method: "POST",
      body: JSON.stringify({ approve, note })
    }),
  ai: (businessId: string) => request<AiWorkspace>(`/v1/businesses/${businessId}/ai`),
  addKnowledge: (businessId: string, body: { title: string; body: string; kind: string; sourceUrl: string | null }) =>
    request<KnowledgeEntry>(`/v1/businesses/${businessId}/ai/knowledge`, { method: "POST", body: JSON.stringify(body) }),
  syncGraph: (businessId: string) =>
    request<AiWorkspace>(`/v1/businesses/${businessId}/ai/graph/sync`, { method: "POST" }),
  runAi: (businessId: string, body: { agent: string; prompt: string }) =>
    request<AiRun>(`/v1/businesses/${businessId}/ai/runs`, { method: "POST", body: JSON.stringify(body) }),
  actions: (businessId: string) => request<ActionWorkspace>(`/v1/businesses/${businessId}/actions`),
  updateActionPolicy: (businessId: string, body: { mode: string; allowLowRiskAuto: boolean; maxAttempts: number }) =>
    request<ActionWorkspace>(`/v1/businesses/${businessId}/actions/policy`, { method: "PUT", body: JSON.stringify(body) }),
  enqueueAction: (businessId: string, body: { kind: string; title: string; targetId: string | null; targetLabel: string | null }) =>
    request<WorkAction>(`/v1/businesses/${businessId}/actions`, { method: "POST", body: JSON.stringify(body) }),
  approveAction: (businessId: string, actionId: string) =>
    request<WorkAction>(`/v1/businesses/${businessId}/actions/${actionId}/approve`, { method: "POST" }),
  executeAction: (businessId: string, actionId: string) =>
    request<WorkAction>(`/v1/businesses/${businessId}/actions/${actionId}/execute`, { method: "POST" }),
  retryAction: (businessId: string, actionId: string) =>
    request<WorkAction>(`/v1/businesses/${businessId}/actions/${actionId}/retry`, { method: "POST" }),
  verifyAction: (businessId: string, actionId: string) =>
    request<WorkAction>(`/v1/businesses/${businessId}/actions/${actionId}/verify`, { method: "POST" }),
  whatsapp: (businessId: string) => request<WhatsAppWorkspace>(`/v1/businesses/${businessId}/whatsapp`),
  connectWhatsApp: (businessId: string, body: { displayName: string; phoneNumber: string }) =>
    request<WhatsAppWorkspace>(`/v1/businesses/${businessId}/whatsapp/connect`, { method: "POST", body: JSON.stringify(body) }),
  verifyWhatsAppPhone: (businessId: string) =>
    request<WhatsAppWorkspace>(`/v1/businesses/${businessId}/whatsapp/phone/verify`, { method: "POST" }),
  importWhatsAppContacts: (businessId: string) =>
    request<WhatsAppWorkspace>(`/v1/businesses/${businessId}/whatsapp/contacts/import`, { method: "POST" }),
  optInWhatsApp: (businessId: string, contactId: string) =>
    request<WhatsAppWorkspace>(`/v1/businesses/${businessId}/whatsapp/contacts/${contactId}/opt-in`, { method: "POST" }),
  optOutWhatsApp: (businessId: string, contactId: string) =>
    request<WhatsAppWorkspace>(`/v1/businesses/${businessId}/whatsapp/contacts/${contactId}/opt-out`, { method: "POST" }),
  createWhatsAppTemplate: (businessId: string, body: { name: string; language: string; category: string; body: string }) =>
    request<WhatsAppTemplate>(`/v1/businesses/${businessId}/whatsapp/templates`, { method: "POST", body: JSON.stringify(body) }),
  approveWhatsAppTemplate: (businessId: string, templateId: string) =>
    request<WhatsAppTemplate>(`/v1/businesses/${businessId}/whatsapp/templates/${templateId}/approve`, { method: "POST" }),
  createWhatsAppCampaign: (businessId: string, body: { name: string; templateId: string }) =>
    request<WhatsAppCampaign>(`/v1/businesses/${businessId}/whatsapp/campaigns`, { method: "POST", body: JSON.stringify(body) }),
  approveWhatsAppCampaign: (businessId: string, campaignId: string) =>
    request<WhatsAppCampaign>(`/v1/businesses/${businessId}/whatsapp/campaigns/${campaignId}/approve`, { method: "POST" }),
  scheduleWhatsAppCampaign: (businessId: string, campaignId: string, scheduledAtUtc: string) =>
    request<WhatsAppCampaign>(`/v1/businesses/${businessId}/whatsapp/campaigns/${campaignId}/schedule`, { method: "POST", body: JSON.stringify({ scheduledAtUtc }) }),
  draftWhatsApp: (businessId: string, body: { contactId: string; kind: string; body: string; templateId: string | null }) =>
    request<WhatsAppMessage>(`/v1/businesses/${businessId}/whatsapp/messages`, { method: "POST", body: JSON.stringify(body) }),
  draftWhatsAppAi: (businessId: string, body: { contactId: string; kind: string; prompt: string; templateId: string | null }) =>
    request<WhatsAppMessage>(`/v1/businesses/${businessId}/whatsapp/messages/draft-ai`, { method: "POST", body: JSON.stringify(body) }),
  approveWhatsAppMessage: (businessId: string, messageId: string) =>
    request<WhatsAppMessage>(`/v1/businesses/${businessId}/whatsapp/messages/${messageId}/approve`, { method: "POST" }),
  sendWhatsAppMessage: (businessId: string, messageId: string) =>
    request<WhatsAppMessage>(`/v1/businesses/${businessId}/whatsapp/messages/${messageId}/send`, { method: "POST" }),
  recordWhatsAppInbound: (businessId: string, body: { contactId: string; body: string }) =>
    request<WhatsAppWorkspace>(`/v1/businesses/${businessId}/whatsapp/inbound`, { method: "POST", body: JSON.stringify(body) }),
  replyWhatsApp: (businessId: string, conversationId: string, body: string) =>
    request<WhatsAppMessage>(`/v1/businesses/${businessId}/whatsapp/conversations/${conversationId}/reply`, { method: "POST", body: JSON.stringify({ body }) }),
  monitoring: (businessId: string) => request<MonitoringWorkspace>(`/v1/businesses/${businessId}/monitoring`),
  runMonitoring: (businessId: string) =>
    request<MonitoringWorkspace>(`/v1/businesses/${businessId}/monitoring/runs`, { method: "POST" }),
  assembleReport: (businessId: string) =>
    request<MonitoringWorkspace>(`/v1/businesses/${businessId}/monitoring/reports`, { method: "POST" }),
  recordReportDecision: (businessId: string, reportId: string, body: { decision: string }) =>
    request<MonitoringWorkspace>(`/v1/businesses/${businessId}/monitoring/reports/${reportId}/decision`, { method: "POST", body: JSON.stringify(body) }),
  addCompetitor: (businessId: string, body: { name: string; website: string | null; notes: string | null }) =>
    request<MonitoringWorkspace>(`/v1/businesses/${businessId}/monitoring/competitors`, { method: "POST", body: JSON.stringify(body) }),
  acknowledgeAlert: (businessId: string, alertId: string) =>
    request<MonitoringWorkspace>(`/v1/businesses/${businessId}/monitoring/alerts/${alertId}/acknowledge`, { method: "POST" }),
  billing: () => request<BillingWorkspace>("/v1/billing"),
  changePlan: (body: { planCode: string; interval: string }) =>
    request<BillingWorkspace>("/v1/billing/change-plan", { method: "POST", body: JSON.stringify(body) }),
  checkoutBilling: (body: { invoiceId: string | null }) =>
    request<BillingWorkspace>("/v1/billing/checkout", { method: "POST", body: JSON.stringify(body) }),
  cancelBilling: (body: { immediately: boolean; reason: string | null }) =>
    request<BillingWorkspace>("/v1/billing/cancel", { method: "POST", body: JSON.stringify(body) }),
  resumeBilling: () => request<BillingWorkspace>("/v1/billing/resume", { method: "POST" }),
  agency: () => request<AgencyWorkspace>("/v1/agency"),
  createAgencyClient: (body: {
    name: string;
    website?: string | null;
    status?: string | null;
    contactName?: string | null;
    contactEmail?: string | null;
    notes?: string | null;
    externalRef?: string | null;
  }) => request<AgencyWorkspace>("/v1/agency/clients", { method: "POST", body: JSON.stringify(body) }),
  updateAgencyClient: (clientId: string, body: {
    status: string;
    contactName?: string | null;
    contactEmail?: string | null;
    notes?: string | null;
    externalRef?: string | null;
  }) => request<AgencyWorkspace>(`/v1/agency/clients/${clientId}`, { method: "PUT", body: JSON.stringify(body) }),
  updateWhiteLabel: (body: {
    displayName: string;
    supportEmail?: string | null;
    supportPhone?: string | null;
    primaryColor?: string | null;
    logoUrl?: string | null;
    customDomain?: string | null;
    enabled: boolean;
  }) => request<AgencyWorkspace>("/v1/agency/white-label", { method: "PUT", body: JSON.stringify(body) }),
  startAgencyWorkflow: (body: { kind: string; clientId?: string | null }) =>
    request<AgencyWorkspace>("/v1/agency/workflows", { method: "POST", body: JSON.stringify(body) }),
  advanceAgencyWorkflow: (workflowId: string, body: { note?: string | null; hold?: boolean; holdReason?: string | null }) =>
    request<AgencyWorkspace>(`/v1/agency/workflows/${workflowId}/advance`, { method: "POST", body: JSON.stringify(body) }),
  assembleAgencyReport: (body: { scope?: string | null; clientId?: string | null }) =>
    request<AgencyWorkspace>("/v1/agency/reports", { method: "POST", body: JSON.stringify(body) }),
  recordAgencyDecision: (reportId: string, body: { decision: string }) =>
    request<AgencyWorkspace>(`/v1/agency/reports/${reportId}/decision`, { method: "POST", body: JSON.stringify(body) }),
  operations: () => request<OperationsWorkspace>("/v1/operations"),
  captureBackup: () => request<OperationsWorkspace>("/v1/operations/backups", { method: "POST" }),
  restoreBackup: (snapshotId: string) =>
    request<OperationsWorkspace>(`/v1/operations/backups/${snapshotId}/restore`, { method: "POST" }),
  startDrill: (body: { kind: string }) =>
    request<OperationsWorkspace>("/v1/operations/drills", { method: "POST", body: JSON.stringify(body) }),
  runInventory: (body: { kind?: string | null }) =>
    request<OperationsWorkspace>("/v1/operations/scans", { method: "POST", body: JSON.stringify(body) }),
  assembleReadiness: () => request<OperationsWorkspace>("/v1/operations/readiness", { method: "POST" }),
  ready: () => request<ReadyStatus>("/ready")
};

export type ReadyStatus = {
  status: string;
  environment?: string;
  role?: string;
  holds?: (string | null)[];
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
  hasLiveCredential: boolean;
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
export type ConnectionAccountOption = { id: string; label: string; kind: string };
export type Evidence = { id: string; kind: string; label: string; value: string; source: string };
export type FindingStep = {
  id: string;
  ordinal: number;
  title: string;
  detail: string;
  officialUrl: string | null;
  completedAtUtc: string | null;
};
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
  resolutionPath: string;
  playbookCode: string | null;
  verifiedAtUtc: string | null;
  steps: FindingStep[];
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
  containsEmail: boolean;
  containsAddress: boolean;
  containsVision: boolean;
  hasContactForm: boolean;
  pageRole: string;
  auditRunId: string | null;
  error: string | null;
  fetchedAtUtc: string;
};
export type TestReport = {
  id: string;
  kind: string;
  title: string;
  observedFact: string;
  recommendation: string;
  holdReason: string;
  auditRunId: string | null;
  createdAtUtc: string;
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
  pages: WebsiteSnapshot[];
  observations: SearchObservation[];
  searchConsole: { status: string; grantKind: string | null; detail: string };
  searchConsoleQueries: { query: string; clicks: number; impressions: number; ctr: number; position: number }[];
  reports: TestReport[];
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
  review: SocialPostReview;
};
export type SocialPostReview = {
  safetyStatus: string;
  safetyDetail: string;
  seoStatus: string;
  seoNotes: string[];
  analyticsStatus: string;
  analyticsDetail: string;
};
export type SocialWorkspace = {
  channels: SocialChannel[];
  items: SocialContent[];
  note: string;
};
export type SocialMediaUpload = { id: string; kind: string; fileName: string; contentType: string };
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
export type CreateProjectBody = {
  name: string;
  clientName: string | null;
  industry: string | null;
  location: string | null;
  description: string | null;
  outcomes: string | null;
  startedOn: string | null;
  completedOn: string | null;
  permissionScope: string;
  confidentiality: string;
};
export type UpdateProjectBody = CreateProjectBody & { publicationStatus: string };
export type ProjectSummary = {
  id: string;
  businessId: string;
  name: string;
  clientName: string | null;
  permissionScope: string;
  confidentiality: string;
  publicationStatus: string;
  updatedAtUtc: string;
};
export type ContentVariant = {
  id: string;
  kind: string;
  title: string;
  body: string;
  status: string;
  publicationHold: string;
};
export type ProjectApproval = {
  id: string;
  contentItemId: string;
  reason: string;
  open: boolean;
  decision: string | null;
  decisionNote: string | null;
};
export type ProjectContentPack = {
  id: string;
  projectId: string | null;
  title: string;
  status: string;
  sourceNote: string;
  variants: ContentVariant[];
  approvals: ProjectApproval[];
};
export type ContentTypeOption = { code: string; name: string };
export type ContentSeoCheck = { code: string; label: string; passed: boolean; note: string };
export type ContentSeo = {
  searchIntent: string;
  checksPassed: number;
  checksTotal: number;
  seoScore: number;
  metaTitle: string;
  metaDescription: string;
  focusKeyword: string | null;
  canonicalUrl: string | null;
  notes: string[];
  lastAnalyzedAtUtc: string | null;
  readabilityScore?: number;
  aeoScore?: number;
  slugScore?: number;
  internalLinkScore?: number;
  entityCoverageScore?: number;
  checks?: ContentSeoCheck[];
  aeoChecks?: ContentSeoCheck[];
  entitiesMentioned?: number;
  entitiesTotal?: number;
};
export type HubContentSummary = {
  id: string;
  contentTypeCode: string;
  title: string;
  slug: string;
  status: string;
  visibility: string;
  updatedAtUtc: string;
  seoScore: number;
  excerpt: string;
  publishedAtUtc: string | null;
};
export type ContentOpportunity = {
  id: string;
  topicId: string;
  topic: string;
  description: string;
  sourceType: string;
  coverageScore: number | null;
  opportunityScore: number | null;
  relevanceScore?: number | null;
  competitionScore?: number | null;
  priority?: number;
  reason: string;
  status: string;
};
export type ContentCalendarItem = {
  id: string;
  contentItemId: string;
  title: string;
  scheduledAtUtc: string;
  status: string;
  channel: string;
};
export type ContentNamed = { id: string; name: string; slug: string };
export type ContentMetricRow = { providerCode: string; metricDate: string; views: number | null; detail: string; clicks?: number | null; engagements?: number | null; leads?: number | null; conversions?: number | null; contentItemId?: string | null };
export type ContentHubWorkspace = {
  types: ContentTypeOption[];
  items: HubContentSummary[];
  opportunities: ContentOpportunity[];
  calendar: ContentCalendarItem[];
  categories: ContentNamed[];
  tags: ContentNamed[];
  metrics: ContentMetricRow[];
  media?: HubMediaAsset[];
  note: string;
  entities?: string[];
  projects?: ContentNamed[];
  channels?: { providerCode: string; mode: string; note: string }[];
};
export type HubMediaAsset = { id: string; label: string; kind: string; sourceUrl: string | null };
export type HubAssist = { action: string; suggestion: string; hold: string; providerName: string; isLive: boolean; target: string };
export type HubContent = {
  id: string;
  businessId: string;
  projectId: string | null;
  contentTypeCode: string;
  title: string;
  slug: string;
  excerpt: string;
  body: string;
  status: string;
  visibility: string;
  sourceNote: string;
  featuredMediaAssetId: string | null;
  canonicalUrl: string | null;
  publishedAtUtc: string | null;
  scheduledAtUtc: string | null;
  updatedAtUtc: string;
  categories: string[];
  tags: string[];
  seo: ContentSeo;
  revisions: { id: string; versionNumber: number; title: string; changeSummary: string; createdAtUtc: string }[];
  variants: { id: string; kind: string; title: string; body: string; status: string; publicationHold: string }[];
  distributions: { id: string; providerCode: string; status: string; failureReason: string | null; publishedAtUtc: string | null }[];
  metrics: ContentMetricRow[];
  media?: { id: string; mediaAssetId: string; role: string; displayOrder: number; label: string | null; sourceUrl: string | null }[];
};
export type HubContentDraft = {
  contentTypeCode: string;
  title: string;
  slug?: string | null;
  excerpt: string;
  body: string;
  visibility: string;
  projectId?: string | null;
  focusKeyword?: string | null;
  canonicalUrl?: string | null;
  categories?: string[];
  tags?: string[];
  featuredMediaAssetId?: string | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
};
export type PublicHubArticle = {
  businessName: string;
  title: string;
  slug: string;
  excerpt: string;
  body: string;
  contentTypeCode: string;
  publishedAtUtc: string;
  featuredImageUrl?: string | null;
  metaTitle?: string;
  metaDescription?: string;
};
export type PublicHubIndex = {
  businessId: string;
  businessName: string;
  articles: { title: string; slug: string; excerpt: string; contentTypeCode: string; publishedAtUtc: string; featuredImageUrl?: string | null }[];
  featured?: { title: string; slug: string; excerpt: string; contentTypeCode: string; publishedAtUtc: string; featuredImageUrl?: string | null } | null;
  caseStudies?: { title: string; slug: string; excerpt: string; contentTypeCode: string; publishedAtUtc: string; featuredImageUrl?: string | null }[];
  services?: string[];
  cta?: string;
  metaTitle?: string;
  metaDescription?: string;
  brandName?: string | null;
  logoUrl?: string | null;
  primaryColor?: string | null;
  whiteLabel?: boolean;
};
export type ProjectDetail = {
  project: ProjectSummary;
  description: string | null;
  outcomes: string | null;
  industry: string | null;
  location: string | null;
  startedOn: string | null;
  completedOn: string | null;
  services: string[];
  brands: string[];
  media: { id: string; label: string; kind: string; sourceUrl: string | null; note: string }[];
  packs: ProjectContentPack[];
  note: string;
};
export type ProjectWorkspace = {
  projects: ProjectSummary[];
  services: { id: string; name: string }[];
  brands: { id: string; name: string }[];
  note: string;
};
export type KnowledgeEntry = {
  id: string;
  title: string;
  body: string;
  kind: string;
  sourceUrl: string | null;
  createdAtUtc: string;
};
export type GraphNode = { id: string; kind: string; label: string; value: string | null };
export type GraphEdge = { id: string; fromNodeId: string; toNodeId: string; relation: string };
export type AiEvaluation = {
  passed: boolean;
  hasEvidence: boolean;
  hasConflict: boolean;
  hasRestrictedFact: boolean;
  confidence: string;
  summary: string;
};
export type AiAudit = { stage: string; detail: string; atUtc: string };
export type AiRun = {
  id: string;
  agent: string;
  prompt: string;
  status: string;
  confidence: string;
  output: string;
  providerName: string;
  providerIsLive: boolean;
  holdReason: string;
  createdAtUtc: string;
  evaluation: AiEvaluation | null;
  audit: AiAudit[];
};
export type AiWorkspace = {
  providerName: string;
  providerIsLive: boolean;
  note: string;
  agents: { code: string; name: string; purpose: string }[];
  knowledge: KnowledgeEntry[];
  nodes: GraphNode[];
  edges: GraphEdge[];
  runs: AiRun[];
};
export type ActionKind = { code: string; name: string; risk: string; externalWrite: boolean; purpose: string };
export type ActionAttempt = { ordinal: number; outcome: string; detail: string; atUtc: string };
export type ActionVerification = { status: string; detail: string; atUtc: string };
export type WorkAction = {
  id: string;
  kind: string;
  status: string;
  risk: string;
  title: string;
  idempotencyKey: string;
  targetId: string | null;
  targetLabel: string | null;
  liveWriteAvailable: boolean;
  autopilotEligible: boolean;
  holdReason: string;
  attemptCount: number;
  nextRetryAtUtc: string | null;
  createdAtUtc: string;
  attempts: ActionAttempt[];
  verification: ActionVerification | null;
};
export type ActionWorkspace = {
  policy: { mode: string; allowLowRiskAuto: boolean; requireApprovalForHighRisk: boolean; maxAttempts: number };
  actionsPerMonth: number;
  actionsUsedThisMonth: number;
  note: string;
  kinds: ActionKind[];
  actions: WorkAction[];
};
export type WhatsAppAccount = {
  id: string;
  displayName: string;
  phoneNumber: string;
  status: string;
  phoneStatus: string;
  connected: boolean;
  phoneVerified: boolean;
  holdReason: string;
  wabaId: string | null;
  lastHealthAtUtc: string | null;
  lastHealthDetail: string | null;
  cloudApiIsLive: boolean;
  cloudApiName: string;
};
export type WhatsAppContact = {
  id: string;
  displayName: string;
  mobile: string;
  consent: string;
  optedInAtUtc: string | null;
  optedOutAtUtc: string | null;
  lastInboundAtUtc: string | null;
  windowOpen: boolean;
  customerId: string | null;
};
export type WhatsAppTemplate = {
  id: string;
  name: string;
  language: string;
  category: string;
  body: string;
  status: string;
  holdReason: string;
};
export type WhatsAppCampaign = {
  id: string;
  templateId: string;
  name: string;
  status: string;
  scheduledAtUtc: string | null;
  holdReason: string;
  audienceCount: number;
  sendCount: number;
  heldCount: number;
  failedCount: number;
};
export type WhatsAppMessage = {
  id: string;
  contactId: string;
  conversationId: string | null;
  campaignId: string | null;
  templateId: string | null;
  kind: string;
  status: string;
  body: string;
  holdReason: string;
  untrusted: boolean;
  providerMessageId: string | null;
  createdAtUtc: string;
  attempts: { ordinal: number; outcome: string; detail: string; atUtc: string }[];
};
export type WhatsAppWorkspace = {
  planEnabled: boolean;
  messagesPerMonth: number;
  messagesUsedThisMonth: number;
  note: string;
  account: WhatsAppAccount | null;
  analytics: { optedIn: number; optedOut: number; templatesApproved: number; campaigns: number; messagesHeld: number; messagesFailed: number; inboundUntrusted: number; deliveryNote: string };
  contacts: WhatsAppContact[];
  templates: WhatsAppTemplate[];
  campaigns: WhatsAppCampaign[];
  conversations: { id: string; contactId: string; windowOpen: boolean; messages: WhatsAppMessage[] }[];
  messages: WhatsAppMessage[];
};
export type MonitoringKind = { code: string; name: string; canObserveWithoutLiveApi: boolean; purpose: string };
export type MonitoringResult = {
  id: string;
  kind: string;
  status: string;
  title: string;
  observedFact: string;
  recommendation: string;
  previousValue: string | null;
  currentValue: string | null;
};
export type MonitoringRun = {
  id: string;
  trigger: string;
  status: string;
  summary: string;
  startedAtUtc: string;
  completedAtUtc: string | null;
  results: MonitoringResult[];
};
export type MonitoringAlert = {
  id: string;
  severity: string;
  status: string;
  title: string;
  detail: string;
  openedAtUtc: string;
  acknowledgedAtUtc: string | null;
};
export type PresenceReport = {
  id: string;
  kind: string;
  title: string;
  observedFact: string;
  recommendation: string;
  aiInterpretation: string;
  customerDecision: string | null;
  periodStartUtc: string;
  periodEndUtc: string;
  holdReason: string;
};
export type MonitoringWorkspace = {
  schedule: {
    intervalHours: number;
    enabled: boolean;
    lastRunAtUtc: string | null;
    nextRunAtUtc: string | null;
    due: boolean;
    holdReason: string;
  };
  note: string;
  kinds: MonitoringKind[];
  runs: MonitoringRun[];
  alerts: MonitoringAlert[];
  competitors: { id: string; name: string; website: string | null; notes: string | null }[];
  reports: PresenceReport[];
};
export type BillingWorkspace = {
  subscription: {
    id: string;
    planCode: string;
    planName: string;
    status: string;
    interval: string;
    holdReason: string;
    cancelAtPeriodEnd: boolean;
  } | null;
  plan: PlanResponse | null;
  providerIsLive: boolean;
  providerName: string;
  note: string;
  availablePlans: PlanResponse[];
  usage: { kind: string; used: number; included: number; note: string }[];
  invoices: { id: string; number: string; status: string; amountInr: number; holdReason: string }[];
  payments: { id: string; status: string; provider: string; detail: string }[];
  webhooks: { id: string; eventType: string; untrusted: boolean; holdReason: string }[];
};
export type AgencyClient = {
  id: string;
  businessId: string;
  tenantId: string;
  name: string;
  website: string | null;
  status: string;
  contactName: string | null;
  contactEmail: string | null;
  notes: string | null;
  externalRef: string | null;
  locationCount: number;
  openFindingCount: number;
  lastScanAtUtc: string | null;
};
export type AgencyWorkspace = {
  tenantName: string;
  tenantType: string;
  planName: string;
  clientCap: number;
  whiteLabelEntitled: boolean;
  note: string;
  whiteLabel: {
    id: string;
    displayName: string;
    supportEmail: string | null;
    supportPhone: string | null;
    primaryColor: string;
    logoUrl: string | null;
    customDomain: string | null;
    enabled: boolean;
    entitled: boolean;
    holdReason: string;
  };
  clients: AgencyClient[];
  workflows: {
    id: string;
    clientId: string | null;
    businessId: string | null;
    kind: string;
    status: string;
    currentStep: number;
    currentStepName: string;
    holdReason: string;
    steps: { id: string; ordinal: number; name: string; completed: boolean; note: string | null }[];
  }[];
  reports: {
    id: string;
    scope: string;
    clientId: string | null;
    businessId: string | null;
    title: string;
    observedFact: string;
    recommendation: string;
    aiInterpretation: string;
    customerDecision: string | null;
    holdReason: string;
    lines: { id: string; businessId: string | null; kind: string; body: string }[];
  }[];
};
export type OperationsWorkspace = {
  environmentName: string;
  hostRole: string;
  rateLimitingEnabled: boolean;
  rateLimitPerMinute: number;
  keyVaultConfigured: boolean;
  appInsightsConfigured: boolean;
  redisConfigured: boolean;
  azureBackupConfigured: boolean;
  note: string;
  cost: { meter: string; used: number; included: number; note: string }[];
  backups: { id: string; status: string; manifest: string; checksum: string; holdReason: string; createdAtUtc: string }[];
  restores: { id: string; snapshotId: string; status: string; holdReason: string; createdAtUtc: string }[];
  drills: { id: string; kind: string; status: string; observedFact: string; holdReason: string; createdAtUtc: string }[];
  inventories: { id: string; kind: string; status: string; packageCount: number; packages: string; holdReason: string; createdAtUtc: string }[];
  readiness: {
    id: string;
    status: string;
    environmentName: string;
    holdCount: number;
    failCount: number;
    holdReason: string;
    checks: { code: string; title: string; outcome: string; detail: string }[];
  } | null;
  audits: { id: string; action: string; detail: string; createdAtUtc: string }[];
};

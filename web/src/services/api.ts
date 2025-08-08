interface LoginRequest {
  username: string;
  password: string;
}

interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
  invitationCode?: string;
}

interface RegisterResponse {
  accessToken: string;
  refreshToken: string;
  tokenType: string;
  expiresIn: number;
  user: {
    id: string;
    username: string;
    email: string;
    emailConfirmed: boolean;
    isActive: boolean;
    roleId: number;
    roleName: string;
    createdAt: string;
    modifiedAt: string;
  };
}

interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  tokenType: string;
  expiresIn: number;
  username: string;
  user?: {
    id: string;
    username: string;
    email?: string;
    firstName?: string;
    lastName?: string;
    avatar?: string;
    isActive?: boolean;
    emailConfirmed?: boolean;
    lastLoginAt?: string;
    provider?: string;
    providerId?: string;
    roleId?: number;
    roleName?: string;
    createdAt?: string;
    modifiedAt?: string;
    permissions?: string[];
  };
}

// User management types
interface User {
  id: string;
  username: string;
  email: string;
  displayName?: string;
  role: UserRole;
  roleName: string;
  isActive: boolean;
  isEmailVerified: boolean;
  lastLoginAt?: string;
  createdAt: string;
  updatedAt: string;
  avatar?: string;
  description?: string;
}

interface UserRole {
  id: string;
  name: string;
  displayName: string;
  permissions: string[];
  description?: string;
  isSystem: boolean;
}

interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  displayName?: string;
  roleId: string;
  description?: string;
}

interface UpdateUserRequest {
  username?: string;
  email?: string;
  displayName?: string;
  roleId?: string;
  isActive?: boolean;
  description?: string;

}

interface UsersRequest {
  page: number;
  pageSize: number;
  searchTerm?: string;
  roleId?: string;
  isEnabled?: boolean;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

interface UsersResponse {
  data: User[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Dashboard 相关类型定义
export interface DashboardResponse {
  totalApiKeys: number;
  activeApiKeys: number;
  totalAccounts: number;
  activeAccounts: number;
  rateLimitedAccounts: number;
  todayRequests: number;
  totalRequests: number;
  todayInputTokens: number;
  todayOutputTokens: number;
  todayCacheCreateTokens: number;
  todayCacheReadTokens: number;
  totalInputTokens: number;
  totalOutputTokens: number;
  totalCacheCreateTokens: number;
  totalCacheReadTokens: number;
  realtimeRPM: number;
  realtimeTPM: number;
  metricsWindow: number;
  isHistoricalMetrics: boolean;
  systemStatus: string;
  uptimeSeconds: number;
}

export interface CostDataResponse {
  todayCosts: {
    totalCost: number;
    formatted: {
      totalCost: string;
    };
  };
  totalCosts: {
    totalCost: number;
    formatted: {
      totalCost: string;
    };
  };
}

export interface UptimeResponse {
  uptimeSeconds: number;
  uptimeText: string;
  startTime: string;
}

export interface ModelStatistics {
  model: string;
  requests: number;
  allTokens: number;
  cost: number;
  formatted?: {
    total: string;
  };
}

export interface TrendDataPoint {
  date?: string;
  hour?: string;
  label?: string;
  inputTokens: number;
  outputTokens: number;
  cacheCreateTokens: number;
  cacheReadTokens: number;
  requests: number;
  cost: number;
}

export interface DateFilterRequest {
  type: 'preset' | 'custom';
  preset?: string;
  customRange?: string[];
  startTime?: string;
  endTime?: string;
}

export interface TrendDataRequest {
  granularity: 'day' | 'hour';
  dateFilter?: DateFilterRequest;
}

export interface ApiKeysTrendRequest {
  metric: 'requests' | 'tokens';
  granularity: 'day' | 'hour';
  dateFilter?: DateFilterRequest;
}

export interface TopApiKeyInfo {
  id: string;
  name: string;
  usage: number;
  cost: number;
}

export interface ApiKeysTrendResponse {
  data: any[];
  topApiKeys: TopApiKeyInfo[];
  totalApiKeys: number;
}

interface ApiKey {
  id: string;
  name: string;
  keyValue: string;
  description?: string;
  tags?: string[];
  tokenLimit?: number;
  rateLimitWindow?: number;
  rateLimitRequests?: number;
  concurrencyLimit: number;
  dailyCostLimit: number;
  monthlyCostLimit: number;
  totalCostLimit: number;
  dailyCostUsed: number;
  monthlyCostUsed: number;
  expiresAt?: string;
  permissions: string;
  claudeAccountId?: string;
  claudeConsoleAccountId?: string;
  geminiAccountId?: string;
  enableModelRestriction: boolean;
  restrictedModels?: string[];
  enableClientRestriction: boolean;
  allowedClients?: string[];
  isEnabled: boolean;
  lastUsedAt?: string;
  totalUsageCount: number;
  totalCost: number;
  model?: string;
  service: string;
  createdAt: string;
  updatedAt: string;
}

export interface CostUsageInfo {
  dailyUsage: number;
  monthlyUsage: number;
  totalUsage: number;
  dailyCostUsed: number;
  dailyCostLimit: number;
  monthlyCostUsed: number;
  monthlyCostLimit: number;
  totalCostUsed: number;
  totalCostLimit: number;
}

interface ProxyConfig {
  enabled: boolean;
  type: 'socks5' | 'http' | 'https';
  host: string;
  port: string;
  username?: string;
  password?: string;
}

interface Account {
  id: string;
  name: string;
  platform: 'claude' | 'claude-console' | 'gemini' | 'openai' | 'thor';
  sessionKey?: string;
  isEnabled: boolean;
  status: 'active' | 'rate_limited' | 'error' | 'disabled';
  lastUsedAt?: string;
  rateLimitedUntil?: string;
  lastError?: string;
  createdAt: string;
  updatedAt: string;
  description?: string;
  accountType?: 'shared' | 'dedicated';
  projectId?: string;
  priority?: number;
  apiUrl?: string;
  apiKey?: string;
  baseUrl?: string;
  supportedModels?: Record<string, string>;
  userAgent?: string;
  rateLimitDuration?: number;
  proxy?: {
    type: string;
    host: string;
    port: number;
    username?: string;
    password?: string;
  };
}

interface OAuthTokenInfo {
  claudeAiOauth?: {
    accessToken: string;
    refreshToken?: string;
    expiresAt: number;
    scopes: string[];
  };
  geminiOauth?: {
    access_token: string;
    refresh_token?: string;
    scope: string;
    token_type: string;
    expiry_date: number;
  };
  tokens?: any;
  // Thor platform support
  apiKey?: string;
  baseUrl?: string;
}

interface AuthUrlResponse {
  authUrl: string;
  sessionId: string;
}

class ApiService {
  private baseURL = '/api';
  private token: string | null = null;

  constructor() {
    this.token = localStorage.getItem('token');
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseURL}${endpoint}`;
    const headers = {
      'Content-Type': 'application/json',
      ...(this.token && { Authorization: `Bearer ${this.token}` }),
      ...options.headers,
    };

    const response = await fetch(url, {
      ...options,
      headers,
    });

    if (!response.ok) {
      const result = await response.json();
      if (result.message) {
        throw new Error(result.message);
      } else {
        throw new Error(response.statusText);
      }
    }

    // 对于204 No Content响应，不尝试解析JSON
    if (response.status === 204) {
      return undefined as T;
    }

    // 检查响应是否有内容
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      const result = await response.json();
      if (result.success && result.data) {
        return result.data;
      } else {
        return result;
      }
    }

    return undefined as T;
  }

  // Auth
  async login(data: LoginRequest): Promise<LoginResponse> {
    try {
      const response = await this.request<LoginResponse>('/auth/login', {
        method: 'POST',
        body: JSON.stringify(data),
      });

      if (response.accessToken) {
        this.token = response.accessToken;
        localStorage.setItem('token', response.accessToken);
      }

      return response;
    } catch (error) {
      throw error;
    }
  }

  async register(data: RegisterRequest): Promise<RegisterResponse> {
    try {
      const response = await this.request<RegisterResponse>('/auth/register', {
        method: 'POST',
        body: JSON.stringify(data),
      });

      return response;
    } catch (error) {
      throw error;
    }
  }

  logout() {
    this.token = null;
    localStorage.removeItem('token');
  }

  isAuthenticated(): boolean {
    return !!this.token;
  }

  // API Keys
  async getApiKeys(): Promise<ApiKey[]> {
    try {
      return this.request<ApiKey[]>('/apikeys');
    } catch (error) {
      throw error;
    }
  }

  async createApiKey(data: any): Promise<ApiKey> {
    return this.request<ApiKey>('/apikeys', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async updateApiKey(id: string, data: any): Promise<ApiKey> {
    return this.request<ApiKey>(`/apikeys/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async enableApiKey(id: string): Promise<void> {
    return this.request<void>(`/apikeys/${id}/enable`, {
      method: 'PATCH',
    });
  }

  async disableApiKey(id: string): Promise<void> {
    return this.request<void>(`/apikeys/${id}/disable`, {
      method: 'PATCH',
    });
  }

  async toggleApiKeyEnabled(id: string): Promise<void> {
    return this.request<void>(`/apikeys/${id}/toggle`, {
      method: 'PATCH',
    });
  }

  async deleteApiKey(id: string): Promise<void> {
    return this.request<void>(`/apikeys/${id}`, {
      method: 'DELETE',
    });
  }

  async getApiKeyUsage(id: string): Promise<CostUsageInfo> {
    return this.request<CostUsageInfo>(`/apikeys/${id}/usage`);
  }

  // Accounts
  async getAccounts(): Promise<Account[]> {
    try {
      return this.request<Account[]>('/accounts');
    } catch (error) {
      // Mock data for demo
      return [
        {
          id: '1',
          name: 'Claude Production Account',
          platform: 'claude',
          sessionKey: 'sk_live_1234567890abcdef1234567890abcdef',
          isEnabled: true,
          status: 'active',
          lastUsedAt: new Date(Date.now() - 3600000).toISOString(), // 1 hour ago
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        },
        {
          id: '2',
          name: 'Gemini Test Account',
          platform: 'gemini',
          sessionKey: 'AIzaSyA1234567890abcdef1234567890abcdef',
          isEnabled: true,
          status: 'rate_limited',
          rateLimitedUntil: new Date(Date.now() + 1800000).toISOString(), // 30 minutes from now
          lastError: 'Rate limit exceeded. Please wait before making more requests.',
          lastUsedAt: new Date(Date.now() - 7200000).toISOString(), // 2 hours ago
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        }
      ];
    }
  }

  async createAccount(data: {
    name: string;
    platform: string;
    sessionKey?: string;
    description?: string;
    accountType?: 'shared' | 'dedicated';
    projectId?: string;
    priority?: number;
    apiUrl?: string;
    apiKey?: string;
    supportedModels?: Record<string, string>;
    userAgent?: string;
    rateLimitDuration?: number;
    proxy?: any;
    claudeAiOauth?: any;
    geminiOauth?: any;
  }): Promise<Account> {
    return this.request<Account>('/accounts/openai', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async createClaudeAccount(data: any): Promise<Account> {
    return this.request<Account>('/accounts/claude', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async createClaudeConsoleAccount(data: any): Promise<Account> {
    return this.request<Account>('/accounts/claude-console', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async createGeminiAccount(data: any): Promise<Account> {
    return this.request<Account>('/accounts/gemini', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async updateClaudeAccount(id: string, data: any): Promise<Account> {
    return this.request<Account>(`/accounts/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async updateClaudeConsoleAccount(id: string, data: any): Promise<Account> {
    return this.request<Account>(`/accounts/claude-console/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async updateGeminiAccount(id: string, data: any): Promise<Account> {
    return this.request<Account>(`/accounts/gemini/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async updateAccount(id: string, data: {
    name?: string;
    platform?: string;
    sessionKey?: string;
    isEnabled?: boolean;
  }): Promise<Account> {
    return this.request<Account>(`/accounts/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async enableAccount(id: string): Promise<void> {
    return this.request<void>(`/accounts/${id}/enable`, {
      method: 'PATCH',
    });
  }

  async disableAccount(id: string): Promise<void> {
    return this.request<void>(`/accounts/${id}/disable`, {
      method: 'PATCH',
    });
  }

  async toggleAccountEnabled(id: string): Promise<void> {
    return this.request<void>(`/accounts/${id}/toggle`, {
      method: 'PATCH',
    });
  }

  async deleteAccount(id: string): Promise<void> {
    return this.request<void>(`/accounts/${id}`, {
      method: 'DELETE',
    });
  }

  // Dashboard APIs
  async getDashboardData(): Promise<DashboardResponse> {
    return this.request<DashboardResponse>('/dashboard');
  }

  async getCostData(): Promise<CostDataResponse> {
    return this.request<CostDataResponse>('/dashboard/costs');
  }

  async getSystemUptime(): Promise<UptimeResponse> {
    return this.request<UptimeResponse>('/dashboard/uptime');
  }

  async getModelStatistics(dateFilter?: DateFilterRequest): Promise<ModelStatistics[]> {
    return this.request<ModelStatistics[]>('/dashboard/model-statistics', {
      method: 'POST',
      body: JSON.stringify(dateFilter || {}),
    });
  }

  async getTrendData(request: TrendDataRequest): Promise<TrendDataPoint[]> {
    return this.request<TrendDataPoint[]>('/dashboard/trend-data', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getApiKeysTrend(request: ApiKeysTrendRequest): Promise<ApiKeysTrendResponse> {
    return this.request<ApiKeysTrendResponse>('/dashboard/apikeys-trend', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getAvailableAccounts(platform?: string): Promise<Account[]> {
    const query = platform ? `?platform=${platform}` : '';
    return this.request<Account[]>(`/accounts/available${query}`);
  }

  // OAuth Methods
  async generateClaudeAuthUrl(data?: { proxy?: any }): Promise<AuthUrlResponse> {
    return this.request<AuthUrlResponse>('/claude-proxy/auth/generate-url', {
      method: 'POST',
      body: JSON.stringify(data || {}),
    });
  }

  async generateGeminiAuthUrl(data?: { proxy?: any }): Promise<AuthUrlResponse> {
    return this.request<AuthUrlResponse>('/auth/gemini/generate-url', {
      method: 'POST',
      body: JSON.stringify(data || {}),
    });
  }

  async exchangeClaudeCode(data: {
    sessionId: string;
    callbackUrl: string;
    proxy?: any;
  }): Promise<any> {
    return this.request<any>('/claude-proxy/auth/exchange-code', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async exchangeGeminiCode(data: {
    code: string;
    sessionId: string;
    proxy?: any;
  }): Promise<any> {
    return this.request<any>('/auth/gemini/exchange-code', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  // Request Logs APIs
  async getRequestLogs(request: RequestLogsRequest): Promise<RequestLogsResponse> {
    return this.request<RequestLogsResponse>('/dashboard/request-logs', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getRequestLogDetail(id: string): Promise<RequestLogDetail> {
    return this.request<RequestLogDetail>(`/dashboard/request-logs/${id}`);
  }

  async getRequestStatusStats(dateFilter?: DateFilterRequest): Promise<RequestStatusStat[]> {
    return this.request<RequestStatusStat[]>('/dashboard/request-status-stats', {
      method: 'POST',
      body: JSON.stringify(dateFilter || {}),
    });
  }

  async getRealtimeRequests(minutes: number = 10): Promise<RealtimeRequestsResponse> {
    return this.request<RealtimeRequestsResponse>(`/dashboard/realtime-requests?minutes=${minutes}`);
  }

  async getApiKeyModelFlowData(dateFilter?: DateFilterRequest): Promise<ApiKeyModelFlowData[]> {
    return this.request<ApiKeyModelFlowData[]>('/dashboard/apikey-model-flow', {
      method: 'POST',
      body: JSON.stringify(dateFilter || {}),
    });
  }

  // Pricing APIs
  async getModelPricing(): Promise<ModelPricing[]> {
    const response = await this.request<{ data: ModelPricing[] }>('/pricing/models');
    return response.data;
  }

  async updateModelPricing(pricing: ModelPricing): Promise<void> {
    await this.request<void>('/pricing/models', {
      method: 'PUT',
      body: JSON.stringify(pricing),
    });
  }

  async enableModel(modelName: string): Promise<void> {
    await this.request<void>(`/pricing/models/${encodeURIComponent(modelName)}/enable`, {
      method: 'POST',
    });
  }

  async disableModel(modelName: string): Promise<void> {
    await this.request<void>(`/pricing/models/${encodeURIComponent(modelName)}/disable`, {
      method: 'POST',
    });
  }

  async getExchangeRates(): Promise<ExchangeRate[]> {
    const response = await this.request<{ data: ExchangeRate[] }>('/pricing/exchange-rates');
    return response.data;
  }

  async updateExchangeRate(fromCurrency: string, toCurrency: string, rate: number): Promise<void> {
    await this.request<void>('/pricing/exchange-rates', {
      method: 'PUT',
      body: JSON.stringify({
        fromCurrency,
        toCurrency,
        rate
      }),
    });
  }

  async calculateCost(request: CalculateCostRequest): Promise<PricingResult> {
    const response = await this.request<{ data: PricingResult }>('/pricing/calculate', {
      method: 'POST',
      body: JSON.stringify(request),
    });
    return response.data;
  }

  // User Management APIs
  async getUsers(pageIndex: number = 1, pageSize: number = 20): Promise<UsersResponse> {
    try {
      const query = new URLSearchParams();
      query.append('pageIndex', pageIndex.toString());
      query.append('pageSize', pageSize.toString());
      
      const response = await this.request<any>(`/users?${query.toString()}`);
      
      // The backend returns PagedResult<UserDto> structure
      return {
        data: response.items || response,
        total: response.totalCount || response.length,
        page: response.pageIndex || pageIndex,
        pageSize: response.pageSize || pageSize,
        totalPages: response.totalPages || Math.ceil((response.totalCount || response.length) / pageSize)
      };
    } catch (error) {
      throw error;
    }
  }

  async getUserById(id: string): Promise<User> {
    return this.request<User>(`/users/${id}`);
  }

  async createUser(data: CreateUserRequest): Promise<User> {
    return this.request<User>('/users', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async updateUser(id: string, data: UpdateUserRequest): Promise<User> {
    return this.request<User>(`/users/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async deleteUser(id: string): Promise<void> {
    return this.request<void>(`/users/${id}`, {
      method: 'DELETE',
    });
  }

  async enableUser(id: string): Promise<void> {
    return this.request<void>(`/users/${id}/enable`, {
      method: 'PATCH',
    });
  }

  async disableUser(id: string): Promise<void> {
    return this.request<void>(`/users/${id}/disable`, {
      method: 'PATCH',
    });
  }

  async toggleUserEnabled(id: string): Promise<void> {
    return this.request<void>(`/users/${id}/toggle`, {
      method: 'PATCH',
    });
  }

  async resetUserPassword(id: string, newPassword: string): Promise<void> {
    return this.request<void>(`/users/${id}/reset-password`, {
      method: 'PATCH',
      body: JSON.stringify({ newPassword }),
    });
  }

  // Role Management APIs
  async getRoles(): Promise<UserRole[]> {
    try {
      return this.request<UserRole[]>('/roles');
    } catch (error) {
      throw error;
    }
  }

  async createRole(data: { name: string; displayName: string; permissions: string[]; description?: string }): Promise<UserRole> {
    return this.request<UserRole>('/roles', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async updateRole(id: string, data: { displayName?: string; permissions?: string[]; description?: string }): Promise<UserRole> {
    return this.request<UserRole>(`/roles/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async deleteRole(id: string): Promise<void> {
    return this.request<void>(`/roles/${id}`, {
      method: 'DELETE',
    });
  }

  // Profile Dashboard API
  async getProfileDashboard(): Promise<ProfileDashboard> {
    return this.request<ProfileDashboard>('/profile/dashboard');
  }

  // Redeem Code APIs
  async useRedeemCode(code: string): Promise<RedeemCodeUseResult> {
    return this.request<RedeemCodeUseResult>('/redeem-codes/use', {
      method: 'POST',
      body: JSON.stringify({ code }),
    });
  }

  async getMyRedeemRecords(page: number = 1, pageSize: number = 20): Promise<{ success: boolean; data: RedeemRecord[] }> {
    return this.request<{ success: boolean; data: RedeemRecord[] }>(`/redeem-codes/my-records?page=${page}&pageSize=${pageSize}`);
  }

  // Admin Redeem Code APIs
  async createRedeemCodes(request: CreateRedeemCodeRequest): Promise<RedeemCode[]> {
    return this.request<RedeemCode[]>('/admin/redeem-codes', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getRedeemCodeList(request: RedeemCodeListRequest): Promise<any> {
    return this.request<any>('/admin/redeem-codes/list', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async updateRedeemCodeStatus(id: string, isEnabled: boolean): Promise<{ success: boolean; message: string }> {
    return this.request<{ success: boolean; message: string }>(`/admin/redeem-codes/${id}/status`, {
      method: 'PUT',
      body: JSON.stringify({ isEnabled }),
    });
  }

  async deleteRedeemCode(id: string): Promise<{ success: boolean; message: string }> {
    return this.request<{ success: boolean; message: string }>(`/admin/redeem-codes/${id}`, {
      method: 'DELETE',
    });
  }

  async getRedeemCodeStats(): Promise<RedeemCodeStats> {
    return this.request<RedeemCodeStats>('/admin/redeem-codes/stats');
  }

  // Invitation APIs
  async getInvitationStats(): Promise<InvitationStatsDto> {
    return this.request<InvitationStatsDto>('/invitation/stats');
  }

  async getInvitationLink(): Promise<{ invitationLink: string }> {
    return this.request<{ invitationLink: string }>('/invitation/link');
  }

  async getInvitationRecords(): Promise<InvitationRecordDto[]> {
    return this.request<InvitationRecordDto[]>('/invitation/records');
  }

  // Invitation Settings APIs (Admin only)
  async getInvitationSettings(): Promise<InvitationSettings> {
    return this.request<InvitationSettings>('/invitation/settings');
  }

  async updateInvitationSettings(settings: UpdateInvitationSettingsRequest): Promise<void> {
    return this.request<void>('/invitation/settings', {
      method: 'PUT',
      body: JSON.stringify(settings),
    });
  }

  // Announcements
  async getAnnouncements(): Promise<any[]> {
    return this.request<any[]>('/announcements');
  }

  async getCurrentAnnouncements(): Promise<any[]> {
    return this.request<any[]>('/announcements/current');
  }

  async createAnnouncement(data: any): Promise<any> {
    return this.request<any>('/announcements', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async updateAnnouncement(id: number, data: any): Promise<any> {
    return this.request<any>(`/announcements/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async deleteAnnouncement(id: number): Promise<void> {
    return this.request<void>(`/announcements/${id}`, {
      method: 'DELETE',
    });
  }

  async toggleAnnouncementVisibility(id: number): Promise<any> {
    return this.request<any>(`/announcements/${id}/toggle`, {
      method: 'PATCH',
    });
  }
}

// Dashboard Response Types
export interface DashboardResponse {
  totalApiKeys: number;
  activeApiKeys: number;
  totalAccounts: number;
  activeAccounts: number;
  rateLimitedAccounts: number;
  todayRequests: number;
  totalRequests: number;
  todayInputTokens: number;
  todayOutputTokens: number;
  todayCacheCreateTokens: number;
  todayCacheReadTokens: number;
  totalInputTokens: number;
  totalOutputTokens: number;
  totalCacheCreateTokens: number;
  totalCacheReadTokens: number;
  realtimeRPM: number;
  realtimeTPM: number;
  metricsWindow: number;
  isHistoricalMetrics: boolean;
  systemStatus: string;
  uptimeSeconds: number;
}

export interface CostDataResponse {
  todayCosts: CostInfo;
  totalCosts: CostInfo;
}

export interface CostInfo {
  totalCost: number;
  formatted: FormattedCost;
}

export interface FormattedCost {
  totalCost: string;
}

export interface UptimeResponse {
  uptimeSeconds: number;
  uptimeText: string;
  startTime: string;
}

export interface ModelStatistics {
  model: string;
  requests: number;
  allTokens: number;
  cost: number;
  formatted?: FormattedModelCost;
}

export interface FormattedModelCost {
  total: string;
}

export interface TrendDataRequest {
  granularity: 'day' | 'hour';
  dateFilter?: DateFilterRequest;
}

export interface TrendDataPoint {
  date?: string;
  hour?: string;
  label?: string;
  inputTokens: number;
  outputTokens: number;
  cacheCreateTokens: number;
  cacheReadTokens: number;
  requests: number;
  cost: number;
}

export interface DateFilterRequest {
  type: 'preset' | 'custom';
  preset?: string;
  customRange?: string[];
  startTime?: string;
  endTime?: string;
}

export interface ApiKeysTrendRequest {
  metric: 'requests' | 'tokens';
  granularity: 'day' | 'hour';
  dateFilter?: DateFilterRequest;
}


export interface ApiKeyMetric {
  name: string;
  requests: number;
  tokens: number;
  cost: number;
  formattedCost: string;
}

// Request Log Types
export interface RequestLogSummary {
  id: string;
  apiKeyId: string;
  apiKeyName: string;
  accountId?: string;
  accountName?: string;
  model: string;
  platform: string;
  requestStartTime: string;
  requestEndTime?: string;
  durationMs?: number;
  status: string;
  errorMessage?: string;
  httpStatusCode?: number;
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
  cost: number;
  isStreaming: boolean;
  clientIp?: string;
  requestId?: string;
}

export interface RequestLogsRequest {
  page: number;
  pageSize: number;
  dateFilter?: DateFilterRequest;
  apiKeyId?: string;
  status?: string;
  model?: string;
  platform?: string;
  searchTerm?: string;
  sortBy: string;
  sortDirection: string;
}

export interface RequestLogsResponse {
  data: RequestLogSummary[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface RequestLogDetail {
  id: string;
  apiKeyId: string;
  apiKeyName: string;
  accountId?: string;
  accountName?: string;
  model: string;
  platform: string;
  requestStartTime: string;
  requestEndTime?: string;
  durationMs?: number;
  status: string;
  errorMessage?: string;
  httpStatusCode?: number;
  inputTokens: number;
  outputTokens: number;
  cacheCreateTokens: number;
  cacheReadTokens: number;
  totalTokens: number;
  cost: number;
  isStreaming: boolean;
  clientIp?: string;
  userAgent?: string;
  requestId?: string;
  metadata?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface RequestStatusStat {
  status: string;
  count: number;
  totalTokens: number;
  totalCost: number;
  averageDurationMs: number;
}

export interface RealtimeRequestSummary {
  id: string;
  apiKeyName: string;
  model: string;
  platform: string;
  requestStartTime: string;
  status: string;
  durationMs?: number;
  totalTokens: number;
  cost: number;
  errorMessage?: string;
}

export interface RealtimeStats {
  totalRequests: number;
  successRequests: number;
  successRate: number;
  totalTokens: number;
  averageResponseTimeMs: number;
  requestsPerMinute: number;
}

export interface RealtimeRequestsResponse {
  recentRequests: RealtimeRequestSummary[];
  windowMinutes: number;
  stats: RealtimeStats;
}

export interface ApiKeyModelFlowData {
  apiKeyId: string;
  apiKeyName: string;
  model: string;
  requests: number;
  tokens: number;
  cost: number;
}

// Pricing Types
export interface ModelPricing {
  model: string;
  inputPrice: number;
  outputPrice: number;
  cacheWritePrice: number;
  cacheReadPrice: number;
  currency: string;
  description?: string;
  isEnabled?: boolean;
}

export interface ExchangeRate {
  fromCurrency: string;
  toCurrency: string;
  rate: number;
  updatedAt: string;
}

export interface CalculateCostRequest {
  model: string;
  inputTokens: number;
  outputTokens: number;
  cacheCreateTokens: number;
  cacheReadTokens: number;
  targetCurrency?: string;
}

export interface PricingResult {
  model: string;
  currency: string;
  inputCost: number;
  outputCost: number;
  cacheCreateCost: number;
  cacheReadCost: number;
  totalCost: number;
  weightedTokens: number;
  unitPrice: number;
}

// Personal Dashboard Types
export interface ProfileDashboard {
  userId: number;
  wallet: WalletStatistics;
  requests: UserRequestStatistics;
  apiKeyCount: number;
  activeApiKeyCount: number;
  lastUpdateTime: string;
}

export interface WalletStatistics {
  userId: number;
  currentBalance: number;
  totalRecharged: number;
  totalUsed: number;
  recentTransactionCount: number;
  dailyAverageUsage: number;
  lastUsedAt?: string;
  lastRechargedAt?: string;
}

export interface UserRequestStatistics {
  userId: number;
  startDate: string;
  endDate: string;
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  successRate: number;
  totalTokens: number;
  totalCost: number;
  averageResponseTime: number;
  modelUsage: ModelUsage[];
  dailyUsage: DailyUsage[];
}

export interface ModelUsage {
  model: string;
  requestCount: number;
  totalTokens: number;
  totalCost: number;
}

export interface DailyUsage {
  date: string;
  requestCount: number;
  totalTokens: number;
  totalCost: number;
  successfulRequests: number;
  failedRequests: number;
}

// Redeem Code Types
export interface RedeemCode {
  id: string;
  code: string;
  type: string;
  amount: number;
  description?: string;
  isUsed: boolean;
  usedByUserId?: string;
  usedByUserName?: string;
  usedAt?: string;
  expiresAt?: string;
  isEnabled: boolean;
  createdByUserId: string;
  createdByUserName: string;
  createdAt: string;
  modifiedAt?: string;
}

export interface CreateRedeemCodeRequest {
  type: string;
  amount: number;
  description?: string;
  expiresAt?: string;
  count: number;
}

export interface RedeemCodeUseResult {
  success: boolean;
  message: string;
  amount: number;
  type: string;
  newBalance: number;
}

export interface RedeemCodeListRequest {
  page: number;
  pageSize: number;
  code?: string;
  type?: string;
  isUsed?: boolean;
  isEnabled?: boolean;
  createdByUserId?: string;
  usedByUserId?: string;
  startDate?: string;
  endDate?: string;
  sortBy: string;
  sortDirection: string;
}

export interface RedeemCodeListResponse {
  data: RedeemCode[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface RedeemRecord {
  id: string;
  code: string;
  type: string;
  amount: number;
  description: string;
  usedAt: string;
}

export interface RedeemCodeStats {
  totalCodes: number;
  usedCodes: number;
  unusedCodes: number;
  expiredCodes: number;
  totalRedeemedAmount: number;
  usageRate: number;
}

// Invitation Types
export interface InvitationStatsDto {
  totalInvited: number;
  maxInvitations: number;
  totalReward: number;
  invitationLink: string;
}

export interface InvitationRecordDto {
  id: string;
  invitedUsername: string;
  invitedEmail: string;
  invitedAt: string; // ISO date string
  inviterReward: number;
  invitedReward: number;
  rewardProcessed: boolean;
  notes?: string;
}

export interface InvitationSettings {
  inviterReward: number;
  invitedReward: number;
  maxInvitations: number;
  invitationEnabled: boolean;
}

export interface UpdateInvitationSettingsRequest {
  inviterReward: number;
  invitedReward: number;
  maxInvitations: number;
  invitationEnabled: boolean;
}

export const apiService = new ApiService();
export type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
  ApiKey,
  Account,
  ProxyConfig,
  OAuthTokenInfo,
  AuthUrlResponse,
  User,
  UserRole,
  CreateUserRequest,
  UpdateUserRequest,
  UsersRequest,
  UsersResponse
};
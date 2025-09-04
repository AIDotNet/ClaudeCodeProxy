import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { ThemeToggle } from "@/components/ui/theme-toggle";
import { Search, Key, DollarSign, Activity, AlertCircle, Link, Clock, Shield, CheckCircle, XCircle, Pause, Trash2, RefreshCw, Timer } from "lucide-react";

interface QuotaInfo {
  dailyCostLimit?: number;
  dailyCostUsed?: number;
  dailyAvailable?: number;
  monthlyCostLimit?: number;
  monthlyCostUsed?: number;
  monthlyAvailable?: number;
  totalCostLimit?: number;
  totalCostUsed?: number;
  totalAvailable?: number;
  organization?: {
    name: string;
    id: string;
  };
  accountBinding?: {
    isBound: boolean;
    accountName?: string;
    accountId?: string;
    rateLimiting?: {
      isEnabled: boolean;
      requestsPerMinute?: number;
      requestsPerHour?: number;
      requestsPerDay?: number;
      currentUsage?: {
        minute: number;
        hour: number;
        day: number;
      };
      resetTimes?: {
        minute: string;
        hour: string;
        day: string;
      };
    };
    status?: 'active' | 'suspended' | 'expired';
    createdAt?: string;
    expiresAt?: string;
    rateLimitedUntil?: string;
  };
}

const CACHE_KEY = 'quota_query_cache';

interface CacheData {
  apiKey: string;
  quotaInfo: QuotaInfo;
  timestamp: number;
}

export default function QuotaQueryPage() {
  const [apiKey, setApiKey] = useState('');
  const [loading, setLoading] = useState(false);
  const [quotaInfo, setQuotaInfo] = useState<QuotaInfo | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [hasCachedData, setHasCachedData] = useState(false);
  const [autoRefresh, setAutoRefresh] = useState(false);

  // Load cached API key on component mount
  useEffect(() => {
    const loadCachedApiKey = () => {
      try {
        const cached = localStorage.getItem(CACHE_KEY);
        if (cached) {
          const cacheData: CacheData = JSON.parse(cached);
          
          if (cacheData.apiKey) {
            setApiKey(cacheData.apiKey);
            setHasCachedData(true);
            // Auto query with cached API key
            setTimeout(() => queryQuota(cacheData.apiKey), 100);
          }
        }
      } catch (err) {
        console.error('Failed to load cached data:', err);
        localStorage.removeItem(CACHE_KEY);
      }
    };

    loadCachedApiKey();
  }, []);

  // Auto refresh when user stays on page for long time
  useEffect(() => {
    let refreshInterval: NodeJS.Timeout;
    
    if (quotaInfo && apiKey.trim()) {
      // Start auto refresh after 5 minutes, then every 5 minutes
      refreshInterval = setInterval(() => {
        if (!loading) {
          setAutoRefresh(true);
          queryQuota(undefined, true).finally(() => {
            setTimeout(() => setAutoRefresh(false), 1000);
          });
        }
      }, 5 * 60 * 1000); // 5 minutes
    }

    return () => {
      if (refreshInterval) {
        clearInterval(refreshInterval);
      }
    };
  }, [quotaInfo, apiKey, loading]);

  const saveApiKeyToCache = (apiKey: string) => {
    try {
      const cacheData: CacheData = {
        apiKey,
        quotaInfo: {}, // Don't cache quota info
        timestamp: Date.now() // Keep timestamp for potential future use
      };
      localStorage.setItem(CACHE_KEY, JSON.stringify(cacheData));
      setHasCachedData(true);
    } catch (err) {
      console.error('Failed to save to cache:', err);
    }
  };

  const clearCache = () => {
    try {
      localStorage.removeItem(CACHE_KEY);
      setHasCachedData(false);
      setApiKey('');
      setQuotaInfo(null);
      setError(null);
    } catch (err) {
      console.error('Failed to clear cache:', err);
    }
  };

  const queryQuota = async (keyToQuery?: string, isAutoRefresh = false) => {
    const currentKey = keyToQuery || apiKey;
    if (!currentKey.trim()) {
      setError('请输入API Key');
      return;
    }

    setLoading(true);
    setError(null);
    // Don't clear quota info during auto refresh to avoid flashing
    if (!isAutoRefresh) {
      setQuotaInfo(null);
    }

    try {
      // 这里调用后端API查询额度
      const response = await fetch('/api/quota/query', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ apiKey: currentKey.trim() }),
      });

      if (!response.ok) {
        throw new Error('查询失败，请检查API Key是否正确');
      }

      const data = await response.json();
      setQuotaInfo(data);
      
      // Save API key to cache (not quota results)
      saveApiKeyToCache(currentKey.trim());
    } catch (err) {
      setError(err instanceof Error ? err.message : '查询失败');
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount?: number) => {
    if (amount === undefined || amount === null) return 'N/A';
    return `$${amount.toFixed(2)}`;
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleString('zh-CN');
  };

  const getStatusBadge = (status?: string) => {
    switch (status) {
      case 'active':
        return <Badge variant="default" className="bg-green-100 text-green-800"><CheckCircle className="h-3 w-3 mr-1" />正常</Badge>;
      case 'suspended':
        return <Badge variant="destructive"><Pause className="h-3 w-3 mr-1" />暂停</Badge>;
      case 'expired':
        return <Badge variant="secondary"><XCircle className="h-3 w-3 mr-1" />过期</Badge>;
      default:
        return <Badge variant="outline">未知</Badge>;
    }
  };

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <div className="border-b">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Key className="h-6 w-6 text-primary" />
              <h1 className="text-xl font-semibold">API Key 额度查询</h1>
            </div>
            <ThemeToggle />
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="container mx-auto px-4 py-8">
        <div className="max-w-7xl mx-auto space-y-6">
          {/* Query Form */}
          <div className="max-w-3xl mx-auto">
            <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Search className="h-5 w-5" />
                查询API Key额度
              </CardTitle>
              <CardDescription>
                输入您的API Key来查询当前的使用额度和余额信息
                <div className="flex flex-col gap-1 mt-2">
                  {hasCachedData && (
                    <div className="flex items-center gap-1 text-blue-600 text-sm">
                      <CheckCircle className="h-3 w-3" />
                      已缓存API Key
                    </div>
                  )}
                  {quotaInfo && (
                    <div className="flex items-center gap-1 text-green-600 text-sm">
                      <RefreshCw className={`h-3 w-3 ${autoRefresh ? 'animate-spin' : ''}`} />
                      {autoRefresh ? '正在自动刷新...' : '每5分钟自动刷新'}
                    </div>
                  )}
                </div>
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="apiKey">API Key</Label>
                <Input
                  id="apiKey"
                  type="password"
                  placeholder="请输入您的API Key"
                  value={apiKey}
                  onChange={(e) => setApiKey(e.target.value)}
                  disabled={loading}
                />
              </div>
              <div className="flex gap-2">
                <Button 
                  onClick={() => queryQuota()} 
                  disabled={loading || !apiKey.trim()}
                  className="flex-1"
                >
                {loading ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                    查询中...
                  </>
                ) : (
                  <>
                    <Search className="h-4 w-4 mr-2" />
                    查询额度
                  </>
                )}
                </Button>
                {hasCachedData && (
                  <Button 
                    variant="outline" 
                    size="default"
                    onClick={clearCache}
                    disabled={loading}
                    className="px-3"
                    title="清除缓存的API Key"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                )}
              </div>
            </CardContent>
            </Card>
          </div>

          {/* Error Message */}
          {error && (
            <div className="max-w-3xl mx-auto">
              <Card className="border-destructive">
              <CardContent className="pt-6">
                <div className="flex items-center gap-2 text-destructive">
                  <AlertCircle className="h-5 w-5" />
                  <span>{error}</span>
                </div>
              </CardContent>
              </Card>
            </div>
          )}

          {/* Loading Skeleton */}
          {loading && (
            <div className="space-y-4">
              <Card>
                <CardHeader>
                  <Skeleton className="h-6 w-32" />
                  <Skeleton className="h-4 w-48" />
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    {[1, 2, 3].map((i) => (
                      <div key={i} className="space-y-2">
                        <Skeleton className="h-4 w-16" />
                        <Skeleton className="h-8 w-24" />
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            </div>
          )}

          {/* Quota Results */}
          {quotaInfo && !loading && (
            <div className="space-y-4">
              {/* Organization Info */}
              {quotaInfo.organization && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Activity className="h-5 w-5" />
                      组织信息
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      <div>
                        <Label className="text-sm text-muted-foreground">组织名称</Label>
                        <p className="font-medium">{quotaInfo.organization.name}</p>
                      </div>
                      <div>
                        <Label className="text-sm text-muted-foreground">组织ID</Label>
                        <p className="font-mono text-sm">{quotaInfo.organization.id}</p>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Main Content Grid */}
              <div className="grid grid-cols-1 xl:grid-cols-5 lg:grid-cols-3 gap-6">
                {/* Left Column - Quota Details */}
                <div className="xl:col-span-3 lg:col-span-2">
                  <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <DollarSign className="h-5 w-5" />
                    额度详情
                  </CardTitle>
                  <CardDescription>
                    API Key的额度使用情况统计
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-6">
                  {/* Daily Quota */}
                  {(quotaInfo.dailyCostLimit !== undefined && quotaInfo.dailyCostLimit > 0) && (
                    <div>
                      <h4 className="font-medium mb-3 text-blue-600">每日额度</h4>
                      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                        <div className="space-y-1">
                          <Label className="text-xs text-muted-foreground">每日限制</Label>
                          <div className="text-lg font-semibold text-blue-600">
                            {formatCurrency(quotaInfo.dailyCostLimit)}
                          </div>
                        </div>
                        <div className="space-y-1">
                          <Label className="text-xs text-muted-foreground">今日已用</Label>
                          <div className="text-lg font-semibold text-orange-600">
                            {formatCurrency(quotaInfo.dailyCostUsed)}
                          </div>
                        </div>
                        <div className="space-y-1">
                          <Label className="text-xs text-muted-foreground">今日剩余</Label>
                          <div className="text-lg font-semibold text-green-600">
                            {formatCurrency(quotaInfo.dailyAvailable)}
                          </div>
                        </div>
                      </div>
                      {quotaInfo.dailyCostLimit > 0 && quotaInfo.dailyCostUsed !== undefined && (
                        <div className="mt-3">
                          <div className="flex justify-between text-xs mb-1">
                            <span>今日使用率</span>
                            <span>{((quotaInfo.dailyCostUsed / quotaInfo.dailyCostLimit) * 100).toFixed(1)}%</span>
                          </div>
                          <div className="w-full bg-gray-200 rounded-full h-1.5">
                            <div 
                              className="bg-blue-500 h-1.5 rounded-full transition-all duration-300"
                              style={{ 
                                width: `${Math.min(100, (quotaInfo.dailyCostUsed / quotaInfo.dailyCostLimit) * 100)}%`
                              }}
                            ></div>
                          </div>
                        </div>
                      )}
                    </div>
                  )}

                  {/* Monthly Quota */}
                  {(quotaInfo.monthlyCostLimit !== undefined && quotaInfo.monthlyCostLimit > 0) && (
                    <div>
                      <h4 className="font-medium mb-3 text-purple-600">月度额度</h4>
                      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                        <div className="space-y-1">
                          <Label className="text-xs text-muted-foreground">月度限制</Label>
                          <div className="text-lg font-semibold text-purple-600">
                            {formatCurrency(quotaInfo.monthlyCostLimit)}
                          </div>
                        </div>
                        <div className="space-y-1">
                          <Label className="text-xs text-muted-foreground">本月已用</Label>
                          <div className="text-lg font-semibold text-orange-600">
                            {formatCurrency(quotaInfo.monthlyCostUsed)}
                          </div>
                        </div>
                        <div className="space-y-1">
                          <Label className="text-xs text-muted-foreground">本月剩余</Label>
                          <div className="text-lg font-semibold text-green-600">
                            {formatCurrency(quotaInfo.monthlyAvailable)}
                          </div>
                        </div>
                      </div>
                      {quotaInfo.monthlyCostLimit > 0 && quotaInfo.monthlyCostUsed !== undefined && (
                        <div className="mt-3">
                          <div className="flex justify-between text-xs mb-1">
                            <span>本月使用率</span>
                            <span>{((quotaInfo.monthlyCostUsed / quotaInfo.monthlyCostLimit) * 100).toFixed(1)}%</span>
                          </div>
                          <div className="w-full bg-gray-200 rounded-full h-1.5">
                            <div 
                              className="bg-purple-500 h-1.5 rounded-full transition-all duration-300"
                              style={{ 
                                width: `${Math.min(100, (quotaInfo.monthlyCostUsed / quotaInfo.monthlyCostLimit) * 100)}%`
                              }}
                            ></div>
                          </div>
                        </div>
                      )}
                    </div>
                  )}

                  {/* Total Quota */}
                  {(quotaInfo.totalCostLimit !== undefined && quotaInfo.totalCostLimit > 0) && (
                    <div>
                      <h4 className="font-medium mb-3 text-red-600">总额度</h4>
                      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                        <div className="space-y-1">
                          <Label className="text-xs text-muted-foreground">总限制</Label>
                          <div className="text-lg font-semibold text-red-600">
                            {formatCurrency(quotaInfo.totalCostLimit)}
                          </div>
                        </div>
                        <div className="space-y-1">
                          <Label className="text-xs text-muted-foreground">总已用</Label>
                          <div className="text-lg font-semibold text-orange-600">
                            {formatCurrency(quotaInfo.totalCostUsed)}
                          </div>
                        </div>
                        <div className="space-y-1">
                          <Label className="text-xs text-muted-foreground">总剩余</Label>
                          <div className="text-lg font-semibold text-green-600">
                            {formatCurrency(quotaInfo.totalAvailable)}
                          </div>
                        </div>
                      </div>
                      {quotaInfo.totalCostLimit > 0 && quotaInfo.totalCostUsed !== undefined && (
                        <div className="mt-3">
                          <div className="flex justify-between text-xs mb-1">
                            <span>总使用率</span>
                            <span>{((quotaInfo.totalCostUsed / quotaInfo.totalCostLimit) * 100).toFixed(1)}%</span>
                          </div>
                          <div className="w-full bg-gray-200 rounded-full h-1.5">
                            <div 
                              className="bg-red-500 h-1.5 rounded-full transition-all duration-300"
                              style={{ 
                                width: `${Math.min(100, (quotaInfo.totalCostUsed / quotaInfo.totalCostLimit) * 100)}%`
                              }}
                            ></div>
                          </div>
                        </div>
                      )}
                    </div>
                  )}

                  {/* No Limits Set */}
                  {(!quotaInfo.dailyCostLimit || quotaInfo.dailyCostLimit === 0) && 
                   (!quotaInfo.monthlyCostLimit || quotaInfo.monthlyCostLimit === 0) && 
                   (!quotaInfo.totalCostLimit || quotaInfo.totalCostLimit === 0) && (
                    <div className="text-center py-6 text-muted-foreground">
                      <DollarSign className="h-12 w-12 mx-auto mb-2 opacity-50" />
                      <p>此 API Key 未设置费用限制</p>
                      <p className="text-sm">当前总消费：{formatCurrency(quotaInfo.totalCostUsed)}</p>
                    </div>
                  )}
                    </CardContent>
                  </Card>
                </div>

                {/* Right Column - Account Binding Info */}
                <div className="xl:col-span-2 lg:col-span-1">
                  {quotaInfo.accountBinding ? (
                    <Card>
                      <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                          <Link className="h-5 w-5" />
                          账户绑定信息
                        </CardTitle>
                      </CardHeader>
                      <CardContent className="space-y-4">
                        <div className="space-y-3">
                          <div>
                            <Label className="text-sm text-muted-foreground">绑定状态</Label>
                            <div className="flex items-center gap-2 mt-1">
                              {quotaInfo.accountBinding.isBound ? (
                                <Badge variant="default" className="bg-green-100 text-green-800">
                                  <CheckCircle className="h-3 w-3 mr-1" />
                                  已绑定
                                </Badge>
                              ) : (
                                <Badge variant="secondary">
                                  <XCircle className="h-3 w-3 mr-1" />
                                  未绑定
                                </Badge>
                              )}
                            </div>
                          </div>
                          
                          {quotaInfo.accountBinding.status && (
                            <div>
                              <Label className="text-sm text-muted-foreground">账户状态</Label>
                              <div className="flex items-center gap-2 mt-1">
                                {getStatusBadge(quotaInfo.accountBinding.status)}
                              </div>
                            </div>
                          )}

                          {quotaInfo.accountBinding.accountName && (
                            <div>
                              <Label className="text-sm text-muted-foreground">绑定账户</Label>
                              <p className="font-medium">{quotaInfo.accountBinding.accountName}</p>
                              {quotaInfo.accountBinding.accountId && (
                                <p className="font-mono text-xs text-muted-foreground">ID: {quotaInfo.accountBinding.accountId}</p>
                              )}
                            </div>
                          )}

                          {quotaInfo.accountBinding.createdAt && (
                            <div>
                              <Label className="text-sm text-muted-foreground">绑定时间</Label>
                              <p className="text-sm">{formatDate(quotaInfo.accountBinding.createdAt)}</p>
                            </div>
                          )}
                          
                          {quotaInfo.accountBinding.expiresAt && (
                            <div>
                              <Label className="text-sm text-muted-foreground">过期时间</Label>
                              <p className="text-sm">{formatDate(quotaInfo.accountBinding.expiresAt)}</p>
                            </div>
                          )}
                          
                          {quotaInfo.accountBinding.rateLimitedUntil && 
                           new Date(quotaInfo.accountBinding.rateLimitedUntil) > new Date() && (
                            <div className="p-3 bg-red-50 dark:bg-red-950/20 rounded-lg border border-red-200 dark:border-red-800">
                              <Label className="text-sm text-muted-foreground flex items-center gap-1">
                                <Timer className="h-3 w-3 text-red-500" />
                                限流解除时间
                              </Label>
                              <p className="text-sm text-red-600 dark:text-red-400 font-medium mt-1">
                                {formatDate(quotaInfo.accountBinding.rateLimitedUntil)}
                              </p>
                              <p className="text-xs text-red-500 dark:text-red-400 mt-1">
                                账户当前处于限流状态
                              </p>
                            </div>
                          )}
                        </div>

                        {/* Rate Limiting Info */}
                        {quotaInfo.accountBinding.rateLimiting && (
                          <div className="mt-6 p-4 bg-muted/50 rounded-lg">
                            <div className="flex items-center gap-2 mb-4">
                              <Shield className="h-5 w-5 text-blue-600" />
                              <h4 className="font-medium">限流设置</h4>
                              {quotaInfo.accountBinding.rateLimiting.isEnabled ? (
                                <Badge variant="default" className="bg-blue-100 text-blue-800">启用</Badge>
                              ) : (
                                <Badge variant="outline">未启用</Badge>
                              )}
                            </div>

                            {quotaInfo.accountBinding.rateLimiting.isEnabled && (
                              <div className="space-y-4">
                                {/* Rate Limits */}
                                <div className="space-y-2">
                                  {quotaInfo.accountBinding.rateLimiting.requestsPerMinute && (
                                    <div className="flex justify-between items-center p-2 bg-background rounded border">
                                      <span className="text-sm">每分钟</span>
                                      <span className="font-semibold text-blue-600">
                                        {quotaInfo.accountBinding.rateLimiting.requestsPerMinute}
                                      </span>
                                    </div>
                                  )}
                                  
                                  {quotaInfo.accountBinding.rateLimiting.requestsPerHour && (
                                    <div className="flex justify-between items-center p-2 bg-background rounded border">
                                      <span className="text-sm">每小时</span>
                                      <span className="font-semibold text-green-600">
                                        {quotaInfo.accountBinding.rateLimiting.requestsPerHour}
                                      </span>
                                    </div>
                                  )}
                                  
                                  {quotaInfo.accountBinding.rateLimiting.requestsPerDay && (
                                    <div className="flex justify-between items-center p-2 bg-background rounded border">
                                      <span className="text-sm">每天</span>
                                      <span className="font-semibold text-orange-600">
                                        {quotaInfo.accountBinding.rateLimiting.requestsPerDay}
                                      </span>
                                    </div>
                                  )}
                                </div>

                                {/* Current Usage */}
                                {quotaInfo.accountBinding.rateLimiting.currentUsage && (
                                  <div>
                                    <Label className="text-sm text-muted-foreground mb-2 block">当前使用量</Label>
                                    <div className="space-y-2">
                                      <div className="flex justify-between items-center p-2 bg-background rounded border">
                                        <span className="text-sm">分钟</span>
                                        <span className="font-medium">
                                          {quotaInfo.accountBinding.rateLimiting.currentUsage.minute} 
                                          {quotaInfo.accountBinding.rateLimiting.requestsPerMinute && 
                                            `/${quotaInfo.accountBinding.rateLimiting.requestsPerMinute}`}
                                        </span>
                                      </div>
                                      
                                      <div className="flex justify-between items-center p-2 bg-background rounded border">
                                        <span className="text-sm">小时</span>
                                        <span className="font-medium">
                                          {quotaInfo.accountBinding.rateLimiting.currentUsage.hour}
                                          {quotaInfo.accountBinding.rateLimiting.requestsPerHour && 
                                            `/${quotaInfo.accountBinding.rateLimiting.requestsPerHour}`}
                                        </span>
                                      </div>
                                      
                                      <div className="flex justify-between items-center p-2 bg-background rounded border">
                                        <span className="text-sm">天</span>
                                        <span className="font-medium">
                                          {quotaInfo.accountBinding.rateLimiting.currentUsage.day}
                                          {quotaInfo.accountBinding.rateLimiting.requestsPerDay && 
                                            `/${quotaInfo.accountBinding.rateLimiting.requestsPerDay}`}
                                        </span>
                                      </div>
                                    </div>
                                  </div>
                                )}

                                {/* Reset Times */}
                                {quotaInfo.accountBinding.rateLimiting.resetTimes && (  
                                  <div>
                                    <Label className="text-sm text-muted-foreground mb-2 block flex items-center gap-1">
                                      <Clock className="h-3 w-3" />
                                      重置时间
                                    </Label>
                                    <div className="space-y-2 text-xs">
                                      {quotaInfo.accountBinding.rateLimiting.resetTimes.minute && (
                                        <div className="p-2 bg-background rounded border">
                                          <div className="text-muted-foreground">分钟重置</div>
                                          <div className="font-mono">{quotaInfo.accountBinding.rateLimiting.resetTimes.minute}</div>
                                        </div>
                                      )}
                                      
                                      {quotaInfo.accountBinding.rateLimiting.resetTimes.hour && (
                                        <div className="p-2 bg-background rounded border">
                                          <div className="text-muted-foreground">小时重置</div>
                                          <div className="font-mono">{quotaInfo.accountBinding.rateLimiting.resetTimes.hour}</div>
                                        </div>
                                      )}
                                      
                                      {quotaInfo.accountBinding.rateLimiting.resetTimes.day && (  
                                        <div className="p-2 bg-background rounded border">
                                          <div className="text-muted-foreground">天重置</div>
                                          <div className="font-mono">{quotaInfo.accountBinding.rateLimiting.resetTimes.day}</div>
                                        </div>
                                      )}
                                    </div>
                                  </div>
                                )}
                              </div>
                            )}
                          </div>
                        )}
                      </CardContent>
                    </Card>
                  ) : (
                    <Card>
                      <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                          <Link className="h-5 w-5" />
                          账户绑定信息
                        </CardTitle>
                      </CardHeader>
                      <CardContent>
                        <div className="text-center py-6 text-muted-foreground">
                          <XCircle className="h-12 w-12 mx-auto mb-2 opacity-50" />
                          <p>此 API Key 未绑定任何账户</p>
                        </div>
                      </CardContent>
                    </Card>
                  )}
                </div>
              </div>
            </div>
          )}

          {/* Footer Note */}
          <Card className="bg-muted/50">
            <CardContent className="pt-6">
              <div className="text-sm text-muted-foreground space-y-2">
                <p className="flex items-center gap-2">
                  <AlertCircle className="h-4 w-4" />
                  <strong>注意事项：</strong>
                </p>
                <ul className="list-disc list-inside space-y-1 ml-6">
                  <li>此查询不需要登录，但请确保API Key的安全性</li>
                  <li>查询结果仅显示当前时刻的额度信息</li>
                  <li>请勿在公共场所或不安全的网络环境下进行查询</li>
                  <li>API Key应妥善保管，避免泄露给他人</li>
                </ul>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
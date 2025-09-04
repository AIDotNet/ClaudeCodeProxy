import { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { ThemeToggle } from "@/components/ui/theme-toggle";
import { Search, Key, DollarSign, Activity, AlertCircle, Link, Clock, Shield, CheckCircle, XCircle, Pause } from "lucide-react";

interface QuotaInfo {
  total_granted?: number;
  total_used?: number;
  total_available?: number;
  organization?: {
    name: string;
    id: string;
  };
  account_binding?: {
    is_bound: boolean;
    account_name?: string;
    account_id?: string;
    rate_limiting?: {
      is_enabled: boolean;
      requests_per_minute?: number;
      requests_per_hour?: number;
      requests_per_day?: number;
      current_usage?: {
        minute: number;
        hour: number;
        day: number;
      };
      reset_times?: {
        minute: string;
        hour: string;
        day: string;
      };
    };
    status?: 'active' | 'suspended' | 'expired';
    created_at?: string;
    expires_at?: string;
  };
}

export default function QuotaQueryPage() {
  const [apiKey, setApiKey] = useState('');
  const [loading, setLoading] = useState(false);
  const [quotaInfo, setQuotaInfo] = useState<QuotaInfo | null>(null);
  const [error, setError] = useState<string | null>(null);

  const queryQuota = async () => {
    if (!apiKey.trim()) {
      setError('请输入API Key');
      return;
    }

    setLoading(true);
    setError(null);
    setQuotaInfo(null);

    try {
      // 这里调用后端API查询额度
      const response = await fetch('/api/quota/query', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ apiKey: apiKey.trim() }),
      });

      if (!response.ok) {
        throw new Error('查询失败，请检查API Key是否正确');
      }

      const data = await response.json();
      setQuotaInfo(data);
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
        <div className="max-w-2xl mx-auto space-y-6">
          {/* Query Form */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Search className="h-5 w-5" />
                查询API Key额度
              </CardTitle>
              <CardDescription>
                输入您的API Key来查询当前的使用额度和余额信息
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
              <Button 
                onClick={queryQuota} 
                disabled={loading || !apiKey.trim()}
                className="w-full"
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
            </CardContent>
          </Card>

          {/* Error Message */}
          {error && (
            <Card className="border-destructive">
              <CardContent className="pt-6">
                <div className="flex items-center gap-2 text-destructive">
                  <AlertCircle className="h-5 w-5" />
                  <span>{error}</span>
                </div>
              </CardContent>
            </Card>
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

              {/* Account Binding Info */}
              {quotaInfo.account_binding && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Link className="h-5 w-5" />
                      账户绑定信息
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <div>
                        <Label className="text-sm text-muted-foreground">绑定状态</Label>
                        <div className="flex items-center gap-2 mt-1">
                          {quotaInfo.account_binding.is_bound ? (
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
                      
                      {quotaInfo.account_binding.status && (
                        <div>
                          <Label className="text-sm text-muted-foreground">账户状态</Label>
                          <div className="flex items-center gap-2 mt-1">
                            {getStatusBadge(quotaInfo.account_binding.status)}
                          </div>
                        </div>
                      )}
                    </div>

                    {quotaInfo.account_binding.account_name && (
                      <div>
                        <Label className="text-sm text-muted-foreground">绑定账户</Label>
                        <p className="font-medium">{quotaInfo.account_binding.account_name}</p>
                        {quotaInfo.account_binding.account_id && (
                          <p className="font-mono text-xs text-muted-foreground">ID: {quotaInfo.account_binding.account_id}</p>
                        )}
                      </div>
                    )}

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      {quotaInfo.account_binding.created_at && (
                        <div>
                          <Label className="text-sm text-muted-foreground">绑定时间</Label>
                          <p className="text-sm">{formatDate(quotaInfo.account_binding.created_at)}</p>
                        </div>
                      )}
                      
                      {quotaInfo.account_binding.expires_at && (
                        <div>
                          <Label className="text-sm text-muted-foreground">过期时间</Label>
                          <p className="text-sm">{formatDate(quotaInfo.account_binding.expires_at)}</p>
                        </div>
                      )}
                    </div>

                    {/* Rate Limiting Info */}
                    {quotaInfo.account_binding.rate_limiting && (
                      <div className="mt-6 p-4 bg-muted/50 rounded-lg">
                        <div className="flex items-center gap-2 mb-4">
                          <Shield className="h-5 w-5 text-blue-600" />
                          <h4 className="font-medium">限流设置</h4>
                          {quotaInfo.account_binding.rate_limiting.is_enabled ? (
                            <Badge variant="default" className="bg-blue-100 text-blue-800">启用</Badge>
                          ) : (
                            <Badge variant="outline">未启用</Badge>
                          )}
                        </div>

                        {quotaInfo.account_binding.rate_limiting.is_enabled && (
                          <div className="space-y-4">
                            {/* Rate Limits */}
                            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                              {quotaInfo.account_binding.rate_limiting.requests_per_minute && (
                                <div className="text-center p-3 bg-background rounded border">
                                  <div className="text-lg font-semibold text-blue-600">
                                    {quotaInfo.account_binding.rate_limiting.requests_per_minute}
                                  </div>
                                  <div className="text-xs text-muted-foreground">请求/分钟</div>
                                </div>
                              )}
                              
                              {quotaInfo.account_binding.rate_limiting.requests_per_hour && (
                                <div className="text-center p-3 bg-background rounded border">
                                  <div className="text-lg font-semibold text-green-600">
                                    {quotaInfo.account_binding.rate_limiting.requests_per_hour}
                                  </div>
                                  <div className="text-xs text-muted-foreground">请求/小时</div>
                                </div>
                              )}
                              
                              {quotaInfo.account_binding.rate_limiting.requests_per_day && (
                                <div className="text-center p-3 bg-background rounded border">
                                  <div className="text-lg font-semibold text-orange-600">
                                    {quotaInfo.account_binding.rate_limiting.requests_per_day}
                                  </div>
                                  <div className="text-xs text-muted-foreground">请求/天</div>
                                </div>
                              )}
                            </div>

                            {/* Current Usage */}
                            {quotaInfo.account_binding.rate_limiting.current_usage && (
                              <div>
                                <Label className="text-sm text-muted-foreground mb-2 block">当前使用量</Label>
                                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                                  <div className="flex justify-between items-center p-2 bg-background rounded border">
                                    <span className="text-sm">分钟</span>
                                    <span className="font-medium">
                                      {quotaInfo.account_binding.rate_limiting.current_usage.minute}
                                      {quotaInfo.account_binding.rate_limiting.requests_per_minute && 
                                        `/${quotaInfo.account_binding.rate_limiting.requests_per_minute}`}
                                    </span>
                                  </div>
                                  
                                  <div className="flex justify-between items-center p-2 bg-background rounded border">
                                    <span className="text-sm">小时</span>
                                    <span className="font-medium">
                                      {quotaInfo.account_binding.rate_limiting.current_usage.hour}
                                      {quotaInfo.account_binding.rate_limiting.requests_per_hour && 
                                        `/${quotaInfo.account_binding.rate_limiting.requests_per_hour}`}
                                    </span>
                                  </div>
                                  
                                  <div className="flex justify-between items-center p-2 bg-background rounded border">
                                    <span className="text-sm">天</span>
                                    <span className="font-medium">
                                      {quotaInfo.account_binding.rate_limiting.current_usage.day}
                                      {quotaInfo.account_binding.rate_limiting.requests_per_day && 
                                        `/${quotaInfo.account_binding.rate_limiting.requests_per_day}`}
                                    </span>
                                  </div>
                                </div>
                              </div>
                            )}

                            {/* Reset Times */}
                            {quotaInfo.account_binding.rate_limiting.reset_times && (
                              <div>
                                <Label className="text-sm text-muted-foreground mb-2 block flex items-center gap-1">
                                  <Clock className="h-3 w-3" />
                                  重置时间
                                </Label>
                                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-xs">
                                  {quotaInfo.account_binding.rate_limiting.reset_times.minute && (
                                    <div className="p-2 bg-background rounded border">
                                      <div className="text-muted-foreground">分钟重置</div>
                                      <div className="font-mono">{quotaInfo.account_binding.rate_limiting.reset_times.minute}</div>
                                    </div>
                                  )}
                                  
                                  {quotaInfo.account_binding.rate_limiting.reset_times.hour && (
                                    <div className="p-2 bg-background rounded border">
                                      <div className="text-muted-foreground">小时重置</div>
                                      <div className="font-mono">{quotaInfo.account_binding.rate_limiting.reset_times.hour}</div>
                                    </div>
                                  )}
                                  
                                  {quotaInfo.account_binding.rate_limiting.reset_times.day && (
                                    <div className="p-2 bg-background rounded border">
                                      <div className="text-muted-foreground">天重置</div>
                                      <div className="font-mono">{quotaInfo.account_binding.rate_limiting.reset_times.day}</div>
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
              )}

              {/* Quota Details */}
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
                <CardContent>
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                    {/* Total Granted */}
                    <div className="space-y-2">
                      <Label className="text-sm text-muted-foreground">总额度</Label>
                      <div className="text-2xl font-bold text-blue-600">
                        {formatCurrency(quotaInfo.total_granted)}
                      </div>
                      <Badge variant="outline" className="text-xs">
                        Total Granted
                      </Badge>
                    </div>

                    {/* Total Used */}
                    <div className="space-y-2">
                      <Label className="text-sm text-muted-foreground">已使用</Label>
                      <div className="text-2xl font-bold text-orange-600">
                        {formatCurrency(quotaInfo.total_used)}
                      </div>
                      <Badge variant="outline" className="text-xs">
                        Total Used
                      </Badge>
                    </div>

                    {/* Total Available */}
                    <div className="space-y-2">
                      <Label className="text-sm text-muted-foreground">剩余额度</Label>
                      <div className="text-2xl font-bold text-green-600">
                        {formatCurrency(quotaInfo.total_available)}
                      </div>
                      <Badge variant="outline" className="text-xs">
                        Available
                      </Badge>
                    </div>
                  </div>

                  {/* Usage Progress */}
                  {quotaInfo.total_granted !== undefined && quotaInfo.total_used !== undefined && quotaInfo.total_granted > 0 && (
                    <div className="mt-6 space-y-2">
                      <div className="flex justify-between text-sm">
                        <span>使用率</span>
                        <span>{((quotaInfo.total_used / quotaInfo.total_granted) * 100).toFixed(1)}%</span>
                      </div>
                      <div className="w-full bg-gray-200 rounded-full h-2">
                        <div 
                          className="bg-primary h-2 rounded-full transition-all duration-300"
                          style={{ 
                            width: `${Math.min(100, (quotaInfo.total_used / quotaInfo.total_granted) * 100)}%`
                          }}
                        ></div>
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>
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
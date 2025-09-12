import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { useToast } from '@/contexts/ToastContext';
import { 
  Loader2, 
  Wallet, 
  DollarSign, 
  Activity,
  Key,
  FileText,
  Calendar,
  RefreshCw
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { apiService } from '@/services/api';

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8'];

const PersonalDashboardPage: React.FC = () => {
  const [dashboard, setDashboard] = useState<any | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const { showToast } = useToast();
  const navigate = useNavigate();

  useEffect(() => {
    loadDashboard();
  }, []);

  const loadDashboard = async (showRefreshing = false) => {
    if (showRefreshing) setRefreshing(true);
    try {
      const data = await apiService.getProfileDashboard();
      setDashboard(data);
    } catch (error) {
      console.error('加载仪表板失败:', error);
      showToast('加载仪表板数据失败', 'error');
    } finally {
      setLoading(false);
      if (showRefreshing) setRefreshing(false);
    }
  };

  const handleRefresh = () => {
    loadDashboard(true);
  };

  const formatCurrency = (amount: number) => {
    return `$${amount.toFixed(6)}`;
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('zh-CN');
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!dashboard) {
    return (
      <div className="flex items-center justify-center h-96">
        <p className="text-muted-foreground">加载失败</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold">个人仪表板</h1>
          <p className="text-muted-foreground">
            数据更新时间: {new Date(dashboard.lastUpdateTime).toLocaleString('zh-CN')}
          </p>
        </div>
        <Button onClick={handleRefresh} disabled={refreshing} variant="outline" size="sm">
          <RefreshCw className={`h-4 w-4 mr-2 ${refreshing ? 'animate-spin' : ''}`} />
          刷新
        </Button>
      </div>

      {/* 核心指标卡片 */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">钱包余额</CardTitle>
            <Wallet className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">
              {formatCurrency(dashboard.wallet.currentBalance)}
            </div>
            <p className="text-xs text-muted-foreground">
              日均消费: {formatCurrency(dashboard.wallet.dailyAverageUsage)}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">总请求数</CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{dashboard.requests.totalRequests.toLocaleString()}</div>
            <p className="text-xs text-muted-foreground">
              成功率: {dashboard.requests.successRate.toFixed(1)}%
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">API Key数量</CardTitle>
            <Key className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{dashboard.apiKeyCount}</div>
            <p className="text-xs text-muted-foreground">
              活跃: {dashboard.activeApiKeyCount}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">总费用</CardTitle>
            <DollarSign className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(dashboard.requests.totalCost)}</div>
            <p className="text-xs text-muted-foreground">
              Token数: {dashboard.requests.totalTokens.toLocaleString()}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* 快速操作 */}
      <Card>
        <CardHeader>
          <CardTitle>快速操作</CardTitle>
          <CardDescription>常用功能的快捷入口</CardDescription>
        </CardHeader>
        <CardContent className="flex gap-4">
          <Button onClick={() => navigate('/apikeys')} variant="outline">
            <Key className="h-4 w-4 mr-2" />
            管理API Key
          </Button>
          <Button onClick={() => navigate('/request-logs')} variant="outline">
            <FileText className="h-4 w-4 mr-2" />
            查看请求日志
          </Button>
          <Button onClick={() => navigate('/profile')} variant="outline">
            <Calendar className="h-4 w-4 mr-2" />
            个人资料
          </Button>
        </CardContent>
      </Card>

      <div className="grid gap-6 md:grid-cols-2">
        {/* 每日使用趋势 */}
        <Card>
          <CardHeader>
            <CardTitle>每日使用趋势</CardTitle>
            <CardDescription>过去30天的请求数量变化</CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={dashboard.requests.dailyUsage}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis 
                  dataKey="date" 
                  tickFormatter={(value) => {
                    // Handle both date string and Date object
                    const date = typeof value === 'string' ? new Date(value) : value;
                    return formatDate(date.toISOString());
                  }}
                />
                <YAxis />
                <Tooltip 
                  labelFormatter={(value) => {
                    const date = typeof value === 'string' ? new Date(value) : value;
                    return formatDate(date.toISOString());
                  }}
                  formatter={(value, name) => [
                    typeof value === 'number' ? value.toLocaleString() : value,
                    name === 'requestCount' ? '请求数' : 
                    name === 'totalCost' ? '费用' : name
                  ]}
                />
                <Line 
                  type="monotone" 
                  dataKey="requestCount" 
                  stroke="#8884d8" 
                  strokeWidth={2}
                  name="requestCount"
                />
                <Line 
                  type="monotone" 
                  dataKey="totalCost" 
                  stroke="#82ca9d" 
                  strokeWidth={2}
                  name="totalCost"
                />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        {/* 模型使用分布 */}
        <Card>
          <CardHeader>
            <CardTitle>模型使用分布</CardTitle>
            <CardDescription>各模型的请求数量占比</CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={dashboard.requests.modelUsage}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ model, percent }:any) => `${model} ${((percent || 0) * 100).toFixed(0)}%`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="requestCount"
                >
                  {dashboard.requests.modelUsage.map((_:any, index:any) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip 
                  formatter={(value) => [
                    typeof value === 'number' ? value.toLocaleString() : value,
                    '请求数'
                  ]}
                />
              </PieChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      {/* 模型使用详情 */}
      <Card>
        <CardHeader>
          <CardTitle>模型使用详情</CardTitle>
          <CardDescription>各模型的详细使用统计</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {dashboard.requests.modelUsage.map((model:any, index:any) => (
              <div key={model.model} className="flex items-center justify-between p-4 border rounded-lg">
                <div className="flex items-center space-x-4">
                  <div 
                    className="w-4 h-4 rounded-full" 
                    style={{ backgroundColor: COLORS[index % COLORS.length] }}
                  />
                  <div>
                    <h4 className="font-medium">{model.model}</h4>
                    <p className="text-sm text-muted-foreground">
                      {model.requestCount} 次请求
                    </p>
                  </div>
                </div>
                <div className="text-right">
                  <p className="font-medium">{formatCurrency(model.totalCost)}</p>
                  <p className="text-sm text-muted-foreground">
                    {model.totalTokens.toLocaleString()} tokens
                  </p>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

export default PersonalDashboardPage;
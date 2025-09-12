import { useState, useEffect } from 'react';
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip, Legend, BarChart, Bar, XAxis, YAxis, CartesianGrid } from 'recharts';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Button } from '@/components/ui/button';
import { PieChart as PieChartIcon,  Zap } from 'lucide-react';
import { apiService, type ModelStatistics, type DateFilterRequest } from '@/services/api';

interface ModelDistributionChartProps {
  className?: string;
}

const COLORS = [
  '#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8',
  '#82CA9D', '#FFC658', '#FF7300', '#00FF00', '#FF00FF'
];

export default function ModelDistributionChart({ className }: ModelDistributionChartProps) {
  const [data, setData] = useState<ModelStatistics[]>([]);
  const [loading, setLoading] = useState(true);
  const [chartType, setChartType] = useState<'pie' | 'bar'>('pie');
  const [metric, setMetric] = useState<'requests' | 'tokens' | 'cost'>('requests');
  const [dateRange, setDateRange] = useState('30days');

  const fetchModelData = async () => {
    try {
      setLoading(true);
      
      const dateFilter: DateFilterRequest = {
        type: 'preset',
        preset: dateRange
      };

      const modelStats = await apiService.getModelStatistics(dateFilter);
      setData(modelStats);
    } catch (error) {
      console.error('获取模型统计数据失败:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchModelData();
  }, [dateRange]);

  const formatValue = (value: number, type: string) => {
    if (type === 'cost') {
      return `$${value.toFixed(4)}`;
    } else if (value >= 1000000) {
      return (value / 1000000).toFixed(1) + 'M';
    } else if (value >= 1000) {
      return (value / 1000).toFixed(1) + 'K';
    }
    return value.toString();
  };

  const getChartData = () => {
    return data.map(item => ({
      name: item.model,
      value: metric === 'requests' ? item.requests : 
             metric === 'tokens' ? item.allTokens : 
             item.cost,
      formatted: formatValue(
        metric === 'requests' ? item.requests : 
        metric === 'tokens' ? item.allTokens : 
        item.cost, 
        metric
      )
    }));
  };

  const CustomTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      const data = payload[0];
      return (
        <div className="bg-white p-3 border rounded-lg shadow-lg">
          <p className="font-medium">{data.payload.name}</p>
          <p style={{ color: data.color }}>
            {metric === 'requests' ? '请求数' : 
             metric === 'tokens' ? 'Token数' : 
             '费用'}: {data.payload.formatted}
          </p>
        </div>
      );
    }
    return null;
  };

  const chartData = getChartData();

  return (
    <Card className={className}>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <PieChartIcon className="h-5 w-5" />
            <CardTitle>模型使用分布</CardTitle>
          </div>
          <div className="flex items-center space-x-2">
            <Select value={chartType} onValueChange={(value: 'pie' | 'bar') => setChartType(value)}>
              <SelectTrigger className="w-24">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="pie">饼图</SelectItem>
                <SelectItem value="bar">柱状图</SelectItem>
              </SelectContent>
            </Select>
            
            <Select value={metric} onValueChange={(value: 'requests' | 'tokens' | 'cost') => setMetric(value)}>
              <SelectTrigger className="w-24">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="requests">请求数</SelectItem>
                <SelectItem value="tokens">Token数</SelectItem>
                <SelectItem value="cost">费用</SelectItem>
              </SelectContent>
            </Select>
            
            <Select value={dateRange} onValueChange={setDateRange}>
              <SelectTrigger className="w-24">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="7days">7天</SelectItem>
                <SelectItem value="30days">30天</SelectItem>
                <SelectItem value="90days">90天</SelectItem>
                <SelectItem value="all">全部</SelectItem>
              </SelectContent>
            </Select>
            
            <Button variant="outline" size="sm" onClick={fetchModelData}>
              <Zap className="h-4 w-4" />
            </Button>
          </div>
        </div>
        <CardDescription>
          {metric === 'requests' ? '各模型请求数量分布' : 
           metric === 'tokens' ? '各模型Token使用分布' : 
           '各模型费用分布'}
        </CardDescription>
      </CardHeader>
      <CardContent>
        {loading ? (
          <div className="h-80 flex items-center justify-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
          </div>
        ) : chartData.length === 0 ? (
          <div className="h-80 flex items-center justify-center text-muted-foreground">
            <p>暂无数据</p>
          </div>
        ) : (
          <ResponsiveContainer width="100%" height={320}>
            {chartType === 'pie' ? (
              <PieChart>
                <Pie
                  data={chartData}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ name, percent }:any) => `${name} ${(percent ?? 0 * 100).toFixed(1)}%`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="value"
                >
                  {chartData.map((_, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip content={<CustomTooltip />} />
                <Legend />
              </PieChart>
            ) : (
              <BarChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis 
                  dataKey="name" 
                  fontSize={12}
                  angle={-45}
                  textAnchor="end"
                  height={80}
                />
                <YAxis 
                  fontSize={12}
                  tickFormatter={(value) => formatValue(value, metric)}
                />
                <Tooltip content={<CustomTooltip />} />
                <Bar dataKey="value" fill="#8884d8">
                  {chartData.map((_, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Bar>
              </BarChart>
            )}
          </ResponsiveContainer>
        )}
        
        {/* 数据表格 */}
        {!loading && chartData.length > 0 && (
          <div className="mt-4 overflow-hidden">
            <div className="max-h-32 overflow-y-auto">
              <table className="w-full text-sm">
                <thead className="sticky top-0 bg-muted">
                  <tr>
                    <th className="text-left p-2">模型</th>
                    <th className="text-right p-2">数值</th>
                    <th className="text-right p-2">占比</th>
                  </tr>
                </thead>
                <tbody>
                  {chartData.map((item, index) => {
                    const total = chartData.reduce((sum, d) => sum + d.value, 0);
                    const percentage = total > 0 ? (item.value / total * 100).toFixed(1) : '0.0';
                    return (
                      <tr key={index} className="border-t">
                        <td className="p-2">{item.name}</td>
                        <td className="text-right p-2">{item.formatted}</td>
                        <td className="text-right p-2">{percentage}%</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
} 
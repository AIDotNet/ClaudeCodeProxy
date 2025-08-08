import React, { useEffect, useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { User, CreditCard, Activity, Key, FileText, Gift, Clock } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { useToast } from '@/contexts/ToastContext'
import { apiService } from '@/services/api'

interface ProfileData {
  id: string
  username: string
  email: string
  role: string
  isActive: boolean
  createdTime: string
  lastLoginTime?: string
}

interface DashboardStats {
  totalApiKeys: number
  activeApiKeys: number
  totalRequests: number
  todayRequests: number
  totalInputTokens: number
  totalOutputTokens: number
  totalCost: number
  successfulRequests: number
  failedRequests: number
}

const ProfilePage: React.FC = () => {
  const { user } = useAuth()
  const { showToast } = useToast()
  const [profileData, setProfileData] = useState<ProfileData | null>(null)
  const [dashboardStats, setDashboardStats] = useState<DashboardStats | null>(null)
  const [loading, setLoading] = useState(true)
  
  // 兑换码相关状态
  const [redeemCode, setRedeemCode] = useState('')
  const [redeemLoading, setRedeemLoading] = useState(false)
  const [showRedeemDialog, setShowRedeemDialog] = useState(false)
  const [showRecordsDialog, setShowRecordsDialog] = useState(false)

  useEffect(() => {
    fetchProfileData()
    fetchDashboardStats()
  }, [])

  const fetchProfileData = async () => {
    try {
      if (user?.id) {
        const response = await apiService.getUserById(user.id.toString())
        const profileData = {
          id: response.id,
          username: response.username,
          email: response.email,
          role: response.roleName || 'User',
          isActive: response.isActive,
          createdTime: response.createdAt,
          lastLoginTime: response.lastLoginAt
        }
        setProfileData(profileData)
      }
    } catch (error) {
      console.error('Failed to fetch profile:', error)
      showToast('获取个人资料失败', 'error')
    }
  }

  const fetchDashboardStats = async () => {
    try {
      const [dashboardData, costData] = await Promise.all([
        apiService.getDashboardData(),
        apiService.getCostData()
      ])
      
      const stats = {
        totalApiKeys: dashboardData.totalApiKeys,
        activeApiKeys: dashboardData.activeApiKeys,
        totalRequests: dashboardData.totalRequests,
        todayRequests: dashboardData.todayRequests,
        totalInputTokens: dashboardData.totalInputTokens,
        totalOutputTokens: dashboardData.totalOutputTokens,
        totalCost: costData.totalCosts.totalCost,
        successfulRequests: Math.floor(dashboardData.totalRequests * 0.95), // Estimate success rate
        failedRequests: Math.floor(dashboardData.totalRequests * 0.05) // Estimate failure rate
      }
      setDashboardStats(stats)
    } catch (error) {
      console.error('Failed to fetch dashboard stats:', error)
      showToast('获取统计数据失败', 'error')
    } finally {
      setLoading(false)
    }
  }

  // 兑换码处理函数
  const handleRedeemCode = async () => {
    if (!redeemCode.trim()) {
      showToast('请输入兑换码', 'error')
      return
    }

    setRedeemLoading(true)
    try {
      const result = await apiService.useRedeemCode(redeemCode.trim())
      if (result.success) {
        showToast(`兑换成功！获得 ¥${result.amount} 余额，当前余额：¥${result.newBalance}`, 'success')
        setRedeemCode('')
        setShowRedeemDialog(false)
        // 刷新页面数据
        fetchDashboardStats()
      } else {
        showToast(result.message, 'error')
      }
    } catch (error) {
      console.error('兑换码使用失败:', error)
      showToast('兑换码使用失败', 'error')
    } finally {
      setRedeemLoading(false)
    }
  }

  // // 获取兑换记录
  // const fetchRedeemRecords = async () => {
  //   setRecordsLoading(true)
  //   try {
  //     const response = await apiService.getMyRedeemRecords()
  //     if (response.success) {
  //       setRedeemRecords(response.data)
  //     }
  //   } catch (error) {
  //     console.error('获取兑换记录失败:', error)
  //     showToast('获取兑换记录失败', 'error')
  //   } finally {
  //     setRecordsLoading(false)
  //   }
  // }


  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-lg">加载中...</div>
      </div>
    )
  }

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center space-x-4 mb-6">
        <User className="h-8 w-8" />
        <div>
          <h1 className="text-3xl font-bold">个人资料</h1>
          <p className="text-muted-foreground">管理您的个人信息和账户设置</p>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* 个人信息卡片 */}
        <Card className="lg:col-span-1">
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <User className="h-5 w-5" />
              <span>基本信息</span>
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center space-x-4">
              <Avatar className="h-16 w-16">
                <AvatarImage src="" alt={profileData?.username} />
                <AvatarFallback className="text-lg">
                  {profileData?.username?.charAt(0).toUpperCase()}
                </AvatarFallback>
              </Avatar>
              <div>
                <h3 className="text-lg font-semibold">{profileData?.username}</h3>
                <p className="text-sm text-muted-foreground">{profileData?.email}</p>
                <Badge variant={profileData?.role === 'Admin' ? 'default' : 'secondary'}>
                  {profileData?.role === 'Admin' ? '管理员' : '普通用户'}
                </Badge>
              </div>
            </div>
            
            <Separator />
            
            <div className="space-y-2">
              <div className="flex justify-between">
                <span className="text-sm font-medium">账户状态</span>
                <Badge variant={profileData?.isActive ? 'default' : 'destructive'}>
                  {profileData?.isActive ? '正常' : '已禁用'}
                </Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-sm font-medium">注册时间</span>
                <span className="text-sm text-muted-foreground">
                  {profileData?.createdTime ? new Date(profileData.createdTime).toLocaleDateString() : '-'}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm font-medium">最后登录</span>
                <span className="text-sm text-muted-foreground">
                  {profileData?.lastLoginTime ? new Date(profileData.lastLoginTime).toLocaleDateString() : '-'}
                </span>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* 统计概览卡片 */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <Activity className="h-5 w-5" />
              <span>使用统计</span>
            </CardTitle>
            <CardDescription>查看您的API使用情况和统计数据</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="grid gap-4 md:grid-cols-3">
              <div className="text-center p-4 border rounded-lg">
                <div className="text-2xl font-bold text-blue-600">
                  {dashboardStats?.totalRequests?.toLocaleString() || '0'}
                </div>
                <div className="text-sm text-muted-foreground">总请求数</div>
              </div>
              <div className="text-center p-4 border rounded-lg">
                <div className="text-2xl font-bold text-green-600">
                  {dashboardStats?.todayRequests?.toLocaleString() || '0'}
                </div>
                <div className="text-sm text-muted-foreground">今日请求</div>
              </div>
              <div className="text-center p-4 border rounded-lg">
                <div className="text-2xl font-bold text-red-600">
                  ¥{dashboardStats?.totalCost?.toFixed(2) || '0.00'}
                </div>
                <div className="text-sm text-muted-foreground">累计消费</div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* 兑换码功能卡片 */}
        <Card className="lg:col-span-1">
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <Gift className="h-5 w-5" />
              <span>兑换码</span>
            </CardTitle>
            <CardDescription>使用兑换码充值余额或获取奖励</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Dialog open={showRedeemDialog} onOpenChange={setShowRedeemDialog}>
              <DialogTrigger asChild>
                <Button className="w-full" variant="outline">
                  <Gift className="mr-2 h-4 w-4" />
                  使用兑换码
                </Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>使用兑换码</DialogTitle>
                  <DialogDescription>
                    请输入您的兑换码来获取余额充值或其他奖励
                  </DialogDescription>
                </DialogHeader>
                <div className="space-y-4">
                  <div>
                    <Label htmlFor="redeem-code">兑换码</Label>
                    <Input
                      id="redeem-code"
                      placeholder="请输入兑换码，如：ABCD-EFGH-IJKL-MNOP"
                      value={redeemCode}
                      onChange={(e) => setRedeemCode(e.target.value.toUpperCase())}
                      disabled={redeemLoading}
                    />
                  </div>
                </div>
                <DialogFooter>
                  <Button variant="outline" onClick={() => setShowRedeemDialog(false)} disabled={redeemLoading}>
                    取消
                  </Button>
                  <Button onClick={handleRedeemCode} disabled={redeemLoading || !redeemCode.trim()}>
                    {redeemLoading ? '兑换中...' : '兑换'}
                  </Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>

            <Dialog open={showRecordsDialog} onOpenChange={setShowRecordsDialog}>
              <DialogTrigger asChild>
                <Button className="w-full" variant="outline">
                  <Clock className="mr-2 h-4 w-4" />
                  兑换记录
                </Button>
              </DialogTrigger>
              <DialogContent className="max-w-4xl">
                <DialogHeader>
                  <DialogTitle>兑换记录</DialogTitle>
                  <DialogDescription>
                    查看您的历史兑换记录
                  </DialogDescription>
                </DialogHeader>
                <div className="space-y-4">
                  {/* {recordsLoading ? (
                    <div className="flex items-center justify-center py-8">
                      <div className="text-sm text-muted-foreground">加载中...</div>
                    </div>
                  ) : redeemRecords.length === 0 ? (
                    <div className="flex items-center justify-center py-8">
                      <div className="text-sm text-muted-foreground">暂无兑换记录</div>
                    </div>
                  ) : (
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>兑换码</TableHead>
                          <TableHead>类型</TableHead>
                          <TableHead>金额</TableHead>
                          <TableHead>描述</TableHead>
                          <TableHead>兑换时间</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {redeemRecords.map((record) => (
                          <TableRow key={record.id}>
                            <TableCell className="font-mono">{record.code}</TableCell>
                            <TableCell>
                              <Badge variant={record.type === 'balance' ? 'default' : 'secondary'}>
                                {record.type === 'balance' ? '余额充值' : '积分奖励'}
                              </Badge>
                            </TableCell>
                            <TableCell>¥{record.amount.toLocaleString()}</TableCell>
                            <TableCell>{record.description || '-'}</TableCell>
                            <TableCell>{new Date(record.usedAt).toLocaleString()}</TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  )} */}
                </div>
                <DialogFooter>
                  <Button variant="outline" onClick={() => setShowRecordsDialog(false)}>
                    关闭
                  </Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>
          </CardContent>
        </Card>
      </div>

      {/* 统计数据卡片 */}
      {dashboardStats && (
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">今日请求</CardTitle>
              <Activity className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{dashboardStats.todayRequests}</div>
              <p className="text-xs text-muted-foreground">
                总请求 {dashboardStats.totalRequests.toLocaleString()} 次
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">总代币数</CardTitle>
              <FileText className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{(dashboardStats.totalInputTokens + dashboardStats.totalOutputTokens).toLocaleString()}</div>
              <p className="text-xs text-muted-foreground">
                成功率 {dashboardStats.totalRequests > 0 
                  ? ((dashboardStats.successfulRequests / dashboardStats.totalRequests) * 100).toFixed(1)
                  : 0}%
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">API密钥</CardTitle>
              <Key className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{dashboardStats.activeApiKeys}</div>
              <p className="text-xs text-muted-foreground">
                共 {dashboardStats.totalApiKeys} 个
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">总消费</CardTitle>
              <CreditCard className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">¥{dashboardStats.totalCost.toFixed(2)}</div>
              <p className="text-xs text-muted-foreground">
                {(dashboardStats.totalInputTokens + dashboardStats.totalOutputTokens).toLocaleString()} 代币
              </p>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  )
}

export default ProfilePage
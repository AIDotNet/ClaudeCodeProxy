import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Users, Plus, Edit2, Trash2, Calendar, Activity, Server, Globe, Key, Settings, Zap } from 'lucide-react';
import { apiService } from '@/services/api';
import type { Account } from '@/services/api';
import { showToast } from '@/utils/toast';
import AccountModal from '@/components/accounts/AccountModal';
import ConfirmModal from '@/components/common/ConfirmModal';
import { useConfirm } from '@/hooks/useConfirm';

const PLATFORMS = [
  { value: 'claude', label: 'Claude', icon: Users, color: 'bg-primary' },
  { value: 'claude-console', label: 'Claude Console', icon: Server, color: 'bg-secondary' },
  { value: 'gemini', label: 'Gemini', icon: Globe, color: 'bg-muted' },
  { value: 'openai', label: 'OpenAI', icon: Zap, color: 'bg-slate-500' },
  { value: 'thor', label: 'Thor', icon: Zap, color: 'bg-blue-500' },
];

export default function AccountsPage() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingAccount, setEditingAccount] = useState<Account | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [currentTime, setCurrentTime] = useState(new Date());
  const { showConfirmModal, confirmOptions, showConfirm, handleConfirm, handleCancel } = useConfirm();

  useEffect(() => {
    fetchAccounts();
  }, []);

  // 每秒更新当前时间，用于实时显示限流倒计时
  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentTime(new Date());
    }, 1000);

    return () => clearInterval(timer);
  }, []);

  const fetchAccounts = async () => {
    try {
      const accountsData = await apiService.getAccounts();
      setAccounts(accountsData);
    } catch (error: any) {
      showToast(error.message || '获取账户列表失败', 'error');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (account: Account) => {
    const confirmed = await showConfirm(
      '删除账户',
      `确定要删除账户 "${account.name}" 吗？\n\n此操作不可撤销，请谨慎操作。`,
      '删除',
      '取消'
    );
    
    if (confirmed) {
      setDeletingId(account.id);
      try {
        await apiService.deleteAccount(account.id);
        // 删除成功后，立即从本地状态中移除已删除的项目
        setAccounts(prevAccounts => prevAccounts.filter(acc => acc.id !== account.id));
        showToast('账户删除成功', 'success');
      } catch (error: any) {
        showToast(error.message || '删除账户失败', 'error');
        // 如果删除失败，重新获取数据以恢复正确状态
        await fetchAccounts();
      } finally {
        setDeletingId(null);
      }
    }
  };

  const handleEdit = (account: Account) => {
    setEditingAccount(account);
    setShowModal(true);
  };

  const handleAdd = () => {
    setEditingAccount(null);
    setShowModal(true);
  };

  const toggleAccountStatus = async (account: Account) => {
    try {
      await apiService.toggleAccountEnabled(account.id);
      await fetchAccounts();
      showToast(`账户已${!account.isEnabled ? '启用' : '禁用'}`, 'success');
    } catch (error: any) {
      showToast(error.message || '更新账户状态失败', 'error');
    }
  };

  const handleModalSuccess = () => {
    setShowModal(false);
    setEditingAccount(null);
    fetchAccounts();
    showToast(editingAccount ? '账户更新成功' : '账户创建成功', 'success');
  };

  const getPlatformConfig = (platform: string) => {
    return PLATFORMS.find(p => p.value === platform) || PLATFORMS[0];
  };

  const getRemainingTime = (rateLimitedUntil: string) => {
    const until = new Date(rateLimitedUntil);
    const diff = until.getTime() - currentTime.getTime();
    
    if (diff <= 0) return { text: '已解除', expired: true };
    
    const hours = Math.floor(diff / 3600000);
    const minutes = Math.floor((diff % 3600000) / 60000);
    const seconds = Math.floor((diff % 60000) / 1000);
    
    if (hours > 0) {
      return { text: `${hours}小时${minutes}分钟${seconds}秒`, expired: false };
    } else {
      return { text: `${minutes}分钟${seconds}秒`, expired: false };
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-2">
          <Users className="h-6 w-6" />
          <h1 className="text-2xl font-bold">账户管理</h1>
        </div>
        <Button onClick={handleAdd}>
          <Plus className="h-4 w-4 mr-2" />
          添加账户
        </Button>
      </div>

      <div className="grid gap-4">
        {accounts.map((account) => {
          const platformConfig = getPlatformConfig(account.platform);
          const PlatformIcon = platformConfig.icon;
          
          return (
            <Card key={account.id} className="hover:shadow-lg transition-shadow">
              <CardContent className="p-6">
                <div className="flex items-start justify-between mb-4">
                  <div className="flex items-start space-x-4">
                    <div className={`w-12 h-12 ${platformConfig.color} rounded-xl flex items-center justify-center flex-shrink-0`}>
                      <PlatformIcon className="w-6 h-6 text-white" />
                    </div>
                    <div className="flex-1">
                      <h3 className="text-lg font-semibold text-foreground mb-1">
                        {account.name}
                      </h3>
                      <div className="flex items-center space-x-2 mb-2">
                        <Badge variant="outline">{platformConfig.label}</Badge>
                        <Badge 
                          variant={account.isEnabled ? 'default' : 'secondary'}
                          className="cursor-pointer"
                          onClick={() => toggleAccountStatus(account)}
                        >
                          {account.isEnabled ? '可用' : '已禁用'}
                        </Badge>
                        {account.status === 'rate_limited' && (
                          <Badge variant="destructive">
                            限流中
                          </Badge>
                        )}
                        {account.status === 'error' && (
                          <Badge variant="destructive">
                            错误
                          </Badge>
                        )}
                        {account.accountType && (
                          <Badge variant="secondary">
                            {account.accountType === 'shared' ? '共享账户' : '专属账户'}
                          </Badge>
                        )}
                        {account.priority && account.platform !== 'gemini' && (
                          <Badge variant="outline">
                            优先级: {account.priority}
                          </Badge>
                        )}
                      </div>
                      {account.description && (
                        <p className="text-sm text-muted-foreground mb-2">{account.description}</p>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleEdit(account)}
                    >
                      <Edit2 className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      disabled={deletingId === account.id}
                      onClick={() => handleDelete(account)}
                    >
                      {deletingId === account.id ? (
                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-primary" />
                      ) : (
                        <Trash2 className="h-4 w-4" />
                      )}
                    </Button>
                  </div>
                </div>

                {/* 详细信息 */}
                <div className="space-y-3">
                  {/* 认证信息 */}
                  {account.sessionKey && (
                    <div>
                      <p className="text-sm font-medium text-foreground mb-1 flex items-center gap-1">
                        <Key className="w-4 h-4" />
                        Session Key
                      </p>
                      <p className="font-mono text-sm bg-muted p-2 rounded break-all">
                        {account.sessionKey.length > 50 
                          ? `${account.sessionKey.substring(0, 20)}...${account.sessionKey.substring(account.sessionKey.length - 20)}`
                          : account.sessionKey
                        }
                      </p>
                    </div>
                  )}

                  {/* Claude Console 特定信息 */}
                  {account.platform === 'claude-console' && account.apiUrl && (
                    <div>
                      <p className="text-sm font-medium text-foreground mb-1 flex items-center gap-1">
                        <Server className="w-4 h-4" />
                        API URL
                      </p>
                      <p className="text-sm bg-muted p-2 rounded break-all">
                        {account.apiUrl}
                      </p>
                    </div>
                  )}

                  {/* Gemini 项目信息 */}
                  {account.platform === 'gemini' && account.projectId && (
                    <div>
                      <p className="text-sm font-medium text-foreground mb-1 flex items-center gap-1">
                        <Settings className="w-4 h-4" />
                        项目编号
                      </p>
                      <p className="text-sm bg-muted p-2 rounded">
                        {account.projectId}
                      </p>
                    </div>
                  )}

                  {/* OpenAI 特定信息 */}
                  {account.platform === 'openai' && (
                    <>
                      {account.openAiOauth ? (
                        <div>
                          <p className="text-sm font-medium text-foreground mb-1 flex items-center gap-1">
                            <Key className="w-4 h-4" />
                            OpenAI OAuth 账户
                          </p>
                          <div className="bg-muted p-3 rounded">
                            {account.openAiOauth.userInfo && (
                              <div className="mb-2">
                                <p className="text-sm"><strong>用户:</strong> {account.openAiOauth.userInfo.name || account.openAiOauth.userInfo.email}</p>
                                <p className="text-sm"><strong>邮箱:</strong> {account.openAiOauth.userInfo.email}</p>
                              </div>
                            )}
                            <p className="text-sm"><strong>权限范围:</strong> {account.openAiOauth.scopes.join(', ')}</p>
                            <p className="text-sm"><strong>过期时间:</strong> {new Date(account.openAiOauth.expiresAt * 1000).toLocaleString()}</p>
                          </div>
                        </div>
                      ) : (
                        <>
                          {account.apiKey && (
                            <div>
                              <p className="text-sm font-medium text-foreground mb-1 flex items-center gap-1">
                                <Key className="w-4 h-4" />
                                API Key
                              </p>
                              <p className="font-mono text-sm bg-muted p-2 rounded break-all">
                                {account.apiKey.length > 50 
                                  ? `${account.apiKey.substring(0, 20)}...${account.apiKey.substring(account.apiKey.length - 20)}`
                                  : account.apiKey
                                }
                              </p>
                            </div>
                          )}
                        </>
                      )}
                      {account.baseUrl && (
                        <div>
                          <p className="text-sm font-medium text-foreground mb-1 flex items-center gap-1">
                            <Server className="w-4 h-4" />
                            Base URL
                          </p>
                          <p className="text-sm bg-muted p-2 rounded break-all">
                            {account.baseUrl}
                          </p>
                        </div>
                      )}
                    </>
                  )}

                  {/* 代理信息 */}
                  {account.proxy && (
                    <div>
                      <p className="text-sm font-medium text-foreground mb-1 flex items-center gap-1">
                        <Server className="w-4 h-4" />
                        代理设置
                      </p>
                      <p className="text-sm bg-muted p-2 rounded">
                        {account.proxy.type?.toUpperCase() || 'HTTP'}://{account.proxy.host}:{account.proxy.port}
                        {account.proxy.username && ' (需要认证)'}
                      </p>
                    </div>
                  )}

                  {/* 限流信息 */}
                  {account.status === 'rate_limited' && account.rateLimitedUntil && (() => {
                    const remainingTime = getRemainingTime(account.rateLimitedUntil);
                    return (
                      <div>
                        <p className="text-sm font-medium text-foreground mb-1 flex items-center gap-1">
                          <Activity className="w-4 h-4 text-red-500" />
                          限流信息
                        </p>
                        <div className={`border p-3 rounded ${remainingTime.expired ? 'bg-green-50 border-green-200' : 'bg-red-50 border-red-200'}`}>
                          <p className={`text-sm mb-1 ${remainingTime.expired ? 'text-green-700' : 'text-red-700'}`}>
                            <strong>解除时间：</strong>
                            {new Date(account.rateLimitedUntil).toLocaleString()}
                          </p>
                          <p className={`text-sm ${remainingTime.expired ? 'text-green-600' : 'text-red-600'}`}>
                            <strong>剩余时间：</strong>
                            <span className={remainingTime.expired ? 'text-green-600 font-semibold' : 'text-red-600'}>
                              {remainingTime.text}
                            </span>
                          </p>
                          {account.lastError && (
                            <p className="text-xs text-red-500 mt-2">
                              <strong>错误详情：</strong>{account.lastError}
                            </p>
                          )}
                          {remainingTime.expired && (
                            <p className="text-xs text-green-600 mt-2 font-medium">
                              💚 限流已解除，账户现在可以正常使用
                            </p>
                          )}
                        </div>
                      </div>
                    );
                  })()}

                  {/* 错误信息 */}
                  {account.status === 'error' && account.lastError && (
                    <div>
                      <p className="text-sm font-medium text-foreground mb-1 flex items-center gap-1">
                        <Activity className="w-4 h-4 text-red-500" />
                        错误信息
                      </p>
                      <div className="bg-red-50 border border-red-200 p-3 rounded">
                        <p className="text-sm text-red-700">{account.lastError}</p>
                      </div>
                    </div>
                  )}

                  {/* 统计信息 */}
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4 pt-3 border-t border-border">
                    <div className="flex items-center space-x-2">
                      <Calendar className="h-4 w-4 text-muted-foreground" />
                      <div>
                        <p className="text-xs text-muted-foreground">创建时间</p>
                        <p className="text-sm">{new Date(account.createdAt).toLocaleDateString()}</p>
                      </div>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Activity className="h-4 w-4 text-muted-foreground" />
                      <div>
                        <p className="text-xs text-muted-foreground">最后使用</p>
                        <p className="text-sm">
                          {account.lastUsedAt ? new Date(account.lastUsedAt).toLocaleString() : '从未使用'}
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Users className="h-4 w-4 text-muted-foreground" />
                      <div>
                        <p className="text-xs text-muted-foreground">状态</p>
                        <p className={`text-sm ${
                          !account.isEnabled ? 'text-red-600' : 
                          account.status === 'rate_limited' ? 'text-orange-600' :
                          account.status === 'error' ? 'text-red-600' :
                          'text-green-600'
                        }`}>
                          {!account.isEnabled ? '已停用' :
                           account.status === 'rate_limited' ? '限流中' :
                           account.status === 'error' ? '异常' :
                           '正常工作'}
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {accounts.length === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Users className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium mb-2">暂无账户</h3>
            <p className="text-muted-foreground text-center mb-4">
              还没有添加任何AI平台账户。点击上方按钮添加您的第一个账户。
            </p>
            <Button onClick={handleAdd}>
              <Plus className="h-4 w-4 mr-2" />
              添加账户
            </Button>
          </CardContent>
        </Card>
      )}

      {/* 账户模态框 */}
      <AccountModal
        show={showModal}
        account={editingAccount}
        onClose={() => {
          setShowModal(false);
          setEditingAccount(null);
        }}
        onSuccess={handleModalSuccess}
      />

      {/* 删除确认模态框 */}
      <ConfirmModal
        show={showConfirmModal}
        title={confirmOptions.title}
        message={confirmOptions.message}
        confirmText={confirmOptions.confirmText}
        cancelText={confirmOptions.cancelText}
        onConfirm={handleConfirm}
        onCancel={handleCancel}
      />
    </div>
  );
}
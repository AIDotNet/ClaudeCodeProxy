import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Modal } from '@/components/ui/modal';
import { UserCircle, ChevronRight, Plus, Trash2, Settings, Zap } from 'lucide-react';
import { apiService, type Account, type OAuthTokenInfo } from '@/services/api';
import { showToast } from '@/utils/toast';
import { useConfirm } from '@/hooks/useConfirm';
import ProxyConfigComponent from './ProxyConfig';
import OAuthFlow from './OAuthFlow';
import ConfirmModal from '../common/ConfirmModal';

interface AccountModalProps {
  show: boolean;
  account?: Account | null;
  onClose: () => void;
  onSuccess: (account?: Account) => void;
}

interface ModelMapping {
  from: string;
  to: string;
}

export default function AccountModal({ show, account, onClose, onSuccess }: AccountModalProps) {
  const isEdit = !!account;
  const { showConfirmModal, confirmOptions, showConfirm, handleConfirm, handleCancel } = useConfirm();
  
  // OAuth步骤
  const [oauthStep, setOauthStep] = useState(1);
  const [loading, setLoading] = useState(false);

  // 表单数据
  interface ProxyConfig {
    enabled: boolean;
    type: 'socks5' | 'http' | 'https';
    host: string;
    port: string;
    username?: string;
    password?: string;
  }

  const [form, setForm] = useState({
    platform: 'claude' as 'claude' | 'claude-console' | 'gemini' | 'openai' | 'thor',
    addType: 'oauth' as 'oauth' | 'manual',
    name: '',
    description: '',
    accountType: 'shared' as 'shared' | 'dedicated',
    projectId: '',
    accessToken: '',
    refreshToken: '',
    apiUrl: '',
    apiKey: '',
    priority: 50,
    userAgent: '',
    rateLimitDuration: 60,
    proxy: {
      enabled: false,
      type: 'socks5' as 'socks5' | 'http' | 'https',
      host: '',
      port: '',
      username: '',
      password: ''
    }
  });

  // 模型映射表
  const [modelMappings, setModelMappings] = useState<ModelMapping[]>([]);

  // 默认的模型选项
  const defaultModels = [
    'claude-3-5-haiku-20241022',
    'claude-sonnet-4-20250514'
  ];

  // 表单验证错误
  const [errors, setErrors] = useState({
    name: '',
    accessToken: '',
    apiUrl: '',
    apiKey: ''
  });

  // 初始化表单数据
  useEffect(() => {
    if (account) {
      setForm({
        platform: account.platform,
        addType: account.platform === 'claude-console' ? 'manual' : 'oauth',
        name: account.name,
        description: account.description || '',
        accountType: account.accountType || 'shared',
        projectId: account.projectId || '',
        accessToken: '',
        refreshToken: '',
        apiUrl: account.apiUrl || '',
        apiKey: '', // 编辑时不显示现有的API Key
        priority: account.priority || 50,
        userAgent: account.userAgent || '',
        rateLimitDuration: account.rateLimitDuration || 60,
        proxy: account.proxy ? {
          enabled: true,
          type: account.proxy.type as 'socks5' | 'http' | 'https',
          host: account.proxy.host,
          port: account.proxy.port?.toString() || '',
          username: account.proxy.username || '',
          password: account.proxy.password || ''
        } : {
          enabled: false,
          type: 'socks5',
          host: '',
          port: '',
          username: '',
          password: ''
        }
      });

      // 初始化模型映射表
      if (account.supportedModels) {
        if (Array.isArray(account.supportedModels)) {
          // 后端存储格式：["from:to", "model1:target1"]
          const mappings = account.supportedModels
            .map(mapping => {
              const parts = mapping.split(':', 2);
              return parts.length === 2 ? { from: parts[0].trim(), to: parts[1].trim() } : null;
            })
            .filter(Boolean) as ModelMapping[];
          setModelMappings(mappings);
        } else if (typeof account.supportedModels === 'object') {
          // 对象格式
          const mappings = Object.entries(account.supportedModels).map(([from, to]) => ({
            from,
            to: String(to)
          }));
          setModelMappings(mappings);
        } else {
          setModelMappings([]);
        }
      } else {
        setModelMappings([]);
      }
    } else {
      // 重置表单
      setForm({
        platform: 'claude',
        addType: 'oauth',
        name: '',
        description: '',
        accountType: 'shared',
        projectId: '',
        accessToken: '',
        refreshToken: '',
        apiUrl: '',
        apiKey: '',
        priority: 50,
        userAgent: '',
        rateLimitDuration: 60,
        proxy: {
          enabled: false,
          type: 'socks5',
          host: '',
          port: '',
          username: '',
          password: ''
        }
      });
      setModelMappings([]);
      setOauthStep(1);
      setErrors({ name: '', accessToken: '', apiUrl: '', apiKey: '' });
    }
  }, [account, show]);

  // 平台变化处理
  useEffect(() => {
    if (form.platform === 'claude-console') {
      setForm(prev => ({ ...prev, addType: 'manual' }));
    } else if (form.platform === 'openai') {
      setForm(prev => ({ 
        ...prev, 
        addType: 'oauth',
        apiUrl: prev.apiUrl || 'https://api.openai.com/v1'
      }));
    } else if (form.platform === 'thor') {
      setForm(prev => ({ 
        ...prev, 
        addType: 'oauth',
        apiUrl: prev.apiUrl || 'https://api.token-ai.cn/v1'
      }));
    }
  }, [form.platform]);

  const updateForm = (field: string, value: any) => {
    setForm(prev => ({ ...prev, [field]: value }));
    
    // 清除对应的错误
    if (errors[field as keyof typeof errors]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const canProceed = form.name?.trim() && form.platform;

  const nextStep = async () => {
    if (!canProceed) {
      if (!form.name?.trim()) {
        setErrors(prev => ({ ...prev, name: '请填写账户名称' }));
      }
      return;
    }

    // Gemini项目编号检查
    if (form.platform === 'gemini' && oauthStep === 1 && form.addType === 'oauth') {
      if (!form.projectId?.trim()) {
        const confirmed = await showConfirm(
          '项目编号未填写',
          '您尚未填写项目编号。\n\n如果您的Google账号绑定了Google Cloud或被识别为Workspace账号，需要提供项目编号。\n如果您使用的是普通个人账号，可以继续不填写。',
          '继续',
          '返回填写'
        );
        if (!confirmed) return;
      }
    }

    setOauthStep(2);
  };

  const handleOAuthSuccess = async (tokenInfo: OAuthTokenInfo) => {
    setLoading(true);
    try {
      const data = {
        name: form.name,
        platform: form.platform,
        description: form.description,
        accountType: form.accountType,
        proxy: form.proxy.enabled ? {
          type: form.proxy.type,
          host: form.proxy.host,
          port: parseInt(form.proxy.port),
          username: form.proxy.username || null,
          password: form.proxy.password || null
        } : null
      };

      if (form.platform === 'claude') {
        Object.assign(data, {
          claudeAiOauth: tokenInfo.claudeAiOauth || tokenInfo,
          priority: form.priority || 50
        });
      } else if (form.platform === 'gemini') {
        Object.assign(data, {
          geminiOauth: tokenInfo.tokens || tokenInfo
        });
        if (form.projectId) {
          Object.assign(data, { projectId: form.projectId });
        }
      } else if (form.platform === 'thor') {
        Object.assign(data, {
          apiKey: tokenInfo.apiKey,
          apiUrl: tokenInfo.baseUrl || 'https://api.token-ai.cn/v1',
          priority: form.priority || 50
        });
      } else if (form.platform === 'openai') {
        Object.assign(data, {
          openAiOauth: tokenInfo.openAiOauth || tokenInfo,
          priority: form.priority || 50
        });
      }

      let result;
      if (form.platform === 'claude') {
        result = await apiService.createClaudeAccount(data);
      } else if (form.platform === 'gemini') {
        result = await apiService.createGeminiAccount(data);
      } else if (form.platform === 'thor') {
        result = await apiService.createAccount(data);
      } else if (form.platform === 'openai') {
        result = await apiService.createAccount(data);
      }

      onSuccess(result);
    } catch (error: any) {
      showToast(error.message || '账户创建失败', 'error');
    } finally {
      setLoading(false);
    }
  };

  const createAccount = async () => {
    // 验证表单
    let hasError = false;
    const newErrors = { name: '', accessToken: '', apiUrl: '', apiKey: '' };

    if (!form.name?.trim()) {
      newErrors.name = '请填写账户名称';
      hasError = true;
    }

    if (form.platform === 'claude-console') {
      if (!form.apiUrl?.trim()) {
        newErrors.apiUrl = '请填写 API URL';
        hasError = true;
      }
      if (!form.apiKey?.trim()) {
        newErrors.apiKey = '请填写 API Key';
        hasError = true;
      }
    } else if (form.platform === 'openai') {
      if (!form.apiKey?.trim()) {
        newErrors.apiKey = '请填写 API Key';
        hasError = true;
      }
    } else if (form.addType === 'manual' && !form.accessToken?.trim()) {
      newErrors.accessToken = '请填写 Access Token';
      hasError = true;
    }

    if (hasError) {
      setErrors(newErrors);
      return;
    }

    setLoading(true);
    try {
      const data = {
        name: form.name,
        platform: form.platform,
        description: form.description,
        accountType: form.accountType,
        proxy: form.proxy.enabled ? {
          type: form.proxy.type,
          host: form.proxy.host,
          port: parseInt(form.proxy.port),
          username: form.proxy.username || null,
          password: form.proxy.password || null
        } : null
      };

      if (form.platform === 'claude') {
        const expiresInMs = form.refreshToken ? (10 * 60 * 1000) : (365 * 24 * 60 * 60 * 1000);
        Object.assign(data, {
          claudeAiOauth: {
            accessToken: form.accessToken,
            refreshToken: form.refreshToken || '',
            expiresAt: Date.now() + expiresInMs,
            scopes: ['user:inference']
          },
          priority: form.priority || 50
        });
      } else if (form.platform === 'gemini') {
        const expiresInMs = form.refreshToken ? (10 * 60 * 1000) : (365 * 24 * 60 * 60 * 1000);
        Object.assign(data, {
          geminiOauth: {
            access_token: form.accessToken,
            refresh_token: form.refreshToken || '',
            scope: 'https://www.googleapis.com/auth/cloud-platform',
            token_type: 'Bearer',
            expiry_date: Date.now() + expiresInMs
          }
        });
        if (form.projectId) {
          Object.assign(data, { projectId: form.projectId });
        }
      } else if (form.platform === 'claude-console') {
        Object.assign(data, {
          apiUrl: form.apiUrl,
          apiKey: form.apiKey,
          priority: form.priority || 50,
          supportedModels: convertMappingsToObject() || {},
          userAgent: form.userAgent || null,
          rateLimitDuration: form.rateLimitDuration || 60
        });
      } else if (form.platform === 'openai') {
        Object.assign(data, {
          apiUrl: form.apiUrl || 'https://api.openai.com/v1',
          apiKey: form.apiKey,
          priority: form.priority || 50,
          supportedModels: convertMappingsToObject() || {},
          userAgent: form.userAgent || null,
          rateLimitDuration: form.rateLimitDuration || 60
        });
      }

      let result;
      if (form.platform === 'claude') {
        result = await apiService.createClaudeAccount(data);
      } else if (form.platform === 'claude-console') {
        result = await apiService.createClaudeConsoleAccount(data);
      } else if (form.platform === 'openai') {
        result = await apiService.createAccount(data);
      } else if (form.platform === 'thor') {
        result = await apiService.createAccount(data);
      } else {
        result = await apiService.createGeminiAccount(data);
      }

      onSuccess(result);
    } catch (error: any) {
      showToast(error.message || '账户创建失败', 'error');
    } finally {
      setLoading(false);
    }
  };

  const updateAccount = async () => {
    if (!form.name?.trim()) {
      setErrors(prev => ({ ...prev, name: '请填写账户名称' }));
      return;
    }

    // Gemini项目编号检查
    if (form.platform === 'gemini' && !form.projectId?.trim()) {
      const confirmed = await showConfirm(
        '项目编号未填写',
        '您尚未填写项目编号。\n\n如果您的Google账号绑定了Google Cloud或被识别为Workspace账号，需要提供项目编号。\n如果您使用的是普通个人账号，可以继续保存。',
        '继续保存',
        '返回填写'
      );
      if (!confirmed) return;
    }

    setLoading(true);
    try {
          const data = {
      name: form.name,
      platform: form.platform,
      description: form.description,
      accountType: form.accountType,
      proxy: form.proxy.enabled ? {
        type: form.proxy.type,
        host: form.proxy.host,
        port: parseInt(form.proxy.port),
        username: form.proxy.username || null,
        password: form.proxy.password || null
      } : null
    };

      // 只有非空时才更新token
      if (form.accessToken || form.refreshToken) {
        if (account!.platform === 'claude') {
          const expiresInMs = form.refreshToken ? (10 * 60 * 1000) : (365 * 24 * 60 * 60 * 1000);
          Object.assign(data, {
            claudeAiOauth: {
              accessToken: form.accessToken || '',
              refreshToken: form.refreshToken || '',
              expiresAt: Date.now() + expiresInMs,
              scopes: ['user:inference']
            }
          });
        } else if (account!.platform === 'gemini') {
          const expiresInMs = form.refreshToken ? (10 * 60 * 1000) : (365 * 24 * 60 * 60 * 1000);
          Object.assign(data, {
            geminiOauth: {
              access_token: form.accessToken || '',
              refresh_token: form.refreshToken || '',
              scope: 'https://www.googleapis.com/auth/cloud-platform',
              token_type: 'Bearer',
              expiry_date: Date.now() + expiresInMs
            }
          });
        }
      }

      if (account!.platform === 'gemini' && form.projectId) {
        Object.assign(data, { projectId: form.projectId });
      }

      if (account!.platform === 'claude') {
        Object.assign(data, { priority: form.priority || 50 });
      }

      if (account!.platform === 'claude-console') {
        Object.assign(data, {
          apiUrl: form.apiUrl,
          priority: form.priority || 50,
          supportedModels: convertMappingsToObject() || {},
          userAgent: form.userAgent || null,
          rateLimitDuration: form.rateLimitDuration || 60
        });
        if (form.apiKey) {
          Object.assign(data, { apiKey: form.apiKey });
        }
      }

      if (account!.platform === 'openai') {
        Object.assign(data, {
          apiUrl: form.apiUrl || 'https://api.openai.com/v1',
          priority: form.priority || 50,
          supportedModels: convertMappingsToObject() || {},
          userAgent: form.userAgent || null,
          rateLimitDuration: form.rateLimitDuration || 60
        });
        if (form.apiKey) {
          Object.assign(data, { apiKey: form.apiKey });
        }
      }

      if (account!.platform === 'thor') {
        Object.assign(data, {
          apiUrl: form.apiUrl || 'https://api.token-ai.cn/v1',
          priority: form.priority || 50,
          supportedModels: convertMappingsToObject() || {},
          userAgent: form.userAgent || null,
          rateLimitDuration: form.rateLimitDuration || 60
        });
        if (form.apiKey) {
          Object.assign(data, { apiKey: form.apiKey });
        }
      }

      if (account!.platform === 'claude') {
        await apiService.updateClaudeAccount(account!.id, data);
      } else if (account!.platform === 'claude-console') {
        await apiService.updateClaudeConsoleAccount(account!.id, data);
      } else if (account!.platform === 'openai') {
        await apiService.updateAccount(account!.id, data);
      } else if (account!.platform === 'thor') {
        await apiService.updateAccount(account!.id, data);
      } else {
        await apiService.updateGeminiAccount(account!.id, data);
      }

      onSuccess();
    } catch (error: any) {
      showToast(error.message || '账户更新失败', 'error');
    } finally {
      setLoading(false);
    }
  };


  const convertMappingsToObject = () => {
    const mapping: Record<string, string> = {};
    modelMappings.forEach(item => {
      if (item.from && item.to) {
        mapping[item.from] = item.to;
      }
    });
    return Object.keys(mapping).length > 0 ? mapping : null;
  };

  const renderStepIndicator = () => {
    if (isEdit || form.addType !== 'oauth') return null;
    
    return (
      <div className="flex items-center justify-center mb-8">
        <div className="flex items-center space-x-4">
          <div className="flex items-center">
            <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-semibold ${
              oauthStep >= 1 ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground'
            }`}>
              1
            </div>
            <span className="ml-2 text-sm font-medium text-foreground">基本信息</span>
          </div>
          <div className="w-8 h-0.5 bg-border" />
          <div className="flex items-center">
            <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-semibold ${
              oauthStep >= 2 ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground'
            }`}>
              2
            </div>
            <span className="ml-2 text-sm font-medium text-foreground">授权认证</span>
          </div>
        </div>
      </div>
    );
  };

  const renderActionButtons = () => {
    const isOAuthNextStep = form.addType === 'oauth' && form.platform !== 'claude-console' && !isEdit && oauthStep === 1;
    
    return (
      <div className="flex gap-4 pt-6 border-t border-border">
        <Button 
          variant="outline" 
          className="flex-1 py-3" 
          onClick={onClose}
        >
          取消
        </Button>
        
        {isOAuthNextStep ? (
          <Button 
            disabled={!canProceed} 
            className="flex-1 py-3" 
            onClick={nextStep}
          >
            下一步
            <ChevronRight className="w-4 h-4 ml-2" />
          </Button>
        ) : (
          <Button 
            disabled={loading}
            className="flex-1 py-3"
            onClick={isEdit ? updateAccount : createAccount}
          >
            {loading ? (
              <div className="flex items-center gap-2">
                <div className="w-4 h-4 border-2 border-primary-foreground border-t-transparent rounded-full animate-spin"></div>
                处理中...
              </div>
            ) : (
              <span>{isEdit ? '保存更改' : '创建账户'}</span>
            )}
          </Button>
        )}
      </div>
    );
  };

  return (
    <Modal
      isOpen={show}
      onClose={onClose}
      title={isEdit ? '编辑账户' : '添加账户'}
      subtitle={isEdit ? '修改账户信息和配置' : '配置新的AI平台账户'}
      icon={<UserCircle className="w-6 h-6 text-primary-foreground" />}
      size="5xl"
      key={`account-modal-${isEdit ? account?.id : 'new'}-${oauthStep}`}
    >
      <div className="space-y-6">
          {/* 步骤指示器 */}
          {renderStepIndicator()}

          {/* 步骤1: 基本信息 */}
          {(oauthStep === 1 && !isEdit) && (
            <div className="space-y-6">
              {/* 平台选择 */}
              {!isEdit && (
                <div className="space-y-2">
                  <Label className="text-sm font-semibold">平台</Label>
                  <div className="flex gap-4">
                    <label className="flex items-center cursor-pointer">
                      <input 
                        type="radio" 
                        name="platform"
                        value="claude"
                        checked={form.platform === 'claude'}
                        onChange={(e) => updateForm('platform', e.target.value)}
                        className="mr-2"
                      />
                      <span className="text-sm text-foreground">Claude</span>
                    </label>
                    <label className="flex items-center cursor-pointer">
                      <input 
                        type="radio" 
                        name="platform"
                        value="claude-console"
                        checked={form.platform === 'claude-console'}
                        onChange={(e) => updateForm('platform', e.target.value)}
                        className="mr-2"
                      />
                      <span className="text-sm text-foreground">Claude Console</span>
                    </label>
                    <label className="flex items-center cursor-pointer">
                      <input 
                        type="radio" 
                        name="platform"
                        value="gemini"
                        checked={form.platform === 'gemini'}
                        onChange={(e) => updateForm('platform', e.target.value)}
                        className="mr-2"
                      />
                      <span className="text-sm text-foreground">Gemini</span>
                    </label>
                    <label className="flex items-center cursor-pointer">
                      <input 
                        type="radio" 
                        name="platform"
                        value="openai"
                        checked={form.platform === 'openai'}
                        onChange={(e) => updateForm('platform', e.target.value)}
                        className="mr-2"
                      />
                      <span className="text-sm text-foreground">OpenAI</span>
                    </label>
                    <label className="flex items-center cursor-pointer">
                      <input 
                        type="radio" 
                        name="platform"
                        value="thor"
                        checked={form.platform === 'thor'}
                        onChange={(e) => updateForm('platform', e.target.value)}
                        className="mr-2"
                      />
                      <span className="text-sm text-foreground">Thor</span>
                    </label>
                  </div>
                </div>
              )}

              {/* 添加方式 */}
              {!isEdit && form.platform !== 'claude-console' && form.platform !== 'thor' && (
                <div className="space-y-2">
                  <Label className="text-sm font-semibold">添加方式</Label>
                  <div className="flex gap-4">
                    <label className="flex items-center cursor-pointer">
                      <input 
                        type="radio" 
                        name="addType"
                        value="oauth"
                        checked={form.addType === 'oauth'}
                        onChange={(e) => updateForm('addType', e.target.value)}
                        className="mr-2"
                      />
                      <span className="text-sm text-foreground">
                        {form.platform === 'openai' ? 'OAuth 授权 (推荐)' : 
                        // @ts-ignore
                         form.platform === 'thor' ? 'Token 快捷获取 (推荐)' : 
                         'OAuth 授权 (推荐)'}
                      </span>
                    </label>
                    <label className="flex items-center cursor-pointer">
                      <input 
                        type="radio" 
                        name="addType"
                        value="manual"
                        checked={form.addType === 'manual'}
                        onChange={(e) => updateForm('addType', e.target.value)}
                        className="mr-2"
                      />
                      <span className="text-sm text-foreground">
                        {form.platform === 'openai' ? '手动输入 API Key' : '手动输入 Access Token'}
                      </span>
                    </label>
                  </div>
                </div>
              )}

              {/* 基本信息字段 */}
              <div className="space-y-2">
                <Label>账户名称</Label>
                <Input
                  value={form.name}
                  onChange={(e) => updateForm('name', e.target.value)}
                  placeholder="为账户设置一个易识别的名称"
                  className={errors.name ? 'border-destructive' : ''}
                />
                {errors.name && <p className="text-destructive text-sm">{errors.name}</p>}
              </div>

              <div className="space-y-2">
                <Label>描述 (可选)</Label>
                <textarea
                  value={form.description}
                  onChange={(e) => updateForm('description', e.target.value)}
                  rows={3}
                  className="w-full p-2 border border-input bg-background text-foreground rounded-md resize-none"
                  placeholder="账户用途说明..."
                />
              </div>

              <div className="space-y-2">
                <Label>账户类型</Label>
                <div className="flex gap-4">
                  <label className="flex items-center cursor-pointer">
                    <input 
                      type="radio" 
                      name="accountType"
                      value="shared"
                      checked={form.accountType === 'shared'}
                      onChange={(e) => updateForm('accountType', e.target.value)}
                      className="mr-2"
                    />
                    <span className="text-sm text-foreground">共享账户</span>
                  </label>
                  <label className="flex items-center cursor-pointer">
                    <input 
                      type="radio" 
                      name="accountType"
                      value="dedicated"
                      checked={form.accountType === 'dedicated'}
                      onChange={(e) => updateForm('accountType', e.target.value)}
                      className="mr-2"
                    />
                    <span className="text-sm text-foreground">专属账户</span>
                  </label>
                </div>
                <p className="text-xs text-muted-foreground">
                  共享账户：供所有API Key使用；专属账户：仅供特定API Key使用
                </p>
              </div>

              {/* Gemini 项目编号 */}
              {form.platform === 'gemini' && (
                <div className="space-y-2">
                  <Label>项目编号 (可选)</Label>
                  <Input
                    value={form.projectId}
                    onChange={(e) => updateForm('projectId', e.target.value)}
                    placeholder="例如：123456789012（纯数字）"
                  />
                  <div className="p-3 bg-muted border border-border rounded-xl">
                    <p className="text-xs text-muted-foreground">
                      <strong>Google Cloud/Workspace 账号需要提供项目编号</strong><br/>
                      某些 Google 账号（特别是绑定了 Google Cloud 的账号）会被识别为 Workspace 账号，需要提供额外的项目编号。
                    </p>
                  </div>
                </div>
              )}

              {/* Claude Console 特定字段 */}
              {form.platform === 'claude-console' && !isEdit && (
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label>API URL *</Label>
                    <Input
                      value={form.apiUrl}
                      onChange={(e) => updateForm('apiUrl', e.target.value)}
                      placeholder="例如：https://api.example.com"
                      className={errors.apiUrl ? 'border-destructive' : ''}
                    />
                    {errors.apiUrl && <p className="text-destructive text-sm">{errors.apiUrl}</p>}
                  </div>

                  <div className="space-y-2">
                    <Label>API Key *</Label>
                    <Input
                      type="password"
                      value={form.apiKey}
                      onChange={(e) => updateForm('apiKey', e.target.value)}
                      placeholder="请输入API Key"
                      className={errors.apiKey ? 'border-destructive' : ''}
                    />
                    {errors.apiKey && <p className="text-destructive text-sm">{errors.apiKey}</p>}
                  </div>
                </div>
              )}

              {/* Thor 特定字段 */}
              {form.platform === 'thor' && !isEdit && (
                <div className="space-y-4">
                  <div className="p-4 bg-blue-50 dark:bg-blue-950 border border-blue-200 dark:border-blue-800 rounded-lg">
                    <div className="flex items-start space-x-3">
                      <Zap className="h-5 w-5 text-blue-600 dark:text-blue-400 mt-0.5" />
                      <div>
                        <h4 className="font-medium text-blue-900 dark:text-blue-100">Thor 平台说明</h4>
                        <p className="text-sm text-blue-700 dark:text-blue-300 mt-1">
                          Thor平台使用OpenAI兼容格式，支持自动获取Token的快捷访问。点击下一步后将跳转到授权页面获取Token。
                        </p>
                        <p className="text-xs text-blue-600 dark:text-blue-400 mt-2">
                          Base URL: https://api.token-ai.cn/v1
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* OpenAI 手动配置字段 - 仅在选择manual方式时显示 */}
              {form.platform === 'openai' && form.addType === 'manual' && !isEdit && (
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label>Base URL</Label>
                    <Input
                      value={form.apiUrl}
                      onChange={(e) => updateForm('apiUrl', e.target.value)}
                      placeholder="例如：https://api.openai.com/v1"
                      className="bg-background text-foreground"
                    />
                    <p className="text-xs text-muted-foreground">
                      OpenAI官方地址：https://api.openai.com/v1 或使用兼容的第三方地址
                    </p>
                  </div>

                  <div className="space-y-2">
                    <Label>API Key *</Label>
                    <Input
                      type="password"
                      value={form.apiKey}
                      onChange={(e) => updateForm('apiKey', e.target.value)}
                      placeholder="请输入API Key，如：sk-..."
                      className={errors.apiKey ? 'border-destructive' : ''}
                    />
                    {errors.apiKey && <p className="text-destructive text-sm">{errors.apiKey}</p>}
                  </div>

                  <div className="space-y-2">
                    <Label>User Agent (可选)</Label>
                    <Input
                      value={form.userAgent}
                      onChange={(e) => updateForm('userAgent', e.target.value)}
                      placeholder="自定义User Agent"
                      className="bg-background text-foreground"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label>请求间隔 (秒)</Label>
                    <Input
                      type="number"
                      min="1"
                      value={form.rateLimitDuration}
                      onChange={(e) => updateForm('rateLimitDuration', parseInt(e.target.value) || 60)}
                      placeholder="默认60秒"
                      className="bg-background text-foreground"
                    />
                  </div>

                </div>
              )}

              {/* OpenAI OAuth 说明 - 仅在选择oauth方式时显示 */}
              {form.platform === 'openai' && form.addType === 'oauth' && !isEdit && (
                <div className="space-y-4">
                  <div className="p-4 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg">
                    <div className="flex items-start space-x-3">
                      <Zap className="h-5 w-5 text-slate-600 dark:text-slate-400 mt-0.5" />
                      <div>
                        <h4 className="font-medium text-slate-900 dark:text-slate-100">OpenAI OAuth 授权</h4>
                        <p className="text-sm text-slate-700 dark:text-slate-300 mt-1">
                          通过 OpenAI 官方 OAuth 授权方式安全地绑定您的 OpenAI 账户，无需手动输入 API Key。
                        </p>
                        <p className="text-xs text-slate-600 dark:text-slate-400 mt-2">
                          点击下一步后将引导您完成 OAuth 授权流程
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* 优先级设置 */}
              {(form.platform === 'claude' || form.platform === 'claude-console' || form.platform === 'openai' || form.platform === 'thor') && (
                <div className="space-y-2">
                  <Label>调度优先级 (1-100)</Label>
                  <Input
                    type="number"
                    min="1"
                    max="100"
                    value={form.priority}
                    onChange={(e) => updateForm('priority', parseInt(e.target.value) || 50)}
                    placeholder="数字越小优先级越高，默认50"
                  />
                  <p className="text-xs text-muted-foreground">
                    数字越小优先级越高，建议范围：1-100
                  </p>
                </div>
              )}

              {/* 手动输入 Token 字段 */}
              {form.addType === 'manual' && form.platform !== 'claude-console' && form.platform !== 'openai' && form.platform !== 'thor' && (
                <Card className="border-border bg-card/50">
                  <CardContent className="p-4 space-y-4">
                    <div className="space-y-2">
                      <Label>Access Token *</Label>
                      <textarea
                        value={form.accessToken}
                        onChange={(e) => updateForm('accessToken', e.target.value)}
                        rows={4}
                        className={`w-full p-2 border rounded-md resize-none font-mono text-xs ${
                          errors.accessToken ? 'border-destructive' : 'border-input'
                        }`}
                        placeholder="请输入 Access Token..."
                      />
                      {errors.accessToken && <p className="text-destructive text-sm">{errors.accessToken}</p>}
                    </div>

                    <div className="space-y-2">
                      <Label>Refresh Token (可选)</Label>
                      <textarea
                        value={form.refreshToken}
                        onChange={(e) => updateForm('refreshToken', e.target.value)}
                        rows={4}
                        className="w-full p-2 border border-input bg-background text-foreground rounded-md resize-none font-mono text-xs"
                        placeholder="请输入 Refresh Token..."
                      />
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* 代理设置 */}
              <ProxyConfigComponent
                value={form.proxy}
                onChange={(proxy: ProxyConfig) => updateForm('proxy', proxy)}
              />
            </div>
          )}

          {/* 步骤2: OAuth授权 */}
          {oauthStep === 2 && form.addType === 'oauth' && (
            <OAuthFlow
              platform={form.platform as 'claude' | 'gemini' | 'thor' | 'openai'}
              proxy={form.proxy}
              onSuccess={handleOAuthSuccess}
              onBack={() => setOauthStep(1)}
            />
          )}

          {/* 编辑模式 */}
          {isEdit && (
            <div className="space-y-8">
              {/* 平台信息卡片 */}
              <Card className="border border-border bg-card/50">
                <CardContent className="p-6">
                  <div className="flex items-center gap-3 mb-4">
                    <div className={`w-8 h-8 rounded-xl flex items-center justify-center ${
                      form.platform === 'claude' ? 'bg-primary' :
                      form.platform === 'claude-console' ? 'bg-primary' :
                      form.platform === 'openai' ? 'bg-primary' : 'bg-primary'
                    }`}>
                      <UserCircle className="w-4 h-4 text-primary-foreground" />
                    </div>
                    <div>
                      <h3 className="font-semibold text-foreground">平台信息</h3>
                      <p className="text-sm text-muted-foreground">
                        {form.platform === 'claude' ? 'Claude AI官方账户' :
                         form.platform === 'claude-console' ? 'Claude Console API' :
                         form.platform === 'openai' ? 'OpenAI API' :
                         form.platform === 'thor' ? 'Thor平台 (OpenAI兼容)' : 'Google Gemini'}
                      </p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* 基本信息卡片 */}
              <Card className="border border-border bg-card/50">
                <CardContent className="p-6">
                  <h3 className="font-semibold text-foreground mb-4 flex items-center gap-2">
                    <div className="w-2 h-2 bg-primary rounded-full"></div>
                    基本信息
                  </h3>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <div className="space-y-2">
                      <Label className="text-sm font-medium text-foreground">账户名称 *</Label>
                      <Input
                        value={form.name}
                        onChange={(e) => updateForm('name', e.target.value)}
                        placeholder="为账户设置一个易识别的名称"
                        className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                      />
                      {errors.name && <p className="text-destructive text-xs">{errors.name}</p>}
                    </div>
                    
                    <div className="space-y-2">
                      <Label className="text-sm font-medium text-foreground">账户类型</Label>
                      <div className="flex gap-6">
                        <label className="flex items-center cursor-pointer group">
                          <input 
                            type="radio" 
                            name="accountType"
                            value="shared"
                            checked={form.accountType === 'shared'}
                            onChange={(e) => updateForm('accountType', e.target.value)}
                            className="w-4 h-4 text-foreground border-input focus:ring-ring"
                          />
                          <span className="ml-2 text-sm text-foreground group-hover:text-foreground">共享账户</span>
                        </label>
                        <label className="flex items-center cursor-pointer group">
                          <input 
                            type="radio" 
                            name="accountType"
                            value="dedicated"
                            checked={form.accountType === 'dedicated'}
                            onChange={(e) => updateForm('accountType', e.target.value)}
                            className="w-4 h-4 text-foreground border-input focus:ring-ring"
                          />
                          <span className="ml-2 text-sm text-foreground group-hover:text-foreground">专属账户</span>
                        </label>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        共享账户：供所有API Key使用；专属账户：仅供特定API Key使用
                      </p>
                    </div>
                  </div>
                  
                  <div className="mt-6 space-y-2">
                    <Label className="text-sm font-medium text-foreground">描述 (可选)</Label>
                    <textarea
                      value={form.description}
                      onChange={(e) => updateForm('description', e.target.value)}
                      rows={3}
                      className="w-full p-3 border border-input rounded-xl resize-none focus:border-ring focus:ring-ring focus:ring-1"
                      placeholder="账户用途说明..."
                    />
                  </div>
                </CardContent>
              </Card>

              {/* Gemini 特定配置 */}
              {form.platform === 'gemini' && (
                <Card className="border border-border bg-card/50">
                  <CardContent className="p-6">
                    <h3 className="font-semibold text-foreground mb-4 flex items-center gap-2">
                      <div className="w-2 h-2 bg-primary rounded-full"></div>
                      Gemini 配置
                    </h3>
                    <div className="space-y-2">
                      <Label className="text-sm font-medium text-foreground">项目编号 (可选)</Label>
                      <Input
                        value={form.projectId}
                        onChange={(e) => updateForm('projectId', e.target.value)}
                        placeholder="例如：123456789012（纯数字）"
                        className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                      />
                      <div className="p-3 bg-muted border border-border rounded-xl">
                        <p className="text-xs text-muted-foreground">
                          <strong>提示：</strong>某些 Google 账号（特别是绑定了 Google Cloud 的账号）会被识别为 Workspace 账号，需要提供项目编号。
                        </p>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Claude Console 特定配置 */}
              {form.platform === 'claude-console' && (
                <Card className="border border-border bg-card/50">
                  <CardContent className="p-6">
                    <h3 className="font-semibold text-foreground mb-4 flex items-center gap-2">
                      <div className="w-2 h-2 bg-primary rounded-full"></div>
                      Claude Console 配置
                    </h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">API URL *</Label>
                        <Input
                          value={form.apiUrl}
                          onChange={(e) => updateForm('apiUrl', e.target.value)}
                          placeholder="例如：https://api.example.com"
                          className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                        />
                      </div>

                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">API Key</Label>
                        <Input
                          type="password"
                          value={form.apiKey}
                          onChange={(e) => updateForm('apiKey', e.target.value)}
                          placeholder="留空表示不更新..."
                          className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                        />
                        <p className="text-xs text-muted-foreground">留空表示保持当前API Key不变</p>
                      </div>

                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">User Agent (可选)</Label>
                        <Input
                          value={form.userAgent}
                          onChange={(e) => updateForm('userAgent', e.target.value)}
                          placeholder="自定义User Agent"
                          className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                        />
                      </div>

                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">请求间隔 (秒)</Label>
                        <Input
                          type="number"
                          min="1"
                          value={form.rateLimitDuration}
                          onChange={(e) => updateForm('rateLimitDuration', parseInt(e.target.value) || 60)}
                          placeholder="默认60秒"
                          className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                        />
                      </div>
                    </div>

                    
                    {/* 模型映射表 */}
                    <div className="mt-6 space-y-4">
                      <div className="flex items-center justify-between">
                        <div>
                          <Label className="text-sm font-medium text-foreground">模型映射 (可选)</Label>
                          <div className="mt-1 space-y-1">
                            <p className="text-xs text-muted-foreground">
                              将请求中的模型名映射为实际调用的模型名
                            </p>
                            <p className="text-xs text-blue-600 dark:text-blue-400">
                              常用映射：claude-3-5-haiku-20241022 → 实际支持的模型名
                            </p>
                          </div>
                        </div>
                        <div className="flex gap-2">
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={() => setModelMappings([...modelMappings, { from: '', to: '' }])}
                            className="border-border text-muted-foreground hover:bg-muted"
                          >
                            <Plus className="w-4 h-4 mr-1" />
                            添加映射
                          </Button>
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={() => setModelMappings([...modelMappings, { from: 'claude-3-5-haiku-20241022', to: '' }])}
                            className="border-border text-muted-foreground hover:bg-muted text-xs"
                          >
                            快速添加 Haiku
                          </Button>
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={() => setModelMappings([...modelMappings, { from: 'claude-sonnet-4-20250514', to: '' }])}
                            className="border-border text-muted-foreground hover:bg-muted text-xs"
                          >
                            快速添加 Sonnet-4
                          </Button>
                        </div>
                      </div>
                      
                      {modelMappings.length === 0 && (
                        <div className="text-center py-8 text-muted-foreground">
                          <div className="w-12 h-12 mx-auto mb-3 bg-muted rounded-full flex items-center justify-center">
                            <Settings className="w-6 h-6" />
                          </div>
                          <p className="text-sm">暂无模型映射配置</p>
                          <p className="text-xs">点击上方按钮添加模型映射规则</p>
                        </div>
                      )}
                      
                      {modelMappings.map((mapping, index) => (
                        <div key={index} className="flex gap-3 items-center p-3 bg-card rounded-xl border border-border">
                          <div className="flex-1">
                            <div className="relative">
                              <Input
                                list={`from-models-${index}`}
                                placeholder="原模型名 (如: gpt-4)"
                                value={mapping.from}
                                onChange={(e) => {
                                  const newMappings = [...modelMappings];
                                  newMappings[index] = { ...mapping, from: e.target.value };
                                  setModelMappings(newMappings);
                                }}
                                className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                              />
                              <datalist id={`from-models-${index}`}>
                                {defaultModels.map(model => (
                                  <option key={model} value={model} />
                                ))}
                              </datalist>
                            </div>
                          </div>
                          <div className="flex items-center justify-center w-8">
                            <ChevronRight className="w-4 h-4 text-muted-foreground" />
                          </div>
                          <div className="flex-1">
                            <div className="relative">
                              <Input
                                list={`to-models-${index}`}
                                placeholder="目标模型名 (如: claude-3-sonnet)"
                                value={mapping.to}
                                onChange={(e) => {
                                  const newMappings = [...modelMappings];
                                  newMappings[index] = { ...mapping, to: e.target.value };
                                  setModelMappings(newMappings);
                                }}
                                className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                              />
                              <datalist id={`to-models-${index}`}>
                                {defaultModels.map(model => (
                                  <option key={model} value={model} />
                                ))}
                              </datalist>
                            </div>
                          </div>
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                              const newMappings = modelMappings.filter((_, i) => i !== index);
                              setModelMappings(newMappings);
                            }}
                            className="text-destructive hover:text-destructive/80 hover:bg-destructive/10 rounded-full"
                          >
                            <Trash2 className="w-4 h-4" />
                          </Button>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* OpenAI 特定配置 */}
              {form.platform === 'openai' && (
                <Card className="border border-border bg-card/50">
                  <CardContent className="p-6">
                    <h3 className="font-semibold text-foreground mb-4 flex items-center gap-2">
                      <div className="w-2 h-2 bg-primary rounded-full"></div>
                      OpenAI 配置
                    </h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">Base URL</Label>
                        <Input
                          value={form.apiUrl}
                          onChange={(e) => updateForm('apiUrl', e.target.value)}
                          placeholder="例如：https://api.openai.com/v1"
                          className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                        />
                        <p className="text-xs text-muted-foreground">
                          OpenAI官方地址：https://api.openai.com/v1 或使用兼容的第三方地址
                        </p>
                      </div>

                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">API Key</Label>
                        <Input
                          type="password"
                          value={form.apiKey}
                          onChange={(e) => updateForm('apiKey', e.target.value)}
                          placeholder="留空表示不更新..."
                          className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                        />
                        <p className="text-xs text-muted-foreground">留空表示保持当前API Key不变</p>
                      </div>

                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">User Agent (可选)</Label>
                        <Input
                          value={form.userAgent}
                          onChange={(e) => updateForm('userAgent', e.target.value)}
                          placeholder="自定义User Agent"
                          className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                        />
                      </div>

                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">请求间隔 (秒)</Label>
                        <Input
                          type="number"
                          min="1"
                          value={form.rateLimitDuration}
                          onChange={(e) => updateForm('rateLimitDuration', parseInt(e.target.value) || 60)}
                          placeholder="默认60秒"
                          className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                        />
                      </div>
                    </div>

                     {/* 模型映射表 */}
                     <div className="mt-6 space-y-4">
                       <div className="flex items-center justify-between">
                         <div>
                           <Label className="text-sm font-medium text-foreground">模型映射 (可选)</Label>
                           <div className="mt-1 space-y-1">
                             <p className="text-xs text-muted-foreground">
                               将请求中的模型名映射为实际调用的模型名
                             </p>
                             <p className="text-xs text-blue-600 dark:text-blue-400">
                               常用映射：claude-sonnet-4-20250514 → 实际支持的模型名
                             </p>
                           </div>
                         </div>
                         <div className="flex gap-2">
                           <Button
                             type="button"
                             variant="outline"
                             size="sm"
                             onClick={() => setModelMappings([...modelMappings, { from: '', to: '' }])}
                             className="border-border text-muted-foreground hover:bg-muted"
                           >
                             <Plus className="w-4 h-4 mr-1" />
                             添加映射
                           </Button>
                           <Button
                             type="button"
                             variant="outline"
                             size="sm"
                             onClick={() => setModelMappings([...modelMappings, { from: 'claude-3-5-haiku-20241022', to: '' }])}
                             className="border-border text-muted-foreground hover:bg-muted text-xs"
                           >
                             快速添加 Haiku
                           </Button>
                           <Button
                             type="button"
                             variant="outline"
                             size="sm"
                             onClick={() => setModelMappings([...modelMappings, { from: 'claude-sonnet-4-20250514', to: '' }])}
                             className="border-border text-muted-foreground hover:bg-muted text-xs"
                           >
                             快速添加 Sonnet-4
                           </Button>
                         </div>
                       </div>
                       
                       {modelMappings.length === 0 && (
                         <div className="text-center py-8 text-muted-foreground">
                           <div className="w-12 h-12 mx-auto mb-3 bg-muted rounded-full flex items-center justify-center">
                             <Settings className="w-6 h-6" />
                           </div>
                           <p className="text-sm">暂无模型映射配置</p>
                           <p className="text-xs">点击上方按钮添加模型映射规则</p>
                         </div>
                       )}
                       
                       {modelMappings.map((mapping, index) => (
                         <div key={index} className="flex gap-3 items-center p-3 bg-card rounded-xl border border-border">
                           <div className="flex-1">
                             <div className="relative">
                               <Input
                                 list={`from-models-openai-${index}`}
                                 placeholder="原模型名 (如: gpt-4)"
                                 value={mapping.from}
                                 onChange={(e) => {
                                   const newMappings = [...modelMappings];
                                   newMappings[index] = { ...mapping, from: e.target.value };
                                   setModelMappings(newMappings);
                                 }}
                                 className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                               />
                               <datalist id={`from-models-openai-${index}`}>
                                 {defaultModels.map(model => (
                                   <option key={model} value={model} />
                                 ))}
                               </datalist>
                             </div>
                           </div>
                           <div className="flex items-center justify-center w-8">
                             <ChevronRight className="w-4 h-4 text-muted-foreground" />
                           </div>
                           <div className="flex-1">
                             <div className="relative">
                               <Input
                                 list={`to-models-openai-${index}`}
                                 placeholder="目标模型名 (如: gpt-4-turbo)"
                                 value={mapping.to}
                                 onChange={(e) => {
                                   const newMappings = [...modelMappings];
                                   newMappings[index] = { ...mapping, to: e.target.value };
                                   setModelMappings(newMappings);
                                 }}
                                 className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                               />
                               <datalist id={`to-models-openai-${index}`}>
                                 {defaultModels.map(model => (
                                   <option key={model} value={model} />
                                 ))}
                               </datalist>
                             </div>
                           </div>
                           <Button
                             type="button"
                             variant="ghost"
                             size="sm"
                             onClick={() => {
                               const newMappings = modelMappings.filter((_, i) => i !== index);
                               setModelMappings(newMappings);
                             }}
                             className="text-destructive hover:text-destructive/80 hover:bg-destructive/10 rounded-full"
                           >
                             <Trash2 className="w-4 h-4" />
                           </Button>
                         </div>
                       ))}
                     </div>
                   </CardContent>
                 </Card>
               )}

              {/* Thor 特定配置 */}
              {form.platform === 'thor' && (
                <Card className="border border-border bg-card/50">
                  <CardContent className="p-6">
                    <h3 className="font-semibold text-foreground mb-4 flex items-center gap-2">
                      <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
                      Thor 配置
                    </h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">Base URL</Label>
                        <Input
                          value={form.apiUrl}
                          onChange={(e) => updateForm('apiUrl', e.target.value)}
                          placeholder="例如：https://api.token-ai.cn/v1"
                          className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                        />
                        <p className="text-xs text-muted-foreground">
                          Thor平台地址：https://api.token-ai.cn/v1
                        </p>
                      </div>

                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">API Key</Label>
                        <Input
                          type="password"
                          value={form.apiKey}
                          onChange={(e) => updateForm('apiKey', e.target.value)}
                          placeholder="留空表示不更新..."
                          className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                        />
                        <p className="text-xs text-muted-foreground">留空表示保持当前API Key不变</p>
                      </div>

                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">User Agent (可选)</Label>
                        <Input
                          value={form.userAgent}
                          onChange={(e) => updateForm('userAgent', e.target.value)}
                          placeholder="自定义User Agent"
                          className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                        />
                      </div>

                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">请求间隔 (秒)</Label>
                        <Input
                          type="number"
                          min="1"
                          value={form.rateLimitDuration}
                          onChange={(e) => updateForm('rateLimitDuration', parseInt(e.target.value) || 60)}
                          placeholder="默认60秒"
                          className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                        />
                      </div>
                    </div>

                     {/* 模型映射表 */}
                     <div className="mt-6 space-y-4">
                       <div className="flex items-center justify-between">
                         <div>
                           <Label className="text-sm font-medium text-foreground">模型映射 (可选)</Label>
                           <div className="mt-1 space-y-1">
                             <p className="text-xs text-muted-foreground">
                               将请求中的模型名映射为实际调用的模型名
                             </p>
                             <p className="text-xs text-blue-600 dark:text-blue-400">
                               常用映射：claude-sonnet-4-20250514 → 实际支持的模型名
                             </p>
                           </div>
                         </div>
                         <div className="flex gap-2">
                           <Button
                             type="button"
                             variant="outline"
                             size="sm"
                             onClick={() => setModelMappings([...modelMappings, { from: '', to: '' }])}
                             className="border-border text-muted-foreground hover:bg-muted"
                           >
                             <Plus className="w-4 h-4 mr-1" />
                             添加映射
                           </Button>
                           <Button
                             type="button"
                             variant="outline"
                             size="sm"
                             onClick={() => setModelMappings([...modelMappings, { from: 'claude-3-5-haiku-20241022', to: '' }])}
                             className="border-border text-muted-foreground hover:bg-muted text-xs"
                           >
                             快速添加 Haiku
                           </Button>
                           <Button
                             type="button"
                             variant="outline"
                             size="sm"
                             onClick={() => setModelMappings([...modelMappings, { from: 'claude-sonnet-4-20250514', to: '' }])}
                             className="border-border text-muted-foreground hover:bg-muted text-xs"
                           >
                             快速添加 Sonnet-4
                           </Button>
                         </div>
                       </div>
                       
                       {modelMappings.length === 0 && (
                         <div className="text-center py-8 text-muted-foreground">
                           <div className="w-12 h-12 mx-auto mb-3 bg-muted rounded-full flex items-center justify-center">
                             <Settings className="w-6 h-6" />
                           </div>
                           <p className="text-sm">暂无模型映射配置</p>
                           <p className="text-xs">点击上方按钮添加模型映射规则</p>
                         </div>
                       )}
                       
                       {modelMappings.map((mapping, index) => (
                         <div key={index} className="flex gap-3 items-center p-3 bg-card rounded-xl border border-border">
                           <div className="flex-1">
                             <div className="relative">
                               <Input
                                 list={`from-models-thor-${index}`}
                                 placeholder="原模型名 (如: gpt-4)"
                                 value={mapping.from}
                                 onChange={(e) => {
                                   const newMappings = [...modelMappings];
                                   newMappings[index] = { ...mapping, from: e.target.value };
                                   setModelMappings(newMappings);
                                 }}
                                 className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                               />
                               <datalist id={`from-models-thor-${index}`}>
                                 {defaultModels.map(model => (
                                   <option key={model} value={model} />
                                 ))}
                               </datalist>
                             </div>
                           </div>
                           <div className="flex items-center justify-center w-8">
                             <ChevronRight className="w-4 h-4 text-muted-foreground" />
                           </div>
                           <div className="flex-1">
                             <div className="relative">
                               <Input
                                 list={`to-models-thor-${index}`}
                                 placeholder="目标模型名 (如: gpt-4-turbo)"
                                 value={mapping.to}
                                 onChange={(e) => {
                                   const newMappings = [...modelMappings];
                                   newMappings[index] = { ...mapping, to: e.target.value };
                                   setModelMappings(newMappings);
                                 }}
                                 className="border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                               />
                               <datalist id={`to-models-thor-${index}`}>
                                 {defaultModels.map(model => (
                                   <option key={model} value={model} />
                                 ))}
                               </datalist>
                             </div>
                           </div>
                           <Button
                             type="button"
                             variant="ghost"
                             size="sm"
                             onClick={() => {
                               const newMappings = modelMappings.filter((_, i) => i !== index);
                               setModelMappings(newMappings);
                             }}
                             className="text-destructive hover:text-destructive/80 hover:bg-destructive/10 rounded-full"
                           >
                             <Trash2 className="w-4 h-4" />
                           </Button>
                         </div>
                       ))}
                     </div>
                   </CardContent>
                 </Card>
               )}

              {/* 平台特定配置卡片 */}
              {(form.platform === 'claude' || form.platform === 'claude-console' || form.platform === 'openai' || form.platform === 'thor') && (
                <Card className="border border-border bg-card/50">
                  <CardContent className="p-6">
                    <h3 className="font-semibold text-foreground mb-4 flex items-center gap-2">
                      <div className="w-2 h-2 bg-primary rounded-full"></div>
                      调度配置
                    </h3>
                    <div className="space-y-2">
                      <Label className="text-sm font-medium text-foreground">调度优先级 (1-100)</Label>
                      <Input
                        type="number"
                        min="1"
                        max="100"
                        value={form.priority}
                        onChange={(e) => updateForm('priority', parseInt(e.target.value) || 50)}
                        className="w-32 border-input bg-background text-foreground focus:border-ring focus:ring-ring"
                      />
                      <p className="text-xs text-muted-foreground">
                        数字越小优先级越高。建议范围：1-100，默认值：50
                      </p>
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Token 更新 */}
              {form.platform !== 'claude-console' && form.platform !== 'openai' && form.platform !== 'thor' && (
                <Card className="border border-border bg-card/50">
                  <CardContent className="p-6">
                    <h3 className="font-semibold text-foreground mb-4 flex items-center gap-2">
                      <div className="w-2 h-2 bg-primary rounded-full"></div>
                      Token 更新
                    </h3>
                    <div className="p-4 bg-muted border border-border rounded-xl mb-4">
                      <p className="text-sm text-muted-foreground">
                        <strong>注意：</strong>可以更新 Access Token 和 Refresh Token。留空表示保持当前Token不变。
                      </p>
                    </div>
                    
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">新的 Access Token</Label>
                        <textarea
                          value={form.accessToken}
                          onChange={(e) => updateForm('accessToken', e.target.value)}
                          rows={4}
                          className="w-full p-3 border border-input rounded-xl resize-none font-mono text-xs focus:border-ring focus:ring-ring focus:ring-1"
                          placeholder="留空表示不更新..."
                        />
                      </div>

                      <div className="space-y-2">
                        <Label className="text-sm font-medium text-foreground">新的 Refresh Token</Label>
                        <textarea
                          value={form.refreshToken}
                          onChange={(e) => updateForm('refreshToken', e.target.value)}
                          rows={4}
                          className="w-full p-3 border border-input rounded-xl resize-none font-mono text-xs focus:border-ring focus:ring-ring focus:ring-1"
                          placeholder="留空表示不更新..."
                        />
                      </div>
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* 代理设置卡片 */}
              <Card className="border border-border bg-card/50">
                <CardContent className="p-6">
                  <h3 className="font-semibold text-foreground mb-4 flex items-center gap-2">
                    <div className="w-2 h-2 bg-primary rounded-full"></div>
                    代理设置
                  </h3>
                  <ProxyConfigComponent
                    value={form.proxy}
                    onChange={(proxy: ProxyConfig) => updateForm('proxy', proxy)}
                  />
                </CardContent>
              </Card>
            </div>
          )}

        {/* 按钮区域 */}
        {renderActionButtons()}
      </div>

      {/* 确认弹窗 */}
      <ConfirmModal
        show={showConfirmModal}
        title={confirmOptions.title}
        message={confirmOptions.message}
        confirmText={confirmOptions.confirmText}
        cancelText={confirmOptions.cancelText}
        onConfirm={handleConfirm}
        onCancel={handleCancel}
      />
    </Modal>
  );
}
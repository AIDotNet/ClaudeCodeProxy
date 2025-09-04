import { useState, useEffect } from 'react';
import { Modal } from '@/components/ui/modal';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { X, Plus } from 'lucide-react';
import { apiService } from '@/services/api';
import type { ApiKey, Account } from '@/services/api';
import { useAuth } from '@/contexts/AuthContext';

interface ApiKeyModalProps {
  open: boolean;
  onClose: () => void;
  editingKey?: ApiKey | null;
  onSuccess: (apiKey: ApiKey) => void;
}

interface FormData {
  name: string;
  description: string;
  tags: string[];
  tokenLimit: string;
  rateLimitWindow: string;
  rateLimitRequests: string;
  concurrencyLimit: string;
  dailyCostLimit: string;
  monthlyCostLimit: string;
  totalCostLimit: string;
  expiresAt: string;
  permissions: string;
  claudeAccountId: string;
  claudeConsoleAccountId: string;
  geminiAccountId: string;
  enableModelRestriction: boolean;
  restrictedModels: string[];
  enableClientRestriction: boolean;
  allowedClients: string[];
  isEnabled: boolean;
  model: string;
  service: string;
  defaultAccountId: string;
}


export default function ApiKeyModal({ open, onClose, editingKey, onSuccess }: ApiKeyModalProps) {
  const { hasRole } = useAuth();
  const [loading, setLoading] = useState(false);
  const [newTag, setNewTag] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [accounts, setAccounts] = useState<Account[]>([]);

  const getInitialFormData = (editingKey?: ApiKey | null): FormData => {
    if (editingKey) {
      return {
        name: editingKey.name || '',
        description: editingKey.description || '',
        tags: editingKey.tags || [],
        tokenLimit: editingKey.tokenLimit?.toString() || '',
        rateLimitWindow: editingKey.rateLimitWindow?.toString() || '',
        rateLimitRequests: editingKey.rateLimitRequests?.toString() || '',
        concurrencyLimit: editingKey.concurrencyLimit?.toString() || '0',
        dailyCostLimit: editingKey.dailyCostLimit?.toString() || '0',
        monthlyCostLimit: editingKey.monthlyCostLimit?.toString() || '0',
        totalCostLimit: editingKey.totalCostLimit?.toString() || '0',
        expiresAt: editingKey.expiresAt ? new Date(editingKey.expiresAt).toISOString().slice(0, 16) : '',
        permissions: editingKey.permissions || 'all',
        claudeAccountId: editingKey.claudeAccountId || '',
        claudeConsoleAccountId: editingKey.claudeConsoleAccountId || '',
        geminiAccountId: editingKey.geminiAccountId || '',
        enableModelRestriction: editingKey.enableModelRestriction || false,
        restrictedModels: editingKey.restrictedModels || [],
        enableClientRestriction: editingKey.enableClientRestriction || false,
        allowedClients: editingKey.allowedClients || [],
        isEnabled: editingKey.isEnabled !== undefined ? editingKey.isEnabled : true,
        model: editingKey.model || '',
        service: editingKey.service || 'all',
        defaultAccountId: editingKey.defaultAccountId ? editingKey.defaultAccountId : 'none'
      };
    }
    return {
      name: '',
      description: '',
      tags: [],
      tokenLimit: '',
      rateLimitWindow: '',
      rateLimitRequests: '',
      concurrencyLimit: '0',
      dailyCostLimit: '0',
      monthlyCostLimit: '0',
      totalCostLimit: '0',
      expiresAt: '',
      permissions: 'all',
      claudeAccountId: '',
      claudeConsoleAccountId: '',
      geminiAccountId: '',
      enableModelRestriction: false,
      restrictedModels: [],
      enableClientRestriction: false,
      allowedClients: [],
      isEnabled: true,
      model: '',
      service: 'all',
      defaultAccountId: 'none'
    };
  };

  const [formData, setFormData] = useState<FormData>(() => getInitialFormData(editingKey));

  // 当 editingKey 改变时重新设置表单数据
  useEffect(() => {
    setFormData(getInitialFormData(editingKey));
    setNewTag('');
    setErrors({});
  }, [editingKey, open]);

  // 获取账户列表（仅管理员）
  useEffect(() => {
    const fetchAccounts = async () => {
      // 调试日志
      console.log('User role check:', {
        hasAdminRole: hasRole('admin'),
        hasAdministratorRole: hasRole('administrator'),
        hasAdminCapitalRole: hasRole('Admin')
      });
      
      if (hasRole('admin') || hasRole('administrator') || hasRole('Admin')) {
        try {
          console.log('Fetching accounts for admin user...');
          const accountList = await apiService.getAccounts();
          console.log('Accounts fetched:', accountList);
          setAccounts(accountList);
        } catch (error) {
          console.error('Failed to fetch accounts:', error);
        }
      } else {
        console.log('User does not have admin role, skipping account fetch');
      }
    };
    
    if (open) {
      fetchAccounts();
    }
  }, [open, hasRole]);

  const updateFormData = (field: keyof FormData, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const addTag = () => {
    if (newTag.trim() && !formData.tags.includes(newTag.trim())) {
      updateFormData('tags', [...formData.tags, newTag.trim()]);
      setNewTag('');
    }
  };

  const removeTag = (index: number) => {
    updateFormData('tags', formData.tags.filter((_, i) => i !== index));
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = '名称不能为空';
    }

    if (formData.tokenLimit && isNaN(Number(formData.tokenLimit))) {
      newErrors.tokenLimit = 'Token限制必须是数字';
    }

    if (formData.rateLimitWindow && isNaN(Number(formData.rateLimitWindow))) {
      newErrors.rateLimitWindow = '时间窗口必须是数字';
    }

    if (formData.rateLimitRequests && isNaN(Number(formData.rateLimitRequests))) {
      newErrors.rateLimitRequests = '请求次数限制必须是数字';
    }

    if (isNaN(Number(formData.concurrencyLimit))) {
      newErrors.concurrencyLimit = '并发限制必须是数字';
    }

    if (isNaN(Number(formData.dailyCostLimit))) {
      newErrors.dailyCostLimit = '每日费用限制必须是数字';
    }

    if (isNaN(Number(formData.monthlyCostLimit))) {
      newErrors.monthlyCostLimit = '月度费用限制必须是数字';
    }

    if (isNaN(Number(formData.totalCostLimit))) {
      newErrors.totalCostLimit = '总费用限制必须是数字';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    setLoading(true);
    try {
      const requestData = {
        name: formData.name,
        description: formData.description || null,
        tags: formData.tags.length > 0 ? formData.tags : null,
        tokenLimit: formData.tokenLimit ? Number(formData.tokenLimit) : null,
        rateLimitWindow: formData.rateLimitWindow ? Number(formData.rateLimitWindow) : null,
        rateLimitRequests: formData.rateLimitRequests ? Number(formData.rateLimitRequests) : null,
        concurrencyLimit: Number(formData.concurrencyLimit),
        dailyCostLimit: Number(formData.dailyCostLimit),
        monthlyCostLimit: Number(formData.monthlyCostLimit),
        totalCostLimit: Number(formData.totalCostLimit),
        expiresAt: formData.expiresAt || null,
        permissions: formData.permissions,
        claudeAccountId: formData.claudeAccountId || null,
        claudeConsoleAccountId: formData.claudeConsoleAccountId || null,
        geminiAccountId: formData.geminiAccountId || null,
        enableModelRestriction: formData.enableModelRestriction,
        restrictedModels: formData.restrictedModels.length > 0 ? formData.restrictedModels : null,
        enableClientRestriction: formData.enableClientRestriction,
        allowedClients: formData.allowedClients.length > 0 ? formData.allowedClients : null,
        isEnabled: formData.isEnabled,
        model: formData.model || null,
        service: formData.service,
        defaultAccountId: formData.defaultAccountId && formData.defaultAccountId !== 'none' ? formData.defaultAccountId : null
      };

      const result = editingKey 
        ? await apiService.updateApiKey(editingKey.id, requestData)
        : await apiService.createApiKey(requestData);
      
      onSuccess(result);
      onClose();
    } catch (error) {
      console.error('Failed to save API key:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal 
      isOpen={open} 
      onClose={onClose}
      title={editingKey ? '编辑 API Key' : '创建新的 API Key'}
      subtitle={editingKey ? '修改您的 API Key 设置和限制' : '配置您的 API Key 设置和限制'}
      size="5xl"
    >
      <form onSubmit={handleSubmit} className="space-y-6">
          {/* 基本信息 */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">基本信息</h3>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="name">名称 *</Label>
                <Input
                  id="name"
                  value={formData.name}
                  onChange={(e) => updateFormData('name', e.target.value)}
                  placeholder="为您的 API Key 取一个名称"
                  className={errors.name ? 'border-destructive' : ''}
                />
                {errors.name && <p className="text-destructive text-xs">{errors.name}</p>}
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">备注描述</Label>
                <Input
                  id="description"
                  value={formData.description}
                  onChange={(e) => updateFormData('description', e.target.value)}
                  placeholder="为这个 API Key 添加一些描述信息"
                />
              </div>
            </div>

            {/* 标签 */}
            <div className="space-y-2">
              <Label>标签</Label>
              {formData.tags.length > 0 && (
                <div className="flex flex-wrap gap-2 mb-2">
                  {formData.tags.map((tag, index) => (
                    <Badge key={index} variant="secondary" className="flex items-center gap-1">
                      {tag}
                      <X 
                        className="h-3 w-3 cursor-pointer" 
                        onClick={() => removeTag(index)}
                      />
                    </Badge>
                  ))}
                </div>
              )}
              <div className="flex gap-2">
                <Input
                  value={newTag}
                  onChange={(e) => setNewTag(e.target.value)}
                  placeholder="添加标签"
                  onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addTag())}
                />
                <Button type="button" variant="outline" onClick={addTag}>
                  <Plus className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>

          {/* 服务配置 */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">服务配置</h3>
            
            <div className="grid grid-cols-1 md:grid-cols-1 gap-4">
              {/* 账户绑定（仅管理员） */}
              {(hasRole('admin') || hasRole('administrator') || hasRole('Admin')) && accounts.length > 0 && (
                <div className="space-y-2">
                  <Label htmlFor="boundAccountId">绑定账户 (管理员功能)</Label>
                  <div className="text-xs text-gray-500 mb-2">
                    当前值: "{formData.defaultAccountId}" | 编辑Key: {editingKey?.id || 'new'} | 账户数: {accounts.length}
                  </div>
                  <Select value={formData.defaultAccountId} onValueChange={(value) => updateFormData('defaultAccountId', value)}>
                    <SelectTrigger>
                      <SelectValue placeholder="选择要绑定的账户（可选）" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="none">不绑定账户</SelectItem>
                      {accounts.map((account) => (
                        <SelectItem key={account.id} value={account.id}>
                          {account.name} ({account.platform}) - {account.status}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <p className="text-xs text-muted-foreground">
                    绑定账户后，此 API Key 将优先使用指定的账户进行请求
                  </p>
                </div>
              )}

              <div className="space-y-2">
                <Label htmlFor="permissions">服务权限</Label>
                <Select value={formData.permissions} onValueChange={(value) => updateFormData('permissions', value)}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">全部服务</SelectItem>
                    <SelectItem value="claude">仅 Claude</SelectItem>
                    <SelectItem value="gemini">仅 Gemini</SelectItem>
                    <SelectItem value="openai">仅 OpenAI</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {formData.service !== 'all' && (
                <div className="space-y-2 md:col-span-2">
                  <Label htmlFor="model">指定模型 (可选)</Label>
                  <Input
                    id="model"
                    value={formData.model}
                    onChange={(e) => updateFormData('model', e.target.value)}
                    placeholder="如果指定，将强制使用此模型"
                  />
                </div>
              )}
            </div>
          </div>

          {/* 费用限制 */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">费用限制</h3>
            
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label htmlFor="dailyCostLimit">每日费用限制 (美元)</Label>
                <Input
                  id="dailyCostLimit"
                  type="number"
                  min="0"
                  step="0.01"
                  value={formData.dailyCostLimit}
                  onChange={(e) => updateFormData('dailyCostLimit', e.target.value)}
                  placeholder="0表示无限制"
                  className={errors.dailyCostLimit ? 'border-destructive' : ''}
                />
                {errors.dailyCostLimit && <p className="text-destructive text-xs">{errors.dailyCostLimit}</p>}
                <p className="text-xs text-muted-foreground">设置0表示不限制每日费用</p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="monthlyCostLimit">月度费用限制 (美元)</Label>
                <Input
                  id="monthlyCostLimit"
                  type="number"
                  min="0"
                  step="0.01"
                  value={formData.monthlyCostLimit}
                  onChange={(e) => updateFormData('monthlyCostLimit', e.target.value)}
                  placeholder="0表示无限制"
                  className={errors.monthlyCostLimit ? 'border-destructive' : ''}
                />
                {errors.monthlyCostLimit && <p className="text-destructive text-xs">{errors.monthlyCostLimit}</p>}
                <p className="text-xs text-muted-foreground">设置0表示不限制月度费用</p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="totalCostLimit">总费用限制 (美元)</Label>
                <Input
                  id="totalCostLimit"
                  type="number"
                  min="0"
                  step="0.01"
                  value={formData.totalCostLimit}
                  onChange={(e) => updateFormData('totalCostLimit', e.target.value)}
                  placeholder="0表示无限制"
                  className={errors.totalCostLimit ? 'border-destructive' : ''}
                />
                {errors.totalCostLimit && <p className="text-destructive text-xs">{errors.totalCostLimit}</p>}
                <p className="text-xs text-muted-foreground">设置0表示不限制总费用</p>
              </div>
            </div>

            <div className="bg-muted p-4 rounded-lg">
              <h4 className="font-medium text-foreground mb-2">💡 费用限制说明</h4>
              <ul className="text-sm text-muted-foreground space-y-1">
                <li>• 每日费用限制：每天重置，达到限制后当天无法继续使用</li>
                <li>• 月度费用限制：每月重置，达到限制后当月无法继续使用</li>
                <li>• 总费用限制：永不重置，达到限制后永久无法使用（除非修改限制）</li>
                <li>• 费用实时计算，包含输入Token、输出Token和缓存费用</li>
              </ul>
            </div>
          </div>

          {/* 操作按钮 */}
          <div className="flex justify-end space-x-2 pt-4">
            <Button type="button" variant="outline" onClick={onClose}>
              取消
            </Button>
            <Button type="submit" disabled={loading}>
              {loading 
                ? (editingKey ? '更新中...' : '创建中...') 
                : (editingKey ? '更新 API Key' : '创建 API Key')
              }
            </Button>
          </div>
        </form>
    </Modal>
  );
}
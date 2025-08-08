import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { 
  Users, 
  Plus, 
  Edit2, 
  Trash2, 
  Search, 
  Filter, 
  Shield, 
  Mail, 
  Clock, 
  MoreHorizontal,
  UserX,
  UserCheck,
  Key,
  ChevronLeft,
  ChevronRight
} from 'lucide-react';
import { apiService } from '@/services/api';
import type { User, UserRole, UsersResponse } from '@/services/api';
import UserModal from '@/components/users/UserModal';
import RoleModal from '@/components/users/RoleModal';
import { showToast } from '@/utils/toast';
import ConfirmModal from '@/components/common/ConfirmModal';
import { useConfirm } from '@/hooks/useConfirm';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<UserRole[]>([]);
  const [loading, setLoading] = useState(true);
  const [total, setTotal] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(0);
  
  // Modals
  const [showUserForm, setShowUserForm] = useState(false);
  const [showRoleForm, setShowRoleForm] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [editingRole, setEditingRole] = useState<UserRole | null>(null);
  
  // Filters
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'enabled' | 'disabled'>('all');
  const [roleFilter, setRoleFilter] = useState<string>('all');
  
  // Loading states
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [togglingId, setTogglingId] = useState<string | null>(null);
  
  const { showConfirmModal, confirmOptions, showConfirm, handleConfirm, handleCancel } = useConfirm();

  useEffect(() => {
    fetchRoles();
  }, []);

  useEffect(() => {
    fetchUsers();
  }, [currentPage, searchTerm, statusFilter, roleFilter]);

  const fetchUsers = async () => {
    try {
      setLoading(true);
      
      const response = await apiService.getUsers(currentPage, pageSize);
      setUsers(response.data);
      setTotal(response.total);
      setTotalPages(response.totalPages);
    } catch (error) {
      console.error('Failed to fetch users:', error);
      showToast('获取用户列表失败', 'error');
    } finally {
      setLoading(false);
    }
  };

  const fetchRoles = async () => {
    try {
      const rolesData = await apiService.getRoles();
      setRoles(rolesData);
    } catch (error) {
      console.error('Failed to fetch roles:', error);
    }
  };

  const handleUserSubmit = async (user: User) => {
    if (editingUser) {
      setUsers(prev => prev.map(u => u.id === editingUser.id ? user : u));
    } else {
      await fetchUsers(); // Refresh the list to get the new user
    }
    setShowUserForm(false);
    setEditingUser(null);
  };

  const handleRoleSubmit = async (role: UserRole) => {
    if (editingRole) {
      setRoles(prev => prev.map(r => r.id === editingRole.id ? role : r));
    } else {
      setRoles(prev => [...prev, role]);
    }
    setShowRoleForm(false);
    setEditingRole(null);
    // Refresh users to update role information
    await fetchUsers();
  };

  const handleDeleteUser = async (id: string) => {
    const user = users.find(u => u.id === id);
    const userName = user?.username || 'User';
    
    const confirmed = await showConfirm(
      '删除用户',
      `确定要删除用户 "${userName}" 吗？\n\n此操作不可撤销，请谨慎操作。`,
      '删除',
      '取消'
    );
    
    if (confirmed) {
      setDeletingId(id);
      try {
        await apiService.deleteUser(id);
        setUsers(prev => prev.filter(u => u.id !== id));
        showToast('用户删除成功', 'success');
        // If we deleted the last user on current page, go to previous page
        if (users.length === 1 && currentPage > 1) {
          setCurrentPage(prev => prev - 1);
        }
      } catch (error: any) {
        console.error('Failed to delete user:', error);
        showToast(error.message || '删除用户失败', 'error');
        await fetchUsers();
      } finally {
        setDeletingId(null);
      }
    }
  };

  const handleEditUser = (user: User) => {
    setEditingUser(user);
    setShowUserForm(true);
  };

  const handleToggleUserStatus = async (user: User) => {
    setTogglingId(user.id);
    try {
      await apiService.toggleUserEnabled(user.id);
      setUsers(prev => prev.map(u => 
        u.id === user.id ? { ...u, isEnabled: !u.isActive } : u
      ));
      showToast(`用户已${!user.isActive ? '启用' : '禁用'}`, 'success');
    } catch (error: any) {
      showToast(error.message || '更新用户状态失败', 'error');
    } finally {
      setTogglingId(null);
    }
  };

  const handleResetPassword = async (user: User) => {
    const confirmed = await showConfirm(
      '重置密码',
      `确定要重置用户 "${user.username}" 的密码吗？\n\n新密码将通过邮件发送给用户。`,
      '重置',
      '取消'
    );
    
    if (confirmed) {
      try {
        // Generate a temporary password
        const tempPassword = Math.random().toString(36).slice(-8);
        await apiService.resetUserPassword(user.id, tempPassword);
        showToast('密码重置成功，新密码已发送至用户邮箱', 'success');
      } catch (error: any) {
        console.error('Failed to reset password:', error);
        showToast(error.message || '重置密码失败', 'error');
      }
    }
  };

  const getUserStatusBadge = (user: User) => {
    if (!user.isActive) {
      return <Badge variant="secondary">已禁用</Badge>;
    }
    if (!user.isEmailVerified) {
      return <Badge variant="outline">未验证</Badge>;
    }
    return <Badge variant="default">正常</Badge>;
  };

  const getUserInitials = (user: User) => {
    const name =  user.username;
    return name.slice(0, 2).toUpperCase();
  };

  const formatLastLogin = (lastLoginAt?: string) => {
    if (!lastLoginAt) return '从未登录';
    const date = new Date(lastLoginAt);
    const now = new Date();
    const diffInHours = (now.getTime() - date.getTime()) / (1000 * 60 * 60);
    
    if (diffInHours < 1) return '刚刚登录';
    if (diffInHours < 24) return `${Math.floor(diffInHours)} 小时前`;
    if (diffInHours < 24 * 7) return `${Math.floor(diffInHours / 24)} 天前`;
    return date.toLocaleDateString();
  };

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
  };

  const clearFilters = () => {
    setSearchTerm('');
    setStatusFilter('all');
    setRoleFilter('all');
    setCurrentPage(1);
  };

  if (loading && users.length === 0) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-border"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-2">
          <Users className="h-6 w-6" />
          <h1 className="text-2xl font-bold">用户管理</h1>
          <Badge variant="secondary">
            {total} 个用户
          </Badge>
        </div>
        <div className="flex items-center space-x-2">
          <Button variant="outline" onClick={() => setShowRoleForm(true)}>
            <Shield className="h-4 w-4 mr-2" />
            角色管理
          </Button>
          <Button onClick={() => setShowUserForm(true)}>
            <Plus className="h-4 w-4 mr-2" />
            添加用户
          </Button>
        </div>
      </div>

      {/* Search and Filter Bar */}
      <div className="flex flex-col sm:flex-row gap-4 p-4 bg-muted rounded-lg">
        <div className="flex-1">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
            <Input
              placeholder="搜索用户名、邮箱或显示名称..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10"
            />
          </div>
        </div>
        <div className="flex gap-2">
          <Select value={statusFilter} onValueChange={(value: 'all' | 'enabled' | 'disabled') => setStatusFilter(value)}>
            <SelectTrigger className="w-32">
              <Filter className="h-4 w-4 mr-2" />
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">全部状态</SelectItem>
              <SelectItem value="enabled">已启用</SelectItem>
              <SelectItem value="disabled">已禁用</SelectItem>
            </SelectContent>
          </Select>
          <Select value={roleFilter} onValueChange={setRoleFilter}>
            <SelectTrigger className="w-32">
              <Shield className="h-4 w-4 mr-2" />
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">全部角色</SelectItem>
              {roles.map((role) => (
                <SelectItem key={role.id} value={role.id}>
                  {role.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Users List */}
      <div className="space-y-4">
        {users.map((user) => (
          <Card key={user.id}>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-4">
                  <Avatar className="h-10 w-10">
                    <AvatarImage src={user.avatar} alt={user.displayName || user.username} />
                    <AvatarFallback className="bg-primary text-primary-foreground">
                      {getUserInitials(user)}
                    </AvatarFallback>
                  </Avatar>
                  
                  <div className="space-y-1">
                    <div className="flex items-center space-x-2">
                      <h3 className="font-medium">{user.displayName || user.username}</h3>
                      {getUserStatusBadge(user)}
                    </div>
                    <div className="flex items-center space-x-4 text-sm text-muted-foreground">
                      <div className="flex items-center space-x-1">
                        <Mail className="h-3 w-3" />
                        <span>{user.email}</span>
                      </div>
                      <div className="flex items-center space-x-1">
                        <Shield className="h-3 w-3" />
                        <span>{user.role?.name}</span>
                      </div>
                      <div className="flex items-center space-x-1">
                        <Clock className="h-3 w-3" />
                        <span>{formatLastLogin(user.lastLoginAt)}</span>
                      </div>
                    </div>
                    {user.description && (
                      <p className="text-sm text-muted-foreground">{user.description}</p>
                    )}
                  </div>
                </div>

                <div className="flex items-center space-x-2">
                  <Button
                    variant="ghost"
                    size="sm"
                    disabled={togglingId === user.id}
                    onClick={() => handleToggleUserStatus(user)}
                  >
                    {togglingId === user.id ? (
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-border" />
                    ) : user.isActive ? (
                      <UserX className="h-4 w-4" />
                    ) : (
                      <UserCheck className="h-4 w-4" />
                    )}
                  </Button>
                  
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="sm">
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem onClick={() => handleEditUser(user)}>
                        <Edit2 className="h-4 w-4 mr-2" />
                        编辑用户
                      </DropdownMenuItem>
                      <DropdownMenuItem onClick={() => handleResetPassword(user)}>
                        <Key className="h-4 w-4 mr-2" />
                        重置密码
                      </DropdownMenuItem>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem 
                        onClick={() => handleDeleteUser(user.id)}
                        disabled={deletingId === user.id || user.role.name === 'admin'}
                        className="text-destructive"
                      >
                        {deletingId === user.id ? (
                          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-border mr-2" />
                        ) : (
                          <Trash2 className="h-4 w-4 mr-2" />
                        )}
                        删除用户
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Empty State */}
      {users.length === 0 && !loading && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Search className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium mb-2">
              {searchTerm || statusFilter !== 'all' || roleFilter !== 'all' 
                ? '未找到匹配的用户' 
                : '暂无用户'
              }
            </h3>
            <p className="text-muted-foreground text-center mb-4">
              {searchTerm || statusFilter !== 'all' || roleFilter !== 'all' 
                ? '请尝试调整搜索条件或过滤器。'
                : '还没有创建任何用户。点击上方按钮添加您的第一个用户。'
              }
            </p>
            {searchTerm || statusFilter !== 'all' || roleFilter !== 'all' ? (
              <Button variant="outline" onClick={clearFilters}>
                清除过滤器
              </Button>
            ) : (
              <Button onClick={() => setShowUserForm(true)}>
                <Plus className="h-4 w-4 mr-2" />
                添加用户
              </Button>
            )}
          </CardContent>
        </Card>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-muted-foreground">
            显示 {(currentPage - 1) * pageSize + 1} - {Math.min(currentPage * pageSize, total)} 
            &nbsp;共 {total} 个用户
          </div>
          <div className="flex items-center space-x-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => handlePageChange(currentPage - 1)}
              disabled={currentPage === 1}
            >
              <ChevronLeft className="h-4 w-4" />
              上一页
            </Button>
            <div className="flex items-center space-x-1">
              {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                let pageNum;
                if (totalPages <= 5) {
                  pageNum = i + 1;
                } else if (currentPage <= 3) {
                  pageNum = i + 1;
                } else if (currentPage >= totalPages - 2) {
                  pageNum = totalPages - 4 + i;
                } else {
                  pageNum = currentPage - 2 + i;
                }
                
                return (
                  <Button
                    key={pageNum}
                    variant={currentPage === pageNum ? 'default' : 'outline'}
                    size="sm"
                    onClick={() => handlePageChange(pageNum)}
                    className="w-10"
                  >
                    {pageNum}
                  </Button>
                );
              })}
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={() => handlePageChange(currentPage + 1)}
              disabled={currentPage === totalPages}
            >
              下一页
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}

      {/* Modals */}
      <UserModal
        open={showUserForm}
        editingUser={editingUser}
        roles={roles}
        onSuccess={handleUserSubmit}
        onClose={() => {
          setShowUserForm(false);
          setEditingUser(null);
        }}
      />

      <RoleModal
        open={showRoleForm}
        editingRole={editingRole}
        onSuccess={handleRoleSubmit}
        onClose={() => {
          setShowRoleForm(false);
          setEditingRole(null);
        }}
      />

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
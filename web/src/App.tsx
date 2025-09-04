import {  RouterProvider, createBrowserRouter } from 'react-router-dom';
import { AuthProvider } from '@/contexts/AuthContext';
import { ToastProvider } from '@/contexts/ToastContext';
import { ThemeProvider } from '@/contexts/ThemeContext';
import { ProtectedRoute } from '@/components/auth/ProtectedRoute';
import AdminLayout from '@/layouts/AdminLayout';
import LoginPage from '@/pages/login';
import RegisterPage from '@/pages/register';
import OAuthCallbackPage from '@/pages/auth/callback';
import DashboardPage from '@/pages/dashboard';
import ApiKeysPage from '@/pages/apikeys';
import AccountsPage from '@/pages/accounts';
import UsersPage from '@/pages/users';
import PricingPage from '@/pages/pricing';
import SettingsPage from '@/pages/settings';
import AdvancedStatsPage from '@/pages/advanced-stats';
import RequestLogsPage from '@/pages/request-logs';
import ProfilePage from '@/pages/profile';
import PersonalDashboardPage from '@/pages/personal-dashboard';
import RedeemCodesPage from '@/pages/redeem-codes';
import InviteFriendsPage from '@/pages/invite-friends';
import AnnouncementsPage from '@/pages/announcements';
import QuotaQueryPage from '@/pages/quota-query';
import './App.css';

const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/register',
    element: <RegisterPage />,
  },
  {
    path: '/auth/callback/:provider',
    element: <OAuthCallbackPage />,
  },
  {
    path: '/quota-query',
    element: <QuotaQueryPage />,
  },
  {
    path: '/',
    element: (
      <ProtectedRoute>
        <AdminLayout />
      </ProtectedRoute>
    ),
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'apikeys', element: <ApiKeysPage /> },
      { path: 'accounts', element: <AccountsPage /> },
      { path: 'users', element: <UsersPage /> },
      { path: 'pricing', element: <PricingPage /> },
      { path: 'request-logs', element: <RequestLogsPage /> },
      { path: 'settings', element: <SettingsPage /> },
      { path: 'advanced-stats', element: <AdvancedStatsPage /> },
      { path: 'profile', element: <ProfilePage /> },
      { path: 'personal-dashboard', element: <PersonalDashboardPage /> },
      { path: 'redeem-codes', element: <RedeemCodesPage /> },
      { path: 'invite-friends', element: <InviteFriendsPage /> },
      { path: 'announcements', element: <AnnouncementsPage /> },
    ],
  },
]);

function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <ToastProvider>
          <RouterProvider
            router={router} />
        </ToastProvider>
      </AuthProvider>
    </ThemeProvider>
  );
}

export default App;
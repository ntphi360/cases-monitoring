import { Routes, Route } from "react-router-dom";

import MainLayout from "../layouts/MainLayout";
import DashboardPage from "../pages/DashboardPage";
import CasesPage from "../pages/CasesPage";
import CaseDetailPage from "../pages/CaseDetailPage";
import AlertsPage from "../pages/AlertsPage";
import NotificationsPage from "../pages/NotificationsPage";
import ReportsPage from "../pages/ReportsPage";
import ImportPage from "../pages/ImportPage";
import LoginPage from "../pages/LoginPage";
import UsersPage from "../pages/UsersPage";
import { AdminRoute, ProtectedRoute } from './ProtectedRoute'

function AppRoutes() {
  return (
    <Routes>
      <Route path="login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
      <Route path="/" element={<MainLayout />}>
        <Route index element={<DashboardPage />} />

        <Route path="cases" element={<CasesPage />} />
        <Route path="cases/:id" element={<CaseDetailPage />} />

        <Route path="alerts" element={<AlertsPage />} />

        <Route
          path="notifications"
          element={<NotificationsPage />}
        />

        <Route path="reports" element={<ReportsPage />} />

        <Route element={<AdminRoute />}>
          <Route path="import" element={<ImportPage />} />
          <Route path="users" element={<UsersPage />} />
        </Route>
      </Route>
      </Route>
    </Routes>
  );
}

export default AppRoutes;

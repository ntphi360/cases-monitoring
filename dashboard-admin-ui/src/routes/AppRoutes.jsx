import { Routes, Route } from "react-router-dom";

import MainLayout from "../layouts/MainLayout";
import DashboardPage from "../pages/DashboardPage";
import CasesPage from "../pages/CasesPage";
import AlertsPage from "../pages/AlertsPage";
import NotificationsPage from "../pages/NotificationsPage";
import ReportsPage from "../pages/ReportsPage";
import ImportPage from "../pages/ImportPage";

function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<MainLayout />}>
        <Route index element={<DashboardPage />} />

        <Route path="cases" element={<CasesPage />} />

        <Route path="alerts" element={<AlertsPage />} />

        <Route
          path="notifications"
          element={<NotificationsPage />}
        />

        <Route path="reports" element={<ReportsPage />} />

        <Route path="import" element={<ImportPage />} />
      </Route>
    </Routes>
  );
}

export default AppRoutes;
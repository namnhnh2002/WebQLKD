import { Navigate, Route, Routes } from "react-router-dom";
import Login from "./pages/Login";
import Dashboard from "./pages/Dashboard";
import Operations from "./pages/Operations";
import AdminModule from "./pages/AdminModule";
import Pos from "./pages/Pos";
import Orders from "./pages/Orders";
import Tables from "./pages/Tables";
import Kitchen from "./pages/Kitchen";
import { useAuth } from "./context/AuthContext";

function ProtectedRoute({ children }: { children: JSX.Element }) {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? children : <Navigate to="/login" replace />;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        }
      />
      <Route path="/operations/:resource" element={<ProtectedRoute><Operations /></ProtectedRoute>} />
      <Route path="/admin/:module" element={<ProtectedRoute><AdminModule /></ProtectedRoute>} />
      <Route path="/pos" element={<ProtectedRoute><Pos /></ProtectedRoute>} />
      <Route path="/orders" element={<ProtectedRoute><Orders /></ProtectedRoute>} />
      <Route path="/tables" element={<ProtectedRoute><Tables /></ProtectedRoute>} />
      <Route path="/kitchen" element={<ProtectedRoute><Kitchen /></ProtectedRoute>} />
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}

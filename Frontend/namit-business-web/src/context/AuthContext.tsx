import { createContext, useContext, useEffect, useState, ReactNode } from "react";
import apiClient from "../api/client";

interface UserProfile {
  id: string;
  fullName: string;
  email: string;
  tenantId: string;
  tenantName: string;
  roles: string[];
  permissions: string[];
}

export interface TenantModuleContext {
  tenantId: string;
  tenantName: string;
  businessTypeCode: string;
  enabledModules: string[];
  permissions: string[];
}

interface AuthContextValue {
  user: UserProfile | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
  moduleContext: TenantModuleContext | null;
  moduleContextLoading: boolean;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(() => {
    const raw = localStorage.getItem("userProfile");
    return raw ? JSON.parse(raw) : null;
  });
  const [moduleContext, setModuleContext] = useState<TenantModuleContext | null>(() => {
    const raw = localStorage.getItem("moduleContext");
    return raw ? JSON.parse(raw) : null;
  });
  const [moduleContextLoading, setModuleContextLoading] = useState(false);

  const loadModuleContext = async () => {
    setModuleContextLoading(true);
    try {
      const { data } = await apiClient.get("/me/context");
      const context = data.data as TenantModuleContext;
      localStorage.setItem("moduleContext", JSON.stringify(context));
      setModuleContext(context);
    } finally {
      setModuleContextLoading(false);
    }
  };

  useEffect(() => {
    if (user && localStorage.getItem("accessToken")) {
      void loadModuleContext().catch(() => undefined);
    }
  }, []);

  const login = async (email: string, password: string) => {
    const { data } = await apiClient.post("/auth/login", { email: email.trim(), password });
    const result = data.data;
    localStorage.setItem("accessToken", result.accessToken);
    localStorage.setItem("refreshToken", result.refreshToken);
    localStorage.setItem("userProfile", JSON.stringify(result.user));
    setUser(result.user);
    await loadModuleContext().catch(() => undefined);
  };

  const logout = () => {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("userProfile");
    localStorage.removeItem("moduleContext");
    setUser(null);
    setModuleContext(null);
  };

  return (
    <AuthContext.Provider value={{ user, login, logout, isAuthenticated: !!user, moduleContext, moduleContextLoading }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth phải được dùng bên trong AuthProvider");
  return ctx;
}

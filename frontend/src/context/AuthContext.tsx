import { createContext, useContext, useState, type ReactNode } from "react";
import axios from "axios";

interface AuthUser {
  token: string;
  username: string;
  email: string;
}

interface AuthContextType {
  user: AuthUser | null;
  login: (email: string, password: string) => Promise<void>;
  register: (
    email: string,
    username: string,
    password: string,
  ) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

const API_BASE = "http://localhost:5249";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    // Restore from sessionStorage on page refresh
    const stored = sessionStorage.getItem("auth");
    return stored ? JSON.parse(stored) : null;
  });

  // Set auth header on every axios request when logged in
  if (user) {
    axios.defaults.headers.common["Authorization"] = `Bearer ${user.token}`;
  }

  const login = async (email: string, password: string) => {
    const response = await axios.post(`${API_BASE}/api/auth/login`, {
      email,
      password,
    });
    const authUser: AuthUser = {
      token: response.data.token,
      username: response.data.username,
      email: response.data.email,
    };
    setUser(authUser);
    sessionStorage.setItem("auth", JSON.stringify(authUser));
    axios.defaults.headers.common["Authorization"] = `Bearer ${authUser.token}`;
  };

  const register = async (
    email: string,
    username: string,
    password: string,
  ) => {
    const response = await axios.post(`${API_BASE}/api/auth/register`, {
      email,
      username,
      password,
    });
    const authUser: AuthUser = {
      token: response.data.token,
      username: response.data.username,
      email: response.data.email,
    };
    setUser(authUser);
    sessionStorage.setItem("auth", JSON.stringify(authUser));
    axios.defaults.headers.common["Authorization"] = `Bearer ${authUser.token}`;
  };

  const logout = () => {
    setUser(null);
    sessionStorage.removeItem("auth");
    delete axios.defaults.headers.common["Authorization"];
  };

  return (
    <AuthContext.Provider value={{ user, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside AuthProvider");
  return ctx;
}

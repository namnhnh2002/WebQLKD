import { useAuth } from "../context/AuthContext";

export default function Dashboard() {
  const { user, logout } = useAuth();

  return (
    <div style={{ padding: 32 }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h1>Xin chào, {user?.fullName}</h1>
        <button onClick={logout} style={{ padding: "8px 16px", borderRadius: 8, border: "1px solid #d1d5db", background: "#fff", cursor: "pointer" }}>
          Đăng xuất
        </button>
      </div>
      <p>Doanh nghiệp: <strong>{user?.tenantName}</strong></p>
      <p>Vai trò: {user?.roles.join(", ")}</p>
      <div style={{ marginTop: 24, padding: 20, background: "#fff", borderRadius: 12 }}>
        Dashboard shell — sẽ được mở rộng theo BusinessType ở Phase 8.
      </div>
    </div>
  );
}

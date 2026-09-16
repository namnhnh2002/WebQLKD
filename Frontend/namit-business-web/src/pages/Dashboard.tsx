import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import apiClient from "../api/client";

type DashboardSummary = { productCount: number; customerCount: number; orderCount: number; revenue: number; inventoryUnits: number; outstandingDebt: number; occupiedTableCount: number; availableTableCount: number };
const moduleMenu = [
  { module: "POS", label: "Bán hàng", icon: "🛒", to: "/pos", permission: "ORDER_VIEW" },
  { module: "TABLE", label: "Bàn", icon: "▦", to: "/tables", permission: "TABLE_VIEW" },
  { module: "KITCHEN", label: "Bếp", icon: "♨", to: "/kitchen", permission: "KITCHEN_VIEW" },
  { module: "PRODUCT", label: "Sản phẩm", icon: "◇", to: "/operations/products", permission: "PRODUCT_VIEW" },
  { module: "INVENTORY", label: "Kho hàng", icon: "▤", to: "/operations/inventory", permission: "INVENTORY_VIEW" },
  { module: "CUSTOMER", label: "Khách hàng", icon: "♙", to: "/operations/customers", permission: "CUSTOMER_VIEW" },
  { module: "SUPPLIER", label: "Nhà cung cấp", icon: "▱", to: "/operations/suppliers", permission: "SUPPLIER_VIEW" },
  { module: "REPORT", label: "Báo cáo", icon: "▥", to: "/admin/reports", permission: "REPORT_VIEW" },
  { module: "PAYMENT", label: "Tài chính", icon: "▣", to: "/admin/finance", permission: "PAYMENT_VIEW" },
  { module: "DEBT", label: "Công nợ", icon: "◉", to: "/admin/finance", permission: "DEBT_VIEW" }
];
const coreMenu = [
  { label: "Nhân viên", icon: "♙", to: "/admin/staff", permission: "USER_VIEW" },
  { label: "Cài đặt", icon: "⚙", to: "/admin/settings", permission: "SETTINGS_VIEW" }
];

export default function Dashboard() {
  const { user, logout, moduleContext, moduleContextLoading } = useAuth();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [summaryError, setSummaryError] = useState("");
  useEffect(() => {
    void apiClient.get("/admin/dashboard/summary")
      .then(response => setSummary(response.data?.data ?? response.data))
      .catch(requestError => setSummaryError(requestError.response?.data?.message ?? "Không thể tải tổng quan."));
  }, []);
  const permissions = moduleContext?.permissions ?? user?.permissions ?? [];
  const enabledModules = moduleContext?.enabledModules ?? moduleMenu.map(item => item.module);
  const visibleModules = moduleMenu.filter(item => enabledModules.includes(item.module) && (!item.permission || permissions.includes(item.permission)));
  const visibleCore = coreMenu.filter(item => permissions.includes(item.permission));

  return (
    <div className="dashboard-shell">
      <aside className="sidebar">
        <div className="sidebar-brand"><span className="brand-mark small">N</span><span>NAM IT <b>Business</b><small>Nền tảng quản lý kinh doanh đa ngành</small></span></div>
        <nav className="sidebar-nav">
          <Link className="nav-item active" to="/dashboard"><span>⌂</span>Tổng quan</Link>
          {visibleModules.map(item => <Link className="nav-item" key={item.module} to={item.to}><span>{item.icon}</span>{item.label}</Link>)}
          {visibleCore.map(item => <Link className="nav-item" key={item.permission} to={item.to}><span>{item.icon}</span>{item.label}</Link>)}
          {moduleContextLoading && <span className="nav-status">Đang tải module...</span>}
        </nav>
        <div className="sidebar-footer">
          <div className="store-switcher"><span className="store-icon">▦</span><div><strong>Cửa hàng Demo</strong><small>Cafe & Trà sữa</small></div><b>⌄</b></div>
          <button className="theme-button" type="button"><span>☼</span> Giao diện sáng <b>›</b></button>
          <button className="logout-button" onClick={logout}>Đăng xuất <span>↗</span></button>
        </div>
      </aside>

      <main className="dashboard-main">
        <header className="dashboard-header">
          <button className="menu-button" type="button" aria-label="Mở menu">☰</button>
          <div className="global-search"><span>⌕</span><input aria-label="Tìm kiếm" placeholder="Tìm kiếm nhanh... (sản phẩm, đơn hàng, khách hàng...)" /></div>
          <div className="header-actions"><button className="icon-button" aria-label="Thông báo">♧<b>3</b></button><div className="profile-chip"><div className="avatar">{user?.fullName?.charAt(0) ?? "N"}</div><div><strong>{user?.fullName ?? "Nguyễn Văn A"}</strong><small>Quản trị viên</small></div><span>⌄</span></div></div>
        </header>

        <div className="dashboard-content">
          <section className="dashboard-intro"><div><h1>👋 Xin chào, <span>{user?.fullName ?? "Nguyễn Văn A"}!</span></h1><p>Chúc bạn có một ngày làm việc hiệu quả!</p></div><button className="date-button" type="button">▣ &nbsp; Hôm nay (16/09/2025) &nbsp;⌄</button></section>
          {summaryError && <div className="operation-error">{summaryError}</div>}
          <section className="metric-grid">
            <Metric icon="$" label="Doanh thu" value={money(summary?.revenue)} change="Từ đơn hoàn tất" tone="green" />
            <Metric icon="▤" label="Tổng đơn hàng" value={number(summary?.orderCount)} change="Tất cả trạng thái" tone="blue" />
            <Metric icon="▣" label="Sản phẩm" value={number(summary?.productCount)} change="Trong tenant hiện tại" tone="purple" />
            <Metric icon="◉" label="Công nợ" value={money(summary?.outstandingDebt)} change="Số dư chưa thu" tone="red" />
            <Metric icon="♙" label="Khách hàng" value={number(summary?.customerCount)} change="Trong tenant hiện tại" tone="orange" />
            <Metric icon="◇" label="Tồn kho" value={number(summary?.inventoryUnits)} change="Tổng số lượng" tone="teal" />
          </section>
          <section className="lower-grid">
            <article className="panel table-panel"><PanelTitle icon="▦" title="Trạng thái bàn" action="Mở sơ đồ bàn →" /><div className="data-table"><div className="table-row"><span>Đang sử dụng</span><strong>{number(summary?.occupiedTableCount)}</strong></div><div className="table-row"><span>Đang trống</span><strong>{number(summary?.availableTableCount)}</strong></div></div></article>
            <article className="panel table-panel"><PanelTitle icon="▥" title="Tổng quan dữ liệu" action="Xem báo cáo →" /><div className="data-table"><div className="table-row"><span>Doanh thu</span><strong>{money(summary?.revenue)}</strong></div><div className="table-row"><span>Công nợ</span><strong>{money(summary?.outstandingDebt)}</strong></div></div></article>
            <article className="panel activity-panel"><PanelTitle icon="◷" title="Nguồn dữ liệu" action="Làm mới" /><p className="dashboard-data-note">Số liệu được tải trực tiếp từ API và được giới hạn theo tenant hiện tại.</p></article>
          </section>
          <footer className="dashboard-footer">NAM IT Business © 2025. Tất cả quyền được bảo lưu.<span>Điều khoản sử dụng &nbsp;|&nbsp; Chính sách bảo mật &nbsp;|&nbsp; Hỗ trợ</span></footer>
        </div>
      </main>
    </div>
  );
}

function Metric({ icon, label, value, change, tone, down = false }: { icon: string; label: string; value: string; change: string; tone: string; down?: boolean }) {
  return <article className={`metric-card tone-${tone}`}><div className="metric-icon">{icon}</div><span>{label}</span><strong>{value}</strong><small className={down ? "down" : ""}>↗ {change}</small></article>;
}

function money(value?: number) { return value === undefined ? "-" : `${value.toLocaleString("vi-VN")}đ`; }
function number(value?: number) { return value === undefined ? "-" : value.toLocaleString("vi-VN"); }
function PanelTitle({ icon, title, action }: { icon: string; title: string; action: string }) { return <div className="panel-heading"><h2><span>{icon}</span>{title}</h2><button type="button">{action}</button></div>; }

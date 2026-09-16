import { useAuth } from "../context/AuthContext";

export default function Dashboard() {
  const { user, logout } = useAuth();

  return (
    <div className="dashboard-shell">
      <aside className="sidebar">
        <div className="sidebar-brand"><span className="brand-mark small">N</span><span>NAM IT <b>Business</b></span></div>
        <p className="sidebar-label">WORKSPACE</p>
        <nav className="sidebar-nav">
          <a className="nav-item active" href="#overview"><span>◈</span>Tổng quan</a>
          <a className="nav-item" href="#orders"><span>↗</span>Đơn hàng</a>
          <a className="nav-item" href="#customers"><span>◎</span>Khách hàng</a>
          <a className="nav-item" href="#inventory"><span>▦</span>Kho hàng</a>
          <a className="nav-item" href="#reports"><span>◒</span>Báo cáo</a>
        </nav>
        <div className="sidebar-footer">
          <div className="support-card"><span>●</span><div><strong>Trợ lý vận hành</strong><small>Đang trực tuyến</small></div></div>
          <button className="logout-button" onClick={logout}>Đăng xuất <span>↗</span></button>
        </div>
      </aside>

      <main className="dashboard-main">
        <header className="dashboard-header">
          <div><p className="eyebrow">THỨ TƯ, 16 THÁNG 9, 2026</p><h1>Tổng quan vận hành</h1></div>
          <div className="profile-chip"><div className="avatar">{user?.fullName?.charAt(0) ?? "A"}</div><div><strong>{user?.fullName ?? "Admin"}</strong><small>Quản trị viên</small></div><span>⌄</span></div>
        </header>

        <section className="welcome-banner">
          <div><p className="eyebrow">{user?.tenantName || "NAM IT BUSINESS"}</p><h2>Một ngày tốt để tăng trưởng.</h2><p>Dữ liệu vận hành của bạn đang được tập hợp tại một nơi.</p></div>
          <div className="banner-orbit"><span>✦</span></div>
        </section>

        <section className="metric-grid">
          <article className="metric-card accent-teal"><div className="metric-top"><span>Doanh thu hôm nay</span><span className="metric-icon">↗</span></div><strong>₫ 0</strong><small>Chưa có dữ liệu giao dịch</small></article>
          <article className="metric-card accent-orange"><div className="metric-top"><span>Đơn hàng</span><span className="metric-icon">◈</span></div><strong>0</strong><small>Đơn hàng trong ngày</small></article>
          <article className="metric-card accent-blue"><div className="metric-top"><span>Khách hàng</span><span className="metric-icon">◎</span></div><strong>0</strong><small>Khách hàng đang hoạt động</small></article>
        </section>

        <section className="dashboard-lower">
          <div className="panel chart-panel"><div className="panel-heading"><div><p className="eyebrow">HIỆU SUẤT</p><h3>Dòng tiền theo thời gian</h3></div><button className="period-button">7 ngày⌄</button></div><div className="empty-chart"><div className="chart-line" /><span>Biểu đồ sẽ xuất hiện khi có giao dịch đầu tiên</span></div></div>
          <div className="panel setup-panel"><p className="eyebrow">BẮT ĐẦU NHANH</p><h3>Hoàn thiện không gian làm việc</h3><p>Thiết lập các thông tin nền tảng để bắt đầu vận hành.</p><div className="setup-row"><span className="setup-check">1</span><span>Thông tin doanh nghiệp</span><b>›</b></div><div className="setup-row"><span className="setup-check muted">2</span><span>Tạo sản phẩm đầu tiên</span><b>›</b></div></div>
        </section>
      </main>
    </div>
  );
}

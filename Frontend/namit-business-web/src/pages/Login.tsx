import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("admin");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await login(email, password);
      navigate("/dashboard");
    } catch (err: any) {
      setError(err.response?.data?.message ?? "Đăng nhập thất bại.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="login-page">
      <div className="login-glow login-glow-one" />
      <div className="login-glow login-glow-two" />
      <section className="login-showcase">
        <div className="brand-mark">N</div>
        <p className="eyebrow">NAM IT / BUSINESS OS</p>
        <h1>Điều hành doanh nghiệp bằng một nhịp nhìn.</h1>
        <p className="showcase-copy">
          Một không gian tập trung cho vận hành, doanh thu và những quyết định quan trọng mỗi ngày.
        </p>
        <div className="showcase-stat">
          <span className="status-dot" />
          <span>Hệ thống sẵn sàng</span>
          <strong>Phase 01</strong>
        </div>
      </section>

      <form className="login-card" onSubmit={handleSubmit}>
        <div className="mobile-brand">
          <div className="brand-mark small">N</div>
          <span>NAM IT Business</span>
        </div>
        <div className="form-heading">
          <p className="eyebrow">WELCOME BACK</p>
          <h2>Chào mừng trở lại</h2>
          <p>Đăng nhập để tiếp tục quản lý hoạt động kinh doanh.</p>
        </div>

        {error && <div className="login-error" role="alert">{error}</div>}

        <label className="field-label" htmlFor="account">Tài khoản</label>
        <input
          id="account"
          className="field-input"
          type="text"
          placeholder="admin"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />

        <label className="field-label" htmlFor="password">Mật khẩu</label>
        <input
          id="password"
          className="field-input"
          type="password"
          placeholder="Nhập mật khẩu của bạn"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />

        <button className="primary-button" type="submit" disabled={loading}>
          <span>{loading ? "Đang xác thực..." : "Đăng nhập hệ thống"}</span>
          {!loading && <span aria-hidden="true">→</span>}
        </button>
        <p className="login-hint">Tài khoản quản trị mặc định: <strong>admin</strong></p>
      </form>
    </main>
  );
}

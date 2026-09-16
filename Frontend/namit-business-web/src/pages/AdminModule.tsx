import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import apiClient from "../api/client";

type ModuleName = "reports" | "finance" | "staff" | "settings";
const modules: Record<ModuleName, { title: string; endpoint: string }> = {
  reports: { title: "Báo cáo", endpoint: "/admin/reports/summary" },
  finance: { title: "Tài chính", endpoint: "/admin/finance/summary" },
  staff: { title: "Nhân viên", endpoint: "/admin/staff" },
  settings: { title: "Cài đặt", endpoint: "/admin/settings" }
};

export default function AdminModule() {
  const { module = "reports" } = useParams<{ module: string }>();
  const name = (module in modules ? module : "reports") as ModuleName;
  const [data, setData] = useState<any>(null);
  const [error, setError] = useState("");
  useEffect(() => { apiClient.get(modules[name].endpoint).then(response => setData(response.data?.data ?? response.data)).catch(requestError => setError(requestError.response?.data?.message ?? "Không thể tải dữ liệu.")); }, [name]);
  return <div className="operation-page"><header className="operation-header"><div><Link className="back-link" to="/dashboard">← Tổng quan</Link><h1>{modules[name].title}</h1><p>Dữ liệu được lọc theo tenant hiện tại.</p></div><button className="operation-refresh" onClick={() => window.location.reload()} aria-label="Tải lại">↻</button></header>{error ? <div className="operation-error">{error}</div> : !data ? <div className="operation-empty">Đang tải dữ liệu...</div> : <ModuleContent name={name} data={data} />}</div>;
}

function ModuleContent({ name, data }: { name: ModuleName; data: any }) {
  if (name === "staff") return <div className="admin-list">{data.map((staff: any) => <article className="admin-card" key={staff.id}><div className="avatar">{staff.fullName?.charAt(0)}</div><div><strong>{staff.fullName}</strong><p>{staff.email} · {staff.phone ?? "Chưa có số điện thoại"}</p><small>{staff.roles?.join(", ") || "Chưa gán vai trò"} · {staff.status}</small></div></article>)}</div>;
  if (name === "settings") return <div className="admin-settings"><Info label="Doanh nghiệp" value={data.name} /><Info label="Mã tenant" value={data.code} /><Info label="Trạng thái" value={data.status} /><h2>Chi nhánh</h2>{data.branches?.map((branch: any) => <Info key={branch.id} label={branch.name} value={`${branch.code} · ${branch.address ?? "Chưa có địa chỉ"}`} />)}</div>;
  const entries = name === "reports" ? [["Doanh thu", data.revenue], ["Đã thu", data.paidAmount], ["Công nợ", data.debtAmount], ["Số đơn hàng", data.orderCount], ["Sản phẩm", data.productCount], ["Sản phẩm sắp hết", data.lowStockCount]] : [["Tổng tiền đã thu", data.totalPaid], ["Công nợ còn lại", data.outstandingDebt], ["Thu hôm nay", data.collectedToday], ["Khoản nợ đang mở", data.debtCount]];
  return <div className="admin-metrics">{entries.map(([label, value]) => <article className="admin-metric" key={label}><span>{label}</span><strong>{typeof value === "number" && label !== "Số đơn hàng" && label !== "Sản phẩm" && label !== "Sản phẩm sắp hết" && label !== "Khoản nợ đang mở" ? `${value.toLocaleString("vi-VN")}đ` : value}</strong></article>)}</div>;
}

function Info({ label, value }: { label: string; value: string }) { return <div className="admin-info"><span>{label}</span><strong>{value}</strong></div>; }

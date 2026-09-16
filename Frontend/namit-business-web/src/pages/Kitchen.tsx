import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import apiClient from "../api/client";

type KitchenOrder = { id: string; orderId: string; status: string; createdAt: string; items: { productName: string; quantity: number; note?: string; station: string; toppings: string[] }[] };
const statuses = ["Pending", "Preparing", "Ready", "Served", "Cancelled"];

export default function Kitchen() {
  const [orders, setOrders] = useState<KitchenOrder[]>([]);
  const [error, setError] = useState("");
  const [station, setStation] = useState("ALL");
  const load = async () => { try { const response = await apiClient.get("/hospitality/kitchen"); setOrders(response.data?.data ?? response.data ?? []); } catch (requestError: any) { setError(requestError.response?.data?.message ?? "Không thể tải kitchen orders."); } };
  useEffect(() => { void load(); }, []);
  const update = async (order: KitchenOrder, status: string) => { try { await apiClient.post(`/hospitality/kitchen/${order.id}/status/${status}`); await load(); } catch (requestError: any) { setError(requestError.response?.data?.message ?? "Không thể cập nhật trạng thái bếp."); } };
  const visibleOrders = orders.map(order => ({ ...order, items: order.items.filter(item => station === "ALL" || item.station.toUpperCase() === station) })).filter(order => order.items.length);
  return <div className="operation-page"><header className="operation-header"><div><Link className="back-link" to="/dashboard">← Tổng quan</Link><h1>Kitchen / Bar</h1><p>Theo dõi món đã gửi theo station từ order thật.</p></div><div className="operation-actions"><button className={station === "ALL" ? "operation-primary" : "operation-refresh"} onClick={() => setStation("ALL")}>Tất cả</button><button className={station === "KITCHEN" ? "operation-primary" : "operation-refresh"} onClick={() => setStation("KITCHEN")}>Kitchen</button><button className={station === "BAR" ? "operation-primary" : "operation-refresh"} onClick={() => setStation("BAR")}>Bar</button><button className="operation-refresh" onClick={() => void load()} aria-label="Tải lại">↻</button></div></header>{error && <div className="operation-error">{error}</div>}{!visibleOrders.length && !error ? <div className="operation-empty">Chưa có món trong station này.</div> : <div className="kitchen-grid">{visibleOrders.map(order => <article className="kitchen-card" key={order.id}><div className="kitchen-card-head"><strong>Order {order.orderId.slice(0, 8)}</strong><span>{order.status}</span></div><small>{new Date(order.createdAt).toLocaleString("vi-VN")}</small><div className="kitchen-items">{order.items.map((item, index) => <div key={`${item.productName}-${index}`}><span>{item.productName} × {item.quantity} · {item.station}</span><small>{item.toppings.join(", ")} {item.note ?? ""}</small></div>)}</div><div className="kitchen-actions">{statuses.map(status => <button className={order.status === status ? "selected" : ""} key={status} onClick={() => void update(order, status)}>{status}</button>)}</div></article>)}</div>}</div>;
}

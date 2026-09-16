import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import apiClient from "../api/client";
import { useAuth } from "../context/AuthContext";

type Table = { id: string; areaName: string; name: string; capacity: number; status: string; hasActiveOrder: boolean };
type Product = { id: string; categoryId: string; categoryName: string; name: string; sellingPrice: number; currentStock: number };
type Topping = { id: string; name: string; price: number };
type Customer = { id: string; name: string };
type Item = { id: string; productName: string; quantity: number; unitPrice: number; lineTotal: number; note?: string; toppings: { name: string }[] };
type Detail = { id: string; areaName: string; name: string; status: string; openedAt?: string; order?: { id: string; orderNumber: string; subtotal: number; discount: number; total: number; items: Item[] } };
type Action = "add" | "transferItem" | "transferTable" | "merge" | "split" | "pay" | null;

const money = (value: number) => `${value.toLocaleString("vi-VN")}đ`;
const statusText: Record<string, string> = { Available: "TRỐNG", Occupied: "ĐANG SỬ DỤNG", Reserved: "ĐẶT TRƯỚC" };

export default function Tables() {
  const { moduleContext, user } = useAuth();
  const permissions = moduleContext?.permissions ?? user?.permissions ?? [];
  const can = (permission: string) => permissions.includes(permission) || user?.roles.includes("TENANT_ADMIN") === true;
  const [tables, setTables] = useState<Table[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [toppings, setToppings] = useState<Topping[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [detail, setDetail] = useState<Detail | null>(null);
  const [action, setAction] = useState<Action>(null);
  const [targetId, setTargetId] = useState("");
  const [itemId, setItemId] = useState("");
  const [itemQuantity, setItemQuantity] = useState(1);
  const [splitQuantities, setSplitQuantities] = useState<Record<string, number>>({});
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("");
  const [quantity, setQuantity] = useState(1);
  const [note, setNote] = useState("");
  const [selectedToppings, setSelectedToppings] = useState<string[]>([]);
  const [customerId, setCustomerId] = useState("");
  const [paymentMethod, setPaymentMethod] = useState(1);
  const [paidAmount, setPaidAmount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [processing, setProcessing] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const load = async () => {
    setLoading(true);
    try {
      const results = await Promise.all([
        apiClient.get("/hospitality/tables"),
        apiClient.get("/products"),
        apiClient.get("/hospitality/toppings"),
        apiClient.get("/customers")
      ]);
      setTables(results[0].data?.data ?? results[0].data ?? []);
      setProducts(results[1].data?.data ?? []);
      setToppings(results[2].data?.data ?? results[2].data ?? []);
      setCustomers(results[3].data?.data ?? []);
    } catch (requestError: any) {
      setError(formatError(requestError));
    } finally { setLoading(false); }
  };
  useEffect(() => { void load(); }, []);

  const openDetail = async (id: string) => {
    try { const response = await apiClient.get(`/hospitality/tables/${id}`); setDetail(response.data?.data ?? response.data); setError(""); }
    catch (requestError: any) { setError(formatError(requestError)); }
  };
  const mutate = async (request: () => Promise<unknown>, success: string) => {
    setProcessing(true); setError(""); setMessage("");
    try { await request(); await load(); if (detail) await openDetail(detail.id); setAction(null); setMessage(success); }
    catch (requestError: any) { setError(formatError(requestError)); }
    finally { setProcessing(false); }
  };

  const categories = useMemo(() => [...new Map(products.map(item => [item.categoryId, item.categoryName])).entries()], [products]);
  const visibleProducts = products.filter(item => (!category || item.categoryId === category) && item.name.toLowerCase().includes(search.toLowerCase()));
  const groups = [...new Set(tables.map(item => item.areaName))];
  const availableTables = tables.filter(table => table.id !== detail?.id && table.status === "Available");
  const occupiedTables = tables.filter(table => table.id !== detail?.id && table.status === "Occupied");
  const selectedItem = detail?.order?.items.find(item => item.id === itemId);

  if (loading) return <div className="operation-page"><div className="operation-empty">Đang tải sơ đồ bàn...</div></div>;
  return <div className="operation-page">
    <header className="operation-header"><div><Link className="back-link" to="/dashboard">← Tổng quan</Link><h1>Sơ đồ bàn</h1><p>Quản lý bàn và order từ API thật.</p></div><button className="operation-refresh" disabled={processing} onClick={() => void load()}>↻</button></header>
    {error && <div className="operation-error">{error}</div>}{message && <div className="pos-success">{message}</div>}
    {groups.map(area => <section className="table-area" key={area}><h2>{area}</h2><div className="table-grid">{tables.filter(table => table.areaName === area).map(table => <button className="dining-table" key={table.id} onClick={() => void openDetail(table.id)}><strong>{table.name}</strong><span>{statusText[table.status] ?? table.status}</span><small>{table.capacity} chỗ · {table.hasActiveOrder ? "Có order" : "Chưa có order"}</small></button>)}</div></section>)}
    {detail && <DetailPanel detail={detail} action={action} setAction={setAction} processing={processing} can={can} availableTables={availableTables} occupiedTables={occupiedTables} targetId={targetId} setTargetId={setTargetId} itemId={itemId} setItemId={setItemId} itemQuantity={itemQuantity} setItemQuantity={setItemQuantity} selectedItem={selectedItem} splitQuantities={splitQuantities} setSplitQuantities={setSplitQuantities} mutate={mutate} products={visibleProducts} categories={categories} search={search} setSearch={setSearch} category={category} setCategory={setCategory} selectedProduct={selectedProduct} setSelectedProduct={setSelectedProduct} quantity={quantity} setQuantity={setQuantity} note={note} setNote={setNote} toppings={toppings} selectedToppings={selectedToppings} setSelectedToppings={setSelectedToppings} customers={customers} customerId={customerId} setCustomerId={setCustomerId} paymentMethod={paymentMethod} setPaymentMethod={setPaymentMethod} paidAmount={paidAmount} setPaidAmount={setPaidAmount} onClose={() => setDetail(null)} />}
  </div>;
}

function DetailPanel(props: any) {
  const { detail, action, setAction, processing, can, availableTables, occupiedTables, targetId, setTargetId, itemId, setItemId, itemQuantity, setItemQuantity, selectedItem, splitQuantities, setSplitQuantities, mutate, products, categories, search, setSearch, category, setCategory, selectedProduct, setSelectedProduct, quantity, setQuantity, note, setNote, toppings, selectedToppings, setSelectedToppings, customers, customerId, setCustomerId, paymentMethod, setPaymentMethod, paidAmount, setPaidAmount, onClose } = props;
  const requireOrder = Boolean(detail.order);
  return <section className="operation-form table-detail-panel"><div className="operation-header"><div><h2>{detail.name}</h2><p>{detail.areaName} · {statusText[detail.status] ?? detail.status}</p></div><button className="operation-refresh" onClick={onClose}>×</button></div>
    {!requireOrder ? <button className="operation-primary" disabled={processing} onClick={() => void mutate(() => apiClient.post(`/hospitality/tables/${detail.id}/open`), "Đã mở bàn.")}>Mở bàn</button> : <>
      <h3>{detail.order.orderNumber}</h3>{detail.order.items.map((item: Item) => <div className="operation-row" key={item.id}><span>{item.productName} × {item.quantity}</span><span>{money(item.unitPrice)}</span><span>{item.toppings.map(topping => topping.name).join(", ")}</span><span>{item.note ?? ""}</span><b>{money(item.lineTotal)}</b></div>)}<p>Tạm tính: {money(detail.order.subtotal)} · Giảm: {money(detail.order.discount)} · Tổng: {money(detail.order.total)}</p>
      <div className="operation-actions"><button disabled={!can("TABLE_UPDATE") || processing} onClick={() => setAction("add")}>Thêm món</button><button disabled={!can("TABLE_TRANSFER") || processing} onClick={() => setAction("transferItem")}>Chuyển món</button><button disabled={!can("TABLE_TRANSFER") || processing} onClick={() => setAction("transferTable")}>Chuyển bàn</button><button disabled={!can("TABLE_MERGE") || processing} onClick={() => setAction("merge")}>Gộp bàn</button><button disabled={!can("TABLE_SPLIT") || processing} onClick={() => setAction("split")}>Tách bàn</button><button disabled={!can("KITCHEN_UPDATE") || processing} onClick={() => void mutate(() => apiClient.post(`/hospitality/orders/${detail.order.id}/kitchen`), "Đã gửi món đến bếp/bar.")}>Gửi bếp</button><button className="operation-primary" disabled={!can("PAYMENT_CREATE") || processing} onClick={() => setAction("pay")}>Thanh toán</button></div>
      {action === "add" && <AddPanel {...{ products, categories, search, setSearch, category, setCategory, selectedProduct, setSelectedProduct, quantity, setQuantity, note, setNote, toppings, selectedToppings, setSelectedToppings, processing, detail, mutate }} />}
      {action === "transferItem" && <ActionPanel title="Chuyển món" tables={availableTables} targetId={targetId} setTargetId={setTargetId}><select value={itemId} onChange={event => setItemId(event.target.value)}><option value="">Chọn món</option>{detail.order.items.map((item: Item) => <option key={item.id} value={item.id}>{item.productName} · {item.quantity}</option>)}</select><input type="number" min="1" max={selectedItem?.quantity} value={itemQuantity} onChange={event => setItemQuantity(Number(event.target.value))} /><button disabled={processing || !targetId || !itemId} className="operation-primary" onClick={() => void mutate(() => apiClient.post(`/hospitality/tables/${detail.id}/items/transfer/${targetId}`, { orderItemId: itemId, quantity: itemQuantity }), "Đã chuyển món.")}>Xác nhận chuyển</button></ActionPanel>}
      {action === "transferTable" && <ActionPanel title="Chuyển bàn" tables={availableTables} targetId={targetId} setTargetId={setTargetId}><button disabled={processing || !targetId} className="operation-primary" onClick={() => void mutate(() => apiClient.post(`/hospitality/tables/${detail.id}/transfer/${targetId}`), "Đã chuyển bàn.")}>Xác nhận chuyển bàn</button></ActionPanel>}
      {action === "merge" && <ActionPanel title="Gộp bàn" tables={occupiedTables} targetId={targetId} setTargetId={setTargetId}><button disabled={processing || !targetId} className="operation-primary" onClick={() => void mutate(() => apiClient.post(`/hospitality/tables/${targetId}/merge/${detail.id}`), "Đã gộp bàn.")}>Xác nhận gộp</button></ActionPanel>}
      {action === "split" && <ActionPanel title="Tách bàn" tables={availableTables} targetId={targetId} setTargetId={setTargetId}><div>{detail.order.items.map((item: Item) => <label className="operation-row" key={item.id}>{item.productName} × {item.quantity}<input type="number" min="0" max={item.quantity} value={splitQuantities[item.id] ?? 0} onChange={event => setSplitQuantities({ ...splitQuantities, [item.id]: Math.min(item.quantity, Math.max(0, Number(event.target.value))) })} /></label>)}</div><button disabled={processing || !targetId} className="operation-primary" onClick={() => { const items = detail.order.items.map((item: Item) => ({ orderItemId: item.id, quantity: splitQuantities[item.id] ?? 0 })).filter((item: { orderItemId: string; quantity: number }) => item.quantity > 0); if (!items.length) return; void mutate(() => apiClient.post(`/hospitality/tables/${detail.id}/split/${targetId}`, { items }), "Đã tách bàn."); }}>Xác nhận tách</button></ActionPanel>}
      {action === "pay" && <PaymentPanel detail={detail} processing={processing} customers={customers} customerId={customerId} setCustomerId={setCustomerId} paymentMethod={paymentMethod} setPaymentMethod={setPaymentMethod} paidAmount={paidAmount} setPaidAmount={setPaidAmount} mutate={mutate} />}
    </>}
  </section>;
}

function AddPanel(props: any) { const { products, categories, search, setSearch, category, setCategory, selectedProduct, setSelectedProduct, quantity, setQuantity, note, setNote, toppings, selectedToppings, setSelectedToppings, processing, detail, mutate } = props; return <div className="operation-form"><h3>Thêm món</h3><input placeholder="Tìm sản phẩm" value={search} onChange={event => setSearch(event.target.value)} /><select value={category} onChange={event => setCategory(event.target.value)}><option value="">Tất cả danh mục</option>{categories.map(([id, name]: [string, string]) => <option key={id} value={id}>{name}</option>)}</select><div className="pos-product-grid">{products.map((product: Product) => <button className="pos-product" key={product.id} disabled={product.currentStock <= 0} onClick={() => setSelectedProduct(product)}><strong>{product.name}</strong><small>{money(product.sellingPrice)} · Tồn {product.currentStock}</small></button>)}</div>{selectedProduct && <><input type="number" min="1" max={selectedProduct.currentStock} value={quantity} onChange={event => setQuantity(Number(event.target.value))} /><div>{toppings.map((topping: Topping) => <label key={topping.id}><input type="checkbox" checked={selectedToppings.includes(topping.id)} onChange={event => setSelectedToppings(event.target.checked ? [...selectedToppings, topping.id] : selectedToppings.filter((id: string) => id !== topping.id))} /> {topping.name} +{money(topping.price)}</label>)}</div><input placeholder="Ghi chú" value={note} onChange={event => setNote(event.target.value)} /><button className="operation-primary" disabled={processing} onClick={() => void mutate(() => apiClient.post(`/hospitality/tables/${detail.id}/items`, { productId: selectedProduct.id, quantity, note: note || null, toppingIds: selectedToppings }), "Đã thêm món vào bàn.")}>Thêm vào bàn</button></>}</div>; }
function ActionPanel({ title, tables, targetId, setTargetId, children }: any) { return <div className="operation-form"><h3>{title}</h3><select value={targetId} onChange={event => setTargetId(event.target.value)}><option value="">Chọn bàn đích</option>{tables.map((table: Table) => <option key={table.id} value={table.id}>{table.name} · {table.areaName}</option>)}</select>{children}</div>; }
function PaymentPanel({ detail, processing, customers, customerId, setCustomerId, paymentMethod, setPaymentMethod, paidAmount, setPaidAmount, mutate }: any) { const total = detail.order.total; const debt = paymentMethod === 5; return <div className="operation-form"><h3>Thanh toán</h3><p>Tạm tính: {money(detail.order.subtotal)} · Giảm: {money(detail.order.discount)} · Tổng: {money(total)}</p><select value={paymentMethod} onChange={event => setPaymentMethod(Number(event.target.value))}><option value="1">Tiền mặt</option><option value="3">Chuyển khoản</option><option value="2">Thẻ</option><option value="5">Công nợ</option></select>{debt && <select value={customerId} onChange={event => setCustomerId(event.target.value)}><option value="">Chọn khách hàng</option>{customers.map((customer: Customer) => <option key={customer.id} value={customer.id}>{customer.name}</option>)}</select>}{!debt && <><input type="number" min="0" value={paidAmount} onChange={event => setPaidAmount(Number(event.target.value))} placeholder="Tiền khách đưa" /><p>Tiền thừa: {money(Math.max(0, paidAmount - total))}</p></>}<button className="operation-primary" disabled={processing || (debt && !customerId) || (!debt && paidAmount < total)} onClick={() => void mutate(() => apiClient.post(`/hospitality/tables/${detail.id}/pay`, { customerId: customerId || null, payments: [{ amount: debt ? 0 : paidAmount, method: paymentMethod, reference: null }] }), "Thanh toán thành công.")}>Xác nhận thanh toán</button></div>; }
function formatError(error: any) { const status = error.response?.status; if (status === 403) return "Bạn không có quyền thực hiện thao tác này."; if (status === 409) return "Dữ liệu vừa thay đổi. Vui lòng tải lại."; if (status >= 500) return "Có lỗi xảy ra. Vui lòng thử lại."; return error.response?.data?.message ?? "Thao tác không thành công."; }

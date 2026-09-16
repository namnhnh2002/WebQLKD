import { FormEvent, useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import apiClient from "../api/client";

const resources = {
  products: { title: "Sản phẩm", endpoint: "/products", columns: ["Mã", "Tên sản phẩm", "Đơn vị", "Giá bán", "Tồn tối thiểu"] },
  customers: { title: "Khách hàng", endpoint: "/customers", columns: ["Mã", "Tên khách hàng", "Điện thoại", "Email", "Hạn mức"] },
  suppliers: { title: "Nhà cung cấp", endpoint: "/suppliers", columns: ["Mã", "Tên nhà cung cấp", "Điện thoại", "Email", "Mã số thuế"] }
} as const;

type Resource = keyof typeof resources;
type Item = Record<string, string | number | null>;

export default function Operations() {
  const { resource = "products" } = useParams<{ resource: string }>();
  const inventory = resource === "inventory";
  const name = (resource in resources ? resource : "products") as Resource;
  const [items, setItems] = useState<Item[]>([]);
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showForm, setShowForm] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const response = await apiClient.get(inventory ? "/inventory/low-stock" : resources[name].endpoint);
      const data = response.data?.data ?? response.data ?? [];
      setItems(Array.isArray(data) ? data : []);
      setError("");
    } catch (requestError: any) {
      setError(requestError.response?.data?.message ?? "Không thể tải dữ liệu. Hãy kiểm tra API đang chạy.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, [resource]);
  const filtered = useMemo(() => items.filter(item => JSON.stringify(item).toLowerCase().includes(query.toLowerCase())), [items, query]);
  const title = inventory ? "Cảnh báo tồn kho" : resources[name].title;

  return <div className="operation-page">
    <header className="operation-header"><div><Link className="back-link" to="/dashboard">← Tổng quan</Link><h1>{title}</h1><p>Dữ liệu vận hành trong tenant hiện tại.</p></div><div className="operation-actions"><input value={query} onChange={event => setQuery(event.target.value)} placeholder="Tìm kiếm..." aria-label="Tìm kiếm dữ liệu" />{!inventory && <button className="operation-primary" onClick={() => setShowForm(value => !value)}>{showForm ? "Đóng form" : "+ Thêm mới"}</button>}<button className="operation-refresh" onClick={() => void load()} aria-label="Tải lại">↻</button></div></header>
    {showForm && <CreateForm resource={name} onSaved={() => { setShowForm(false); void load(); }} />}
    {error && <div className="operation-error">{error}</div>}
    {loading ? <div className="operation-empty">Đang tải dữ liệu...</div> : filtered.length === 0 ? <div className="operation-empty">Chưa có dữ liệu phù hợp.</div> : <DataTable items={filtered} resource={inventory ? "inventory" : name} />}
  </div>;
}

function DataTable({ items, resource }: { items: Item[]; resource: Resource | "inventory" }) {
  const columns = resource === "inventory" ? ["Sản phẩm", "Tồn hiện tại", "Tồn tối thiểu", "Trạng thái"] : resources[resource].columns;
  return <div className="operation-table"><div className="operation-row operation-row-head">{columns.map(column => <span key={column}>{column}</span>)}</div>{items.map((item, index) => <div className="operation-row" key={String(item.id ?? index)}>{resource === "inventory" ? <><span>{String(item.productName ?? item.name ?? "-")}</span><span className="stock-number">{String(item.currentStock ?? 0)}</span><span>{String(item.minStock ?? 0)}</span><span><em className="stock-badge danger">Sắp hết</em></span></> : <><span>{String(item.code ?? "-")}</span><span className="operation-name">{String(item.name ?? "-")}</span><span>{String(item.phone ?? item.unit ?? "-")}</span><span>{String(item.email ?? item.sellingPrice ?? "-")}</span><span>{String(item.creditLimit ?? item.minStock ?? item.taxCode ?? "-")}</span></>}</div>)}</div>;
}

function CreateForm({ resource, onSaved }: { resource: Resource; onSaved: () => void }) {
  const [form, setForm] = useState({ code: "", name: "", phone: "", email: "", address: "" });
  const [categories, setCategories] = useState<Item[]>([]);
  const [categoryId, setCategoryId] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  useEffect(() => {
    if (resource !== "products") return;
    void apiClient.get("/categories").then(response => setCategories(response.data?.data ?? [])).catch(() => setCategories([]));
  }, [resource]);
  const submit = async (event: FormEvent) => {
    event.preventDefault(); setSaving(true); setError("");
    const payload = resource === "customers"
      ? { code: form.code, name: form.name, phone: form.phone, email: form.email, address: form.address, groupId: null, creditLimit: 0, status: 1 }
      : resource === "suppliers"
        ? { code: form.code, name: form.name, phone: form.phone, email: form.email, address: form.address, taxCode: "", status: 1 }
        : { categoryId, code: form.code, name: form.name, unit: "pcs", costPrice: 0, sellingPrice: 0, minStock: 0, status: 1 };
    try { await apiClient.post(resources[resource].endpoint, payload); onSaved(); } catch (requestError: any) { setError(requestError.response?.data?.message ?? "Không thể lưu dữ liệu."); } finally { setSaving(false); }
  };
  const update = (key: keyof typeof form, value: string) => setForm(current => ({ ...current, [key]: value }));
  return <form className="operation-form" onSubmit={submit}><h2>Thêm {resources[resource].title.toLowerCase()}</h2>{error && <div className="operation-error">{error}</div>}<div className="operation-form-grid"><input required placeholder="Mã" value={form.code} onChange={event => update("code", event.target.value)} /><input required placeholder="Tên" value={form.name} onChange={event => update("name", event.target.value)} />{resource === "products" && <select required value={categoryId} onChange={event => setCategoryId(event.target.value)}><option value="">Chọn danh mục</option>{categories.map(category => <option key={String(category.id)} value={String(category.id)}>{String(category.name)}</option>)}</select>}<input placeholder="Điện thoại" value={form.phone} onChange={event => update("phone", event.target.value)} /><input placeholder="Email" value={form.email} onChange={event => update("email", event.target.value)} /><input placeholder="Địa chỉ" value={form.address} onChange={event => update("address", event.target.value)} /></div><button className="operation-primary" disabled={saving}>{saving ? "Đang lưu..." : "Lưu dữ liệu"}</button></form>;
}

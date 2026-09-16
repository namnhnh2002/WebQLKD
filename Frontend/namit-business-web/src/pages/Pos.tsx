import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import apiClient from "../api/client";

type Product = { id: string; categoryId: string; categoryName: string; code: string; name: string; unit: string; sellingPrice: number; currentStock: number };
type CartLine = Product & { quantity: number };
type Customer = { id: string; name: string; phone?: string; creditLimit: number };
type Branch = { id: string; name: string; code: string };

export default function Pos() {
  const [products, setProducts] = useState<Product[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [branches, setBranches] = useState<Branch[]>([]);
  const [cart, setCart] = useState<CartLine[]>([]);
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("");
  const [branchId, setBranchId] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [discount, setDiscount] = useState(0);
  const [discountPercent, setDiscountPercent] = useState(0);
  const [paymentMethod, setPaymentMethod] = useState(1);
  const [paidAmount, setPaidAmount] = useState(0);
  const [reference, setReference] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  useEffect(() => {
    Promise.all([apiClient.get("/products"), apiClient.get("/customers"), apiClient.get("/branches")]).then(([productResponse, customerResponse, branchResponse]) => {
      setProducts(productResponse.data?.data ?? []);
      setCustomers(customerResponse.data?.data ?? []);
      const branchData = branchResponse.data?.data ?? [];
      setBranches(branchData);
      if (branchData.length === 1) setBranchId(branchData[0].id);
    }).catch(requestError => setError(requestError.response?.data?.message ?? "Không thể tải dữ liệu POS."))
      .finally(() => setLoading(false));
  }, []);

  const categories = useMemo(() => [...new Map(products.map(product => [product.categoryId, product.categoryName])).entries()], [products]);
  const visibleProducts = products.filter(product => (!category || product.categoryId === category) && JSON.stringify(product).toLowerCase().includes(search.toLowerCase()));
  const subtotal = cart.reduce((sum, line) => sum + line.sellingPrice * line.quantity, 0);
  const discountValue = discountPercent > 0 ? subtotal * discountPercent / 100 : discount;
  const total = Math.max(0, subtotal - discountValue);
  const change = Math.max(0, paidAmount - total);

  const addProduct = (product: Product) => setCart(lines => {
    const existing = lines.find(line => line.id === product.id);
    if (existing) return lines.map(line => line.id === product.id ? { ...line, quantity: Math.min(line.quantity + 1, product.currentStock) } : line);
    return product.currentStock > 0 ? [...lines, { ...product, quantity: 1 }] : lines;
  });
  const updateQuantity = (id: string, quantity: number) => setCart(lines => quantity <= 0 ? lines.filter(line => line.id !== id) : lines.map(line => line.id === id ? { ...line, quantity: Math.min(quantity, line.currentStock) } : line));
  const submit = async () => {
    setError(""); setMessage("");
    if (!branchId) return setError("Vui lòng chọn chi nhánh.");
    if (!cart.length) return setError("Chưa có sản phẩm trong giỏ hàng.");
    if (paymentMethod === 4 && !customerId) return setError("Muốn ghi công nợ phải chọn khách hàng.");
    if (paidAmount > total) return setError("Tiền khách đưa không hợp lệ.");
    setSaving(true);
    try {
      const response = await apiClient.post("/pos/orders", { branchId, customerId: customerId || null, items: cart.map(line => ({ productId: line.id, quantity: line.quantity, unitPrice: line.sellingPrice })), discount: discountPercent > 0 ? 0 : discount, discountPercent, payments: paidAmount > 0 ? [{ amount: paidAmount, method: paymentMethod, reference: reference || null }] : [] });
      setMessage(`Đã tạo đơn ${response.data?.orderNumber ?? response.data?.data?.orderNumber ?? "thành công"}.`);
      setCart([]); setDiscount(0); setDiscountPercent(0); setPaidAmount(0); setReference("");
    } catch (requestError: any) { setError(requestError.response?.data?.message ?? "Không thể tạo đơn hàng. Vui lòng thử lại."); } finally { setSaving(false); }
  };

  return <div className="pos-page"><header className="pos-header"><div><Link className="back-link" to="/dashboard">← Tổng quan</Link><h1>Bán hàng</h1><p>POS dùng chung cho mọi loại hình kinh doanh.</p></div><Link className="operation-primary" to="/orders">Lịch sử đơn hàng</Link></header>{error && <div className="operation-error">{error}</div>}{message && <div className="pos-success">{message}</div>}{loading ? <div className="operation-empty">Đang tải sản phẩm...</div> : <div className="pos-layout"><aside className="pos-categories"><strong>Danh mục</strong><button className={!category ? "selected" : ""} onClick={() => setCategory("")}>Tất cả</button>{categories.map(([id, name]) => <button className={category === id ? "selected" : ""} key={id} onClick={() => setCategory(id)}>{name}</button>)}</aside><section className="pos-products"><div className="pos-search"><span>⌕</span><input value={search} onChange={event => setSearch(event.target.value)} placeholder="Tìm sản phẩm hoặc barcode..." /></div><div className="pos-product-grid">{visibleProducts.map(product => <button className="pos-product" key={product.id} onClick={() => addProduct(product)} disabled={product.currentStock <= 0}><span className="pos-product-icon">{product.name.charAt(0)}</span><strong>{product.name}</strong><small>{product.sellingPrice.toLocaleString("vi-VN")}đ · {product.unit}</small><em>Tồn: {product.currentStock}</em></button>)}{!visibleProducts.length && <div className="operation-empty">Chưa có sản phẩm phù hợp.</div>}</div></section><aside className="pos-cart"><h2>Giỏ hàng <small>{cart.length} sản phẩm</small></h2>{!cart.length ? <div className="pos-cart-empty">Chưa có sản phẩm trong giỏ hàng</div> : <div className="pos-lines">{cart.map(line => <div className="pos-line" key={line.id}><div><strong>{line.name}</strong><small>{line.sellingPrice.toLocaleString("vi-VN")}đ / {line.unit}</small></div><input type="number" min="1" max={line.currentStock} value={line.quantity} onChange={event => updateQuantity(line.id, Number(event.target.value))} /><b>{(line.sellingPrice * line.quantity).toLocaleString("vi-VN")}đ</b><button onClick={() => updateQuantity(line.id, 0)} aria-label={`Xóa ${line.name}`}>×</button></div>)}</div>}<div className="pos-checkout"><label>Chi nhánh<select value={branchId} onChange={event => setBranchId(event.target.value)}><option value="">Chọn chi nhánh</option>{branches.map(branch => <option key={branch.id} value={branch.id}>{branch.name}</option>)}</select></label><label>Khách hàng<select value={customerId} onChange={event => setCustomerId(event.target.value)}><option value="">Khách lẻ</option>{customers.map(customer => <option key={customer.id} value={customer.id}>{customer.name} {customer.phone ? `· ${customer.phone}` : ""}</option>)}</select></label><div className="discount-fields"><input type="number" min="0" value={discount} disabled={discountPercent > 0} onChange={event => { setDiscount(Number(event.target.value)); setDiscountPercent(0); }} placeholder="Giảm tiền" /><input type="number" min="0" max="100" value={discountPercent} disabled={discount > 0} onChange={event => { setDiscountPercent(Number(event.target.value)); setDiscount(0); }} placeholder="Giảm %" /></div><div className="pos-total"><span>Tạm tính</span><b>{subtotal.toLocaleString("vi-VN")}đ</b><span>Giảm giá</span><b>-{discountValue.toLocaleString("vi-VN")}đ</b><strong>Tổng tiền</strong><strong>{total.toLocaleString("vi-VN")}đ</strong></div><div className="payment-methods">{[[1, "Tiền mặt"], [3, "Chuyển khoản"], [2, "Thẻ"], [5, "Công nợ"]].map(([value, label]) => <button className={paymentMethod === value ? "selected" : ""} key={value} onClick={() => { setPaymentMethod(Number(value)); if (value === 5) setPaidAmount(0); }}>{label}</button>)}</div><label>Tiền khách đưa<input type="number" min="0" disabled={paymentMethod === 5} value={paidAmount} onChange={event => setPaidAmount(Number(event.target.value))} /></label>{paymentMethod === 3 && <input value={reference} onChange={event => setReference(event.target.value)} placeholder="Mã giao dịch (tùy chọn)" />}<div className="pos-change">Tiền thừa: <b>{change.toLocaleString("vi-VN")}đ</b></div><button className="pos-pay" disabled={saving || !cart.length} onClick={() => void submit()}>{saving ? "Đang xử lý..." : "Thanh toán"}</button></div></aside></div>}</div>;
}

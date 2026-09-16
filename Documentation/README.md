# NAM IT Business — Phase 1: Foundation

## 1. Yêu cầu cài đặt trước
- Visual Studio 2022 (17.8+) với workload **ASP.NET and web development**
- .NET 8 SDK
- Node.js 18+ và npm
- PostgreSQL 14+ (chạy local, hoặc Docker: `docker run --name namit-pg -e POSTGRES_PASSWORD=CHANGE_ME -p 5432:5432 -d postgres:16`)
- (Tùy chọn) `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`

## 2. Mở solution trong Visual Studio
1. Giải nén file zip.
2. Mở `NamIT.Business.sln` bằng Visual Studio.
3. Click phải solution → **Restore NuGet Packages** (hoặc mở Developer PowerShell rồi chạy `dotnet restore`).
4. Set **NamIT.Business.Api** làm Startup Project (click phải → Set as Startup Project).

## 3. Cấu hình kết nối Database & JWT secret
Mở `Backend/NamIT.Business.Api/appsettings.json` (hoặc tốt hơn, dùng **User Secrets** — click phải project Api → Manage User Secrets) và cập nhật:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=namit_business;Username=postgres;Password=<mật khẩu thật của bạn>"
  },
  "Jwt": {
    "SecretKey": "<chuỗi random dài tối thiểu 32 ký tự, KHÔNG commit lên git>"
  }
}
```

## 4. Tạo migration & database
Mở **Package Manager Console** trong Visual Studio, chọn Default Project = `NamIT.Business.Infrastructure`, Startup Project = `NamIT.Business.Api`, rồi chạy:

```powershell
Add-Migration InitialCreate -Project NamIT.Business.Infrastructure -StartupProject NamIT.Business.Api
Update-Database -Project NamIT.Business.Infrastructure -StartupProject NamIT.Business.Api
```

Hoặc dùng CLI (Developer PowerShell, ở thư mục `Backend`):
```powershell
dotnet ef migrations add InitialCreate -p NamIT.Business.Infrastructure -s NamIT.Business.Api
dotnet ef database update -p NamIT.Business.Infrastructure -s NamIT.Business.Api
```

Khi chạy ở môi trường Development, `Program.cs` sẽ tự động `MigrateAsync()` + seed dữ liệu (BusinessTypes, Roles, Permissions) mỗi lần start — bạn vẫn cần tạo migration trước ít nhất 1 lần.

## 5. Chạy Backend
- Nhấn F5 trong Visual Studio (hoặc `dotnet run --project Backend/NamIT.Business.Api`).
- Mở Swagger tại `https://localhost:<port>/swagger`.
- Test luồng:
  1. `POST /api/auth/register-tenant` — tạo doanh nghiệp + tài khoản admin đầu tiên.
  2. `POST /api/auth/login` — lấy accessToken + refreshToken.
  3. Bấm **Authorize** trên Swagger, nhập `Bearer <accessToken>`.
  4. `GET /api/me` — kiểm tra thông tin user + TenantId.

## 6. Chạy Frontend
```bash
cd Frontend/namit-business-web
npm install
cp .env.example .env    # sửa VITE_API_BASE_URL đúng port của Api
npm run dev
```
Mở `http://localhost:5173` → trang Login sẽ hiện ra.

## 7. Chạy test (Tenant Isolation)
Trong Visual Studio: **Test Explorer** → Run All.
Hoặc CLI: `dotnet test Backend/NamIT.Business.Tests`

Test project dùng EF Core InMemory provider nên không cần PostgreSQL để chạy test.

## 8. Kiến trúc đã hoàn thành ở Phase 1
- Clean Architecture: Domain / Application / Infrastructure / Api / Tests.
- Multi-tenant isolation qua Global Query Filter (kết hợp TenantId + soft-delete) trong `ApplicationDbContext`, TenantId luôn lấy từ JWT claim `tenant_id` (server-side), không tin frontend.
- JWT Access Token + Refresh Token (refresh token hash SHA-256 trước khi lưu DB, không log plain text).
- Password hash bằng BCrypt (work factor 12).
- Bảng: Tenants, BusinessTypes, TenantModules, Branches, Users, UserBranches, Roles, Permissions, RolePermissions, UserRoles, RefreshTokens, AuditLogs.
- Role hệ thống: SUPER_ADMIN, TENANT_ADMIN, MANAGER, SELLER, ACCOUNTANT, WAREHOUSE, CASHIER (seed sẵn).
- Permission mẫu (PRODUCT_*, ORDER_*, INVENTORY_*, CUSTOMER_*, REPORT_VIEW, USER_*, SETTINGS_*) seed sẵn, gán full quyền cho TENANT_ADMIN.
- Global exception handling trả về format `ApiResponse<T>` thống nhất, không lộ stack trace ở Production.
- Serilog logging ra console + file.
- Swagger có sẵn nút Authorize để test JWT.
- React + TypeScript + Vite: trang Login, Dashboard shell, AuthContext (lưu token, tự refresh khi 401), route bảo vệ.
- Test tenant isolation: xác nhận Tenant A không thấy dữ liệu Tenant B, entity mới luôn nhận TenantId từ server context (không tin client), soft-delete được lọc mặc định.

## 9. Việc BẠN cần tự làm (vì môi trường build không có internet)
File code đã đầy đủ nhưng **chưa được restore/build/test thật sự** vì môi trường tạo ra file này không có kết nối mạng để tải NuGet/npm packages. Khi mở trên Visual Studio của bạn (có mạng), hãy:
1. Restore NuGet + npm packages (bước 2 và 6 ở trên).
2. Build solution (Ctrl+Shift+B) — sửa lỗi build nếu có (phiên bản package có thể cần điều chỉnh nhẹ tuỳ SDK bạn cài).
3. Chạy migration + `dotnet test` — báo lại nếu có lỗi để mình fix tiếp.

## 10. Chưa làm ở Phase 1 (theo đúng phạm vi đã thống nhất)
Product, Customer, Supplier, Order, Payment, Inventory, Table, Kitchen, Billiard, Fashion Variant, Dashboard/Report chi tiết, SaaS Subscription — sẽ làm ở Phase 2 trở đi, sau khi bạn xác nhận Phase 1 build/test ổn trên máy bạn.

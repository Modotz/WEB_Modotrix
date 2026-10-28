 ERP.Web

Versi ini mempertahankan template/sidebar ERP di sebelah kiri.

Perubahan:
- Sidebar kiri tidak dihilangkan.
- Menu tetap lengkap sesuai rancangan awal.
- Users tetap berada di `Master Data > Users`.
- Tambah User menggunakan Bootstrap Modal.
- Edit User menggunakan Bootstrap Modal.
- Create menggunakan POST.
- Update menggunakan POST.
- Delete menggunakan POST.
- Tidak menggunakan HttpPut.
- Tidak menggunakan HttpDelete.
- Delete tetap soft delete melalui Web API.

API:
- GET  /api/users
- GET  /api/users/{id}
- POST /api/users/create
- POST /api/users/update
- POST /api/users/delete

## Login & Hak Akses

- Semua halaman wajib login (cookie `erp_auth`); token JWT dari API disimpan di cookie dan diteruskan ke setiap request API (`Services/ApiTokenHandler.cs`).
- Hak akses mengikuti tabel `access_role` (matriks Lihat/Tambah/Ubah/Hapus per role, menu **Master Data > Hak Akses**). Role id 1 (Administrator) selalu akses penuh.
- Lupa password: `/Account/ForgotPassword` → email berisi tautan `/Account/ResetPassword?token=...` (berlaku 1 jam).

### Menambah modul (halaman) baru

1. **Daftarkan** menu di `Security/MenuRegistry.cs` → `Pages`:
   `new("url-menu-di-tabel-menus", "NamaController")`.
   Tanpa ini halaman hanya bisa dibuka Administrator, dan saat aplikasi start muncul warning
   `Controller belum terdaftar di Security/MenuRegistry.cs`.
2. **Izin action** ditentukan otomatis dari nama action:
   `Create*/Add*/Copy*` = tambah, `Edit*/Update*/Save*/Approve*/Cancel*/Send*` = ubah,
   `Delete*/Remove*` = hapus, `Upload*` = tambah atau ubah, selainnya = lihat.
   Bila berbeda dari aplikasi PHP (lihat filter `permission:menu,aksi` di `app/Config/Routes*.php`),
   tambahkan pengecualian di `ActionOverrides` / `WritesAsUpdateControllers`.
3. **Tombol aksi** di view diberi atribut agar otomatis tersembunyi bila tidak diizinkan:
   - `data-perm="create|update|delete"` — aksi pada menu halaman ini,
   - `data-perm="goods-receipts:create"` — aksi pada menu lain,
   - `data-perm-any="create update"` — tampil bila salah satu diizinkan.
   Ini hanya tampilan; server tetap menolak aksi tanpa izin (403).
4. Di Razor bisa juga: `@if (await Access.CanAsync("brands", "create")) { ... }`.

## Audit Log

- Perubahan data dicatat otomatis oleh API. Web mencatat ekspor & cetak lewat `Security/ActivityAuditFilter.cs`
  untuk action bernama `Export*`, `Print*` dan `Label` (cukup ikuti penamaan ini di modul baru).
- Halaman: **Pengaturan > Log Audit** (`/AuditLog`) — filter, detail perubahan (sebelum/sesudah), ekspor Excel/PDF, cetak, bersihkan log.

## Identitas record: UID, bukan id angka

- Route default `{controller}/{action}/{uid?}` → action memakai `Guid uid` (mis. `/Products/Edit/<uid>`), parameter induk `productUid`, `poUid`, dst.
- View: `asp-route-uid="@item.Uid"`, hidden `name="Uid"`, JS memakai `@item.Uid`. Jangan pernah menaruh id angka di URL atau payload aksi.
- Method API client ada di `Services/ApiClient/ApiClient.<Modul>.cs` (partial class per modul).

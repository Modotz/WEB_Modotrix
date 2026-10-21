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

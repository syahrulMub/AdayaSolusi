# Order Management API

Prototype REST API menggunakan ASP.NET Core untuk memenuhi technical test Senior .NET Developer.

## Technology

- ASP.NET Core
- Entity Framework Core
- SQLite
- xUnit
- Swagger

---

# Database

Database menggunakan **SQLite**.

Alasan pemilihan:

- Mudah dijalankan tanpa instalasi database server.
- Cocok untuk prototype.
- Mendukung transaction sehingga cukup untuk mendemonstrasikan concurrency dan idempotency.

Migration dapat dijalankan dengan:

```bash
dotnet ef database update
```

---

# Features

## Functional

- Create Order
- Get Order
- Get Orders
- Update Order Status
- Cancel Order
- Stock Management

Business Rules:

- Pending → Confirmed / Cancelled
- Confirmed → Shipped / Cancelled
- Shipped → Delivered
- Delivered / Cancelled merupakan terminal state.

Stock akan:

- berkurang saat order dibuat
- dikembalikan ketika order dibatalkan
- tidak boleh menjadi minus

---

# Idempotency

Endpoint Create Order menggunakan header

```
Idempotency-Key
```

Contoh

```
POST /api/orders

Idempotency-Key: 123456789
```

Strategi yang digunakan:

1. Client mengirim Idempotency-Key.
2. Request pertama menyimpan key ke tabel `IdempotencyRecords`.
3. Jika request lain datang dengan key yang sama sebelum request pertama selesai, database akan menolak karena terdapat Unique Constraint pada kolom Key.
4. Request kedua akan mendapatkan response **409 Conflict**.

Pendekatan ini dipilih karena sederhana, mudah dipahami, dan memanfaatkan atomic operation dari database.

---

# Concurrency Handling

## Skenario A

Concurrent Stock Deduction.

Implementasi:

- Stock divalidasi sebelum order diproses.
- Pengurangan stock dilakukan di dalam transaksi database.
- Idempotency mencegah duplicate submit dari client.

Tujuan:

- stock tidak pernah minus
- hanya request yang valid yang berhasil

---

## Skenario B

Concurrent Status Update.

Implementasi menggunakan **Optimistic Concurrency**.

Order memiliki RowVersion.

Saat update status:

- client mengirim RowVersion
- EF Core melakukan pengecekan RowVersion
- jika RowVersion berubah maka update dibatalkan dan mengembalikan **409 Conflict**

Hal ini memastikan hanya satu admin yang berhasil melakukan perubahan.

---

## Skenario C

Concurrent Idempotent Create.

Dua request dengan Idempotency-Key yang sama dikirim secara bersamaan.

Implementasi:

- kolom Key memiliki Unique Index
- request pertama berhasil menyimpan key
- request kedua gagal karena duplicate key
- hanya satu order yang berhasil dibuat

---

# Race Condition Prevention

Selain requirement utama, terdapat beberapa potensi race condition.

## 1. Duplicate Submit

Penyebab

User menekan tombol submit berkali-kali.

Solusi

Menggunakan Idempotency-Key sehingga hanya satu request yang diproses.

---

## 2. Concurrent Status Update

Penyebab

Beberapa admin mengubah status order secara bersamaan.

Solusi

Menggunakan Optimistic Concurrency (RowVersion).

---

# Error Handling

API menggunakan status code HTTP yang sesuai.

| Status Code | Keterangan            |
| ----------- | --------------------- |
| 200         | Success               |
| 201         | Created               |
| 400         | Bad Request           |
| 404         | Not Found             |
| 409         | Conflict              |
| 500         | Internal Server Error |

---

# Logging

Logging menggunakan `ILogger`.

Log yang dicatat antara lain:

- Request berhasil
- Request gagal
- Order tidak ditemukan
- Invalid status transition
- Exception

---

# Testing

Pengujian menggunakan **xUnit**.

Concurrent test yang dibuat:

**Concurrent_CreateOrder_WithSameIdempotencyKey_Should_CreateOnlyOneOrder**

Skenario:

- mengirim dua request secara bersamaan menggunakan `Task.WhenAll()`
- kedua request menggunakan Idempotency-Key yang sama
- hasil yang diharapkan:
  - satu request berhasil (`201 Created`)
  - satu request gagal (`409 Conflict`)

---

# Menjalankan Project

Restore package

```bash
dotnet restore
```

Migration

```bash
dotnet ef database update
```

Run API

```bash
dotnet run
```

Run Test

```bash
dotnet test
```

Swagger

```
https://localhost:{port}/swagger
```

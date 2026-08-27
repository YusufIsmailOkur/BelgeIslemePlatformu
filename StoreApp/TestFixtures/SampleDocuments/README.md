# Örnek Belge Seti

Bu klasör, MVP boyunca her hafta manuel doğrulama için kullanılacak sabit test belgesi
setini barındırır (bkz. `docs/TEST-SCENARIOS.md`).

Her alt klasöre ilgili belge türünden **2-3 gerçek veya temsili örnek dosya** (PDF/Excel/CSV)
elle eklenmelidir:

- `Invoice/` — Fatura
- `Quote/` — Teklif
- `Order/` — Sipariş
- `DispatchNote/` — İrsaliye
- `Specification/` — Şartname
- `ShipmentRequest/` — Sevkiyat Talep Formu
- `ContractAppendix/` — Çerçeve Sözleşme Eki
- `TechnicalAppendix/` — Teknik Şartname Eki
- `BulkOrderList/` — Toplu Sipariş/Liste (Excel/CSV)
- `Other/` — Genel belge (şablon dışı örnekler)

Ayrıca en az **1 taranmış (görüntü tabanlı) PDF** ve en az **1 düşük güven skoru üretecek
bozuk/eksik örnek** bulunmalıdır (Hafta 5/6 test senaryoları için).

Bu klasördeki dosyalar gerçek müşteri verisi içermemelidir.
